using Core.Application.Contracts.Financeiro;
using Core.Application.DTOs.Financeiro;
using Core.Domain.Common;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed partial class DespesaService(
    IDespesaRepository repository,
    IReceitaRepository receitaRepository,
    IContaBancariaRepository contaRepository,
    ICartaoRepository cartaoRepository,
    IAreaRepository areaRepository,
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IDocumentoStorageService documentoStorageService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher,
    FaturaCartaoService? faturaCartaoService = null)
{
    private sealed record AmigoRateioValidado(int AmigoId, string Nome, decimal Valor);

    public async Task<IReadOnlyCollection<DespesaListaDto>> ListarAsync(ListarDespesasRequest request, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var dataInicio = request.DataInicio;
        var dataFim = request.DataFim;

        if (dataInicio.HasValue && dataFim.HasValue && dataFim.Value < dataInicio.Value)
            throw new DomainException("periodo_invalido");

        if (string.IsNullOrWhiteSpace(request.Competencia) && !dataInicio.HasValue && !dataFim.HasValue)
        {
            var periodoAtual = CompetenciaPeriodoHelper.Resolver(null, null, null);
            dataInicio = periodoAtual.DataInicio;
            dataFim = periodoAtual.DataFim;
        }

        try
        {
            var despesas = await repository.ListarPorUsuarioAsync(
                usuarioAutenticadoId,
                request.Id,
                request.Descricao,
                request.Competencia,
                dataInicio,
                dataFim,
                cancellationToken);

            if (!cancellationToken.IsCancellationRequested && request.VerificarUltimaRecorrencia)
            {
                await VerificarUltimasRecorrenciasERecuperarFalhasAsync(usuarioAutenticadoId, request.Competencia, cancellationToken);
            }

            return despesas
                .Where(x => !EhTransacaoEntreContas(x))
                .Where(x => !request.DesconsiderarVinculadosCartaoCredito || !x.FaturaCartaoId.HasValue)
                .Where(x => !request.DesconsiderarCancelados || x.Status != StatusDespesa.Cancelada)
                .Select(MapLista)
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                return [];

            throw;
        }
    }

    public async Task<IReadOnlyCollection<DespesaDto>> ListarPendentesAprovacaoAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        try
        {
            return (await repository.ListarPendentesAprovacaoPorUsuarioAsync(usuarioAutenticadoId, cancellationToken))
                .Select(Map)
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                return [];

            throw;
        }
    }

    public async Task<DespesaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada"));

    public async Task<DespesaDto> CriarAsync(CriarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        var dataVencimentoInformada = req.TipoPagamento != TipoPagamento.CartaoCredito || req.DataVencimento != default
            ? req.DataVencimento
            : DateOnly.FromDateTime(req.DataLancamento);
        ValidarComum(req.Descricao, req.DataLancamento, dataVencimentoInformada, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.ContaBancariaId, req.CartaoId, usuarioAutenticadoId, cancellationToken);
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            req.TipoPagamento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);
        var competencia = ResolverCompetencia(req.Competencia);
        var faturaCartaoId = await ResolverFaturaCartaoIdAsync(vinculo.CartaoId, competencia, usuarioAutenticadoId, cancellationToken);
        var dataVencimentoFatura = await ObterDataVencimentoFaturaAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken);
        var ehLancamentoCartao = vinculo.CartaoId.HasValue;
        var documentos = await SalvarDocumentosAsync(req.Documentos ?? [], usuarioAutenticadoId, cancellationToken: cancellationToken);

        var despesa = new Despesa
        {
            Descricao = req.Descricao.Trim(),
            Observacao = req.Observacao,
            Competencia = competencia,
            DataLancamento = req.DataLancamento,
            DataVencimento = dataVencimentoFatura ?? dataVencimentoInformada,
            TipoDespesa = req.TipoDespesa,
            TipoPagamento = req.TipoPagamento,
            Recorrencia = recorrencia,
            RecorrenciaFixa = recorrenciaFixa,
            QuantidadeRecorrencia = quantidadeRecorrencia,
            ValorTotal = req.ValorTotal,
            ValorTotalRateioAmigos = amigos.Count == 0 ? null : valorTotalRateioAmigos,
            TipoRateioAmigos = amigos.Count == 0 ? null : tipoRateioAmigos,
            ValorLiquido = liquido,
            Desconto = req.Desconto,
            Acrescimo = req.Acrescimo,
            Imposto = req.Imposto,
            Juros = req.Juros,
            ContaBancariaId = vinculo.ContaBancariaId,
            ContaDestinoId = contaDestinoId,
            CartaoId = vinculo.CartaoId,
            FaturaCartaoId = faturaCartaoId,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = ehLancamentoCartao ? StatusDespesa.Efetivada : StatusDespesa.Pendente,
            DataEfetivacao = ehLancamentoCartao ? req.DataLancamento : null,
            ValorEfetivacao = ehLancamentoCartao ? liquido : null,
            Documentos = documentos,
            AmigosRateio = amigosValidados.Select(x => new DespesaAmigoRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList(),
            AreasRateio = (req.AreasSubAreasRateio ?? []).Select(x => new DespesaAreaRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList(),
            Logs =
            [
                new DespesaLog
                {
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = ehLancamentoCartao
                        ? "Despesa criada com status efetivado por lancamento em cartao."
                        : "Despesa criada com status pendente."
                }
            ]
        };

        var despesaCriada = await repository.CriarAsync(despesa, cancellationToken);
        await CriarEspelhosRateioAsync(despesaCriada, amigosValidados, req.AreasSubAreasRateio ?? [], cancellationToken);
        despesaCriada = await SincronizarTransacaoEntreContasAsync(despesaCriada, usuarioAutenticadoId, cancellationToken);
        if (ehLancamentoCartao)
        {
            await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
                TipoTransacaoFinanceira.Despesa,
                despesaCriada.Id,
                usuarioAutenticadoId,
                DateOnly.FromDateTime(despesaCriada.DataLancamento),
                0m,
                despesaCriada.ValorEfetivacao ?? despesaCriada.ValorLiquido,
                despesaCriada.ValorEfetivacao ?? despesaCriada.ValorLiquido,
                "Efetivacao de despesa",
                despesaCriada.TipoPagamento,
                despesaCriada.ContaBancariaId,
                despesaCriada.ContaDestinoId,
                despesaCriada.CartaoId,
                cancellationToken: cancellationToken);
        }
        await RecalcularFaturaAsync(despesaCriada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        var alvo = recorrenciaFixa ? 100 : quantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo > 1)
        {
            var mensagem = new DespesaRecorrenciaBackgroundMessage(
                usuarioAutenticadoId,
                despesaCriada.Id,
                req.Descricao.Trim(),
                req.Observacao,
                despesaCriada.DataHoraCadastro,
                req.DataLancamento,
                dataVencimentoInformada,
                req.TipoDespesa,
                req.TipoPagamento,
                recorrencia,
                recorrenciaFixa,
                recorrenciaFixa ? 100 : quantidadeRecorrencia,
                req.ValorTotal,
                req.Desconto,
                req.Acrescimo,
                req.Imposto,
                req.Juros,
                vinculo.ContaBancariaId,
                contaDestinoId,
                vinculo.CartaoId,
                amigos.Count == 0 ? null : valorTotalRateioAmigos,
                amigos.Count == 0 ? null : tipoRateioAmigos,
                [],
                amigosValidados.Select(x => new RateioAmigoBackgroundMessage(x.AmigoId, x.Nome, x.Valor)).ToArray(),
                (req.AreasSubAreasRateio ?? []).Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarDespesaAsync(mensagem, cancellationToken);
        }

        return Map(despesaCriada);
    }

    public async Task<DespesaDto> AtualizarAsync(
        long id,
        AtualizarDespesaRequest req,
        EscopoRecorrencia escopoRecorrencia = EscopoRecorrencia.ApenasEssa,
        CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Pendente &&
            !(despesa.FaturaCartaoId.HasValue && despesa.Status == StatusDespesa.Efetivada))
            throw new DomainException("status_invalido");
        if (!Enum.IsDefined(escopoRecorrencia)) throw new DomainException("escopo_recorrencia_invalido");
        await ValidarFaturaParaAlteracaoAsync(despesa.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        var dataVencimentoInformada = req.TipoPagamento != TipoPagamento.CartaoCredito || req.DataVencimento != default
            ? req.DataVencimento
            : DateOnly.FromDateTime(req.DataLancamento);
        ValidarComum(req.Descricao, req.DataLancamento, dataVencimentoInformada, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.ContaBancariaId ?? despesa.ContaBancariaId, req.CartaoId ?? despesa.CartaoId, usuarioAutenticadoId, cancellationToken);
        var contaDestinoIdBase = await ResolverContaDestinoTransferenciaAsync(
            req.TipoPagamento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId ?? despesa.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);

        var serie = await ListarSerieRecorrenteAsync(despesa, usuarioAutenticadoId, cancellationToken);
        var faturasOriginais = serie.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).ToHashSet();
        var alvos = SelecionarAlvosPorEscopo(serie, despesa, escopoRecorrencia);
        var indicePorId = serie
            .Select((item, indice) => new { item.Id, Indice = indice })
            .ToDictionary(x => x.Id, x => x.Indice);
        var indiceBase = indicePorId.GetValueOrDefault(despesa.Id, 0);

        Despesa? despesaAtualizada = null;

        foreach (var alvo in alvos)
        {
            var deslocamento = indicePorId.GetValueOrDefault(alvo.Id, indiceBase) - indiceBase;
            var dataLancamentoBase = AvancarData(req.DataLancamento, recorrencia, deslocamento);

            alvo.Descricao = req.Descricao.Trim();
            alvo.Observacao = req.Observacao;
            alvo.Competencia = ResolverCompetencia(req.Competencia, dataLancamentoBase);
            alvo.DataLancamento = dataLancamentoBase;
            var dataVencimentoAtualizada = AvancarData(dataVencimentoInformada, recorrencia, deslocamento);
            alvo.DataVencimento = dataVencimentoAtualizada;
            alvo.TipoDespesa = req.TipoDespesa;
            alvo.TipoPagamento = req.TipoPagamento;
            alvo.Recorrencia = recorrencia;
            alvo.RecorrenciaFixa = recorrenciaFixa;
            alvo.QuantidadeRecorrencia = quantidadeRecorrencia;
            alvo.ValorTotal = req.ValorTotal;
            alvo.ValorTotalRateioAmigos = amigos.Count == 0 ? null : valorTotalRateioAmigos;
            alvo.TipoRateioAmigos = amigos.Count == 0 ? null : tipoRateioAmigos;
            alvo.ValorLiquido = liquido;
            alvo.Desconto = req.Desconto;
            alvo.Acrescimo = req.Acrescimo;
            alvo.Imposto = req.Imposto;
            alvo.Juros = req.Juros;
            alvo.ContaBancariaId = vinculo.ContaBancariaId;
            alvo.ContaDestinoId = contaDestinoIdBase;
            alvo.CartaoId = vinculo.CartaoId;
            alvo.FaturaCartaoId = await ResolverFaturaCartaoIdAsync(vinculo.CartaoId, alvo.Competencia, usuarioAutenticadoId, cancellationToken);
            var dataVencimentoFatura = await ObterDataVencimentoFaturaAsync(alvo.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
            if (dataVencimentoFatura.HasValue)
                alvo.DataVencimento = dataVencimentoFatura.Value;
            if (alvo.Status == StatusDespesa.Efetivada)
            {
                alvo.DataEfetivacao ??= alvo.DataLancamento;
                alvo.ValorEfetivacao = liquido;
            }
            else
            {
                alvo.DataEfetivacao = null;
                alvo.ValorEfetivacao = null;
            }
            if (req.Documentos is not null)
                alvo.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, alvo.Id, cancellationToken: cancellationToken);

            alvo.AmigosRateio = amigosValidados.Select(x => new DespesaAmigoRateio
            {
                DespesaId = alvo.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList();
            alvo.AreasRateio = (req.AreasSubAreasRateio ?? []).Select(x => new DespesaAreaRateio
            {
                DespesaId = alvo.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList();
            alvo.Logs.Add(new DespesaLog { DespesaId = alvo.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa atualizada." });

            var atualizado = await repository.AtualizarAsync(alvo, cancellationToken);
            await SincronizarEspelhosRateioAsync(atualizado, amigosValidados, req.AreasSubAreasRateio ?? [], cancellationToken);
            atualizado = await SincronizarTransacaoEntreContasAsync(atualizado, usuarioAutenticadoId, cancellationToken);

            if (atualizado.Id == id)
                despesaAtualizada = atualizado;
        }

        var faturasAtualizadas = alvos.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).ToHashSet();
        foreach (var faturaId in faturasOriginais.Union(faturasAtualizadas))
            await RecalcularFaturaAsync(faturaId, usuarioAutenticadoId, cancellationToken);

        return Map(despesaAtualizada ?? despesa);
    }

    public async Task<DespesaDto> AprovarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (!despesa.DespesaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (despesa.Status != StatusDespesa.PendenteAprovacao) throw new DomainException("status_invalido");

        despesa.Status = StatusDespesa.Pendente;
        despesa.Logs.Add(new DespesaLog
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio aprovado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(despesa, cancellationToken));
    }

    public async Task<DespesaDto> RejeitarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (!despesa.DespesaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (despesa.Status != StatusDespesa.PendenteAprovacao) throw new DomainException("status_invalido");

        despesa.Status = StatusDespesa.Rejeitado;
        despesa.Logs.Add(new DespesaLog
        {
            DespesaId = despesa.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio rejeitado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(despesa, cancellationToken));
    }

    public async Task<DespesaDto> EfetivarAsync(long id, EfetivarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(despesa.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(req.TipoPagamento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (DateOnly.FromDateTime(req.DataEfetivacao) < DateOnly.FromDateTime(despesa.DataLancamento)) throw new DomainException("periodo_invalido");
        var vinculo = await ResolverVinculoPagamentoAsync(
            req.TipoPagamento,
            req.ContaBancariaId ?? despesa.ContaBancariaId,
            req.CartaoId ?? despesa.CartaoId,
            usuarioAutenticadoId,
            cancellationToken);
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            req.TipoPagamento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId ?? despesa.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);

        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = despesa.ValorEfetivacao ?? 0m;
        despesa.DataEfetivacao = req.DataEfetivacao;
        despesa.TipoPagamento = req.TipoPagamento;
        despesa.ValorTotal = req.ValorTotal;
        despesa.Desconto = req.Desconto;
        despesa.Acrescimo = req.Acrescimo;
        despesa.Imposto = req.Imposto;
        despesa.Juros = req.Juros;
        despesa.ContaBancariaId = vinculo.ContaBancariaId;
        despesa.ContaDestinoId = contaDestinoId;
        despesa.CartaoId = vinculo.CartaoId;
        despesa.FaturaCartaoId = await ResolverFaturaCartaoIdAsync(vinculo.CartaoId, despesa.Competencia, usuarioAutenticadoId, cancellationToken);
        var dataVencimentoFatura = await ObterDataVencimentoFaturaAsync(despesa.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (dataVencimentoFatura.HasValue)
            despesa.DataVencimento = dataVencimentoFatura.Value;
        despesa.ValorLiquido = liquido;
        despesa.ValorEfetivacao = liquido;
        despesa.Status = StatusDespesa.Efetivada;
        if (req.Documentos is not null)
            despesa.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, despesa.Id, cancellationToken: cancellationToken);
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa efetivada." });

        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        despesaAtualizada = await SincronizarTransacaoEntreContasAsync(despesaAtualizada, usuarioAutenticadoId, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(req.DataEfetivacao),
            valorAntesTransacao,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            "Efetivacao de despesa",
            despesaAtualizada.TipoPagamento,
            despesaAtualizada.ContaBancariaId,
            contaDestinoId,
            despesaAtualizada.CartaoId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(req.ObservacaoHistorico),
            transacaoIdEspelho: despesaAtualizada.ReceitaTransferenciaId);

        await RecalcularFaturaAsync(despesaAtualizada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        return Map(despesaAtualizada);
    }

    public async Task<DespesaDto> CancelarAsync(
        long id,
        EscopoRecorrencia escopoRecorrencia = EscopoRecorrencia.ApenasEssa,
        CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(despesa.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (despesa.FaturaCartaoId.HasValue && despesa.Status == StatusDespesa.Efetivada)
        {
            if (escopoRecorrencia != EscopoRecorrencia.ApenasEssa)
                throw new DomainException("status_invalido");

            var valorAntesTransacao = despesa.ValorEfetivacao ?? despesa.ValorLiquido;
            var dataEstorno = DataHoraBrasil.Hoje();
            despesa.Status = StatusDespesa.Cancelada;
            despesa.DataEfetivacao = null;
            despesa.ValorEfetivacao = null;
            despesa.Logs.Add(new DespesaLog
            {
                DespesaId = despesa.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                Acao = AcaoLogs.Exclusao,
                Descricao = "Despesa estornada e cancelada."
            });

            var cancelada = await repository.AtualizarAsync(despesa, cancellationToken);
            cancelada = await SincronizarTransacaoEntreContasAsync(cancelada, usuarioAutenticadoId, cancellationToken);
            await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
                TipoTransacaoFinanceira.Despesa,
                cancelada.Id,
                usuarioAutenticadoId,
                dataEstorno,
                valorAntesTransacao,
                valorAntesTransacao,
                0m,
                "Estorno de despesa",
                cancelada.TipoPagamento,
                cancelada.ContaBancariaId,
                cancelada.ContaDestinoId,
                cancelada.CartaoId,
                cancellationToken: cancellationToken);
            await RecalcularFaturaAsync(cancelada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
            return Map(cancelada);
        }

        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(escopoRecorrencia)) throw new DomainException("escopo_recorrencia_invalido");

        var serie = await ListarSerieRecorrenteAsync(despesa, usuarioAutenticadoId, cancellationToken);
        var alvos = SelecionarAlvosPorEscopo(serie, despesa, escopoRecorrencia);

        Despesa? despesaAtualizada = null;

        foreach (var alvo in alvos)
        {
            alvo.Status = StatusDespesa.Cancelada;
            alvo.Logs.Add(new DespesaLog { DespesaId = alvo.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Despesa cancelada." });
            var atualizado = await repository.AtualizarAsync(alvo, cancellationToken);
            atualizado = await SincronizarTransacaoEntreContasAsync(atualizado, usuarioAutenticadoId, cancellationToken);

            if (atualizado.Id == id)
                despesaAtualizada = atualizado;
        }

        if (escopoRecorrencia == EscopoRecorrencia.TodasPendentes && despesa.RecorrenciaFixa)
        {
            foreach (var item in serie.Where(x => x.RecorrenciaFixa))
            {
                item.RecorrenciaFixa = false;
                await repository.AtualizarAsync(item, cancellationToken);
            }
        }

        foreach (var faturaId in alvos.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).Distinct())
            await RecalcularFaturaAsync(faturaId, usuarioAutenticadoId, cancellationToken);

        return Map(despesaAtualizada ?? despesa);
    }

    public async Task<DespesaDto> EstornarAsync(long id, EstornarDespesaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(despesa.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (despesa.Status != StatusDespesa.Efetivada) throw new DomainException("status_invalido");
        if (req.DataEstorno == default) throw new DomainException("data_estorno_obrigatoria");
        if (req.DataEstorno < DateOnly.FromDateTime(despesa.DataLancamento)) throw new DomainException("periodo_invalido");
        if (despesa.DataEfetivacao.HasValue && req.DataEstorno < DateOnly.FromDateTime(despesa.DataEfetivacao.Value)) throw new DomainException("periodo_invalido");
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            despesa.TipoPagamento,
            despesa.ContaBancariaId,
            req.ContaDestinoId ?? despesa.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);
        var valorAntesTransacao = despesa.ValorEfetivacao ?? despesa.ValorLiquido;
        despesa.Status = StatusDespesa.Pendente;
        despesa.DataEfetivacao = null;
        despesa.ValorEfetivacao = null;
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa estornada." });
        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        despesaAtualizada = await SincronizarTransacaoEntreContasAsync(despesaAtualizada, usuarioAutenticadoId, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEstorno,
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de despesa",
            despesa.TipoPagamento,
            despesa.ContaBancariaId,
            contaDestinoId,
            despesa.CartaoId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(req.ObservacaoHistorico),
            ocultarDoHistorico: req.OcultarDoHistorico,
            transacaoIdEspelho: despesaAtualizada.ReceitaTransferenciaId);

        await RecalcularFaturaAsync(despesaAtualizada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        return Map(despesaAtualizada);
    }

    private Task<long?> ResolverFaturaCartaoIdAsync(long? cartaoId, string competencia, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.ResolverFaturaIdParaTransacaoCartaoAsync(cartaoId, competencia, usuarioAutenticadoId, cancellationToken) ?? Task.FromResult<long?>(null);

    private Task<DateOnly?> ObterDataVencimentoFaturaAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.ObterDataVencimentoPorFaturaIdAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken) ?? Task.FromResult<DateOnly?>(null);

    private Task RecalcularFaturaAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.RecalcularTotalPorFaturaIdAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken) ?? Task.CompletedTask;

    private Task ValidarFaturaParaAlteracaoAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.ValidarFaturaPermiteAlteracaoAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken) ?? Task.CompletedTask;

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task<List<Despesa>> ListarSerieRecorrenteAsync(Despesa referencia, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (referencia.Recorrencia == Recorrencia.Unica || referencia.DespesaOrigemId.HasValue)
            return [referencia];

        var todas = await repository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        var chaveSerie = referencia.DespesaRecorrenciaOrigemId ?? referencia.Id;
        var seriePorChave = todas
            .Where(x => x.DespesaOrigemId is null)
            .Where(x => x.Id == chaveSerie || x.DespesaRecorrenciaOrigemId == chaveSerie)
            .OrderBy(x => x.DataLancamento)
            .ThenBy(x => x.Id)
            .ToList();

        if (seriePorChave.Any(x => x.Id == referencia.Id))
            return seriePorChave;

        var serie = todas
            .Where(x => x.DespesaOrigemId is null)
            .Where(x => x.Descricao == referencia.Descricao)
            .Where(x => x.TipoDespesa == referencia.TipoDespesa)
            .Where(x => x.TipoPagamento == referencia.TipoPagamento)
            .Where(x => x.Recorrencia == referencia.Recorrencia)
            .Where(x => x.RecorrenciaFixa == referencia.RecorrenciaFixa)
            .Where(x => x.ContaBancariaId == referencia.ContaBancariaId)
            .Where(x => x.CartaoId == referencia.CartaoId)
            .OrderBy(x => x.DataLancamento)
            .ThenBy(x => x.Id)
            .ToList();

        return serie.Any(x => x.Id == referencia.Id) ? serie : [referencia];
    }

    private static List<Despesa> SelecionarAlvosPorEscopo(IReadOnlyList<Despesa> serie, Despesa referencia, EscopoRecorrencia escopoRecorrencia)
    {
        if (escopoRecorrencia == EscopoRecorrencia.ApenasEssa || referencia.Recorrencia == Recorrencia.Unica || referencia.DespesaOrigemId.HasValue)
            return [referencia];

        var indiceBase = serie
            .Select((item, indice) => new { item.Id, Indice = indice })
            .FirstOrDefault(x => x.Id == referencia.Id)?.Indice ?? -1;

        if (indiceBase < 0)
            return [referencia];

        return escopoRecorrencia switch
        {
            EscopoRecorrencia.EssaEAsProximas => serie.Where((item, indice) => indice >= indiceBase && item.Status == StatusDespesa.Pendente).ToList(),
            EscopoRecorrencia.TodasPendentes => serie.Where(item => item.Status == StatusDespesa.Pendente).ToList(),
            _ => [referencia]
        };
    }

    private async Task VerificarUltimasRecorrenciasERecuperarFalhasAsync(
        int usuarioAutenticadoId,
        string? competencia,
        CancellationToken cancellationToken)
    {
        var todasDespesas = await repository.ListarPorUsuarioAsync(
            usuarioAutenticadoId,
            null,
            null,
            null,
            null,
            null,
            cancellationToken);

        var candidatas = todasDespesas
            .Where(x => x.DespesaOrigemId is null)
            .Where(x => x.Recorrencia != Recorrencia.Unica)
            .GroupBy(x => x.DespesaRecorrenciaOrigemId ?? x.Id)
            .ToArray();

        var periodoCompetencia = CompetenciaPeriodoHelper.Resolver(competencia, null, null);

        foreach (var grupo in candidatas)
        {
            var origem = grupo
                .OrderBy(x => x.DataLancamento)
                .ThenBy(x => x.Id)
                .First();
            var transacaoBaseId = origem.DespesaRecorrenciaOrigemId ?? origem.Id;

            var alvoBase = origem.RecorrenciaFixa
                ? Math.Max(100, grupo.Max(x => x.QuantidadeRecorrencia.GetValueOrDefault(1)))
                : origem.QuantidadeRecorrencia.GetValueOrDefault(1);

            if (alvoBase <= 1)
                continue;

            var quantidadeGerada = ContarTransacoesGeradasDaBase(grupo, transacaoBaseId);
            var possuiLacuna = SeriePossuiLacuna(grupo, origem, alvoBase);

            if (quantidadeGerada < alvoBase || possuiLacuna)
            {
                await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, alvoBase, cancellationToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(competencia))
                continue;

            if (!origem.RecorrenciaFixa)
                continue;

            var dataUltima = AvancarData(origem.DataLancamento, origem.Recorrencia, alvoBase - 1);
            var dataUltimaDia = DateOnly.FromDateTime(dataUltima);
            if (dataUltimaDia < periodoCompetencia.DataInicio || dataUltimaDia > periodoCompetencia.DataFim)
                continue;

            await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, alvoBase + 100, cancellationToken);
        }
    }

    private static int ContarTransacoesGeradasDaBase(IEnumerable<Despesa> grupo, long transacaoBaseId) =>
        grupo.Count(x =>
            x.DespesaOrigemId is null &&
            (x.Id == transacaoBaseId || x.DespesaRecorrenciaOrigemId == transacaoBaseId));

    private static bool SeriePossuiLacuna(IEnumerable<Despesa> grupo, Despesa origem, int alvo)
    {
        var dataLancamentoOrigemInicio = origem.DataLancamento.Date;
        var dataLancamentoOrigemFim = dataLancamentoOrigemInicio.AddDays(1);

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamentoEsperada = AvancarData(origem.DataLancamento, origem.Recorrencia, numero - 1);
            var dataVencimentoEsperada = AvancarData(origem.DataVencimento, origem.Recorrencia, numero - 1);

            var dataLancamentoInicio = dataLancamentoEsperada.Date;
            var dataLancamentoFim = dataLancamentoInicio.AddDays(1);

            var existe = grupo.Any(x =>
                (
                    (x.DataLancamento >= dataLancamentoInicio && x.DataLancamento < dataLancamentoFim) ||
                    (x.DataLancamento >= dataLancamentoOrigemInicio && x.DataLancamento < dataLancamentoOrigemFim)
                ) &&
                x.DataVencimento == dataVencimentoEsperada);

            if (!existe)
                return true;
        }

        return false;
    }

    private async Task PublicarRecorrenciaDaOrigemAsync(int usuarioAutenticadoId, Despesa origem, int quantidadeRecorrencia, CancellationToken cancellationToken)
    {
        var mensagem = new DespesaRecorrenciaBackgroundMessage(
            usuarioAutenticadoId,
            origem.DespesaRecorrenciaOrigemId ?? origem.Id,
            origem.Descricao,
            origem.Observacao,
            origem.DataHoraCadastro,
            origem.DataLancamento,
            origem.DataVencimento,
            origem.TipoDespesa,
            origem.TipoPagamento,
            origem.Recorrencia,
            origem.RecorrenciaFixa,
            quantidadeRecorrencia,
            origem.ValorTotal,
            origem.Desconto,
            origem.Acrescimo,
            origem.Imposto,
            origem.Juros,
            origem.ContaBancariaId,
            origem.ContaDestinoId,
            origem.CartaoId,
            origem.ValorTotalRateioAmigos,
            origem.TipoRateioAmigos,
            [],
            [],
            []);

        await recorrenciaBackgroundPublisher.PublicarDespesaAsync(mensagem, cancellationToken);
    }

    private static DateTime AvancarData(DateTime data, Recorrencia recorrencia, int repeticoes) =>
        recorrencia switch
        {
            Recorrencia.Diaria => data.AddDays(repeticoes),
            Recorrencia.Semanal => data.AddDays(7 * repeticoes),
            Recorrencia.Quinzenal => data.AddDays(15 * repeticoes),
            Recorrencia.Mensal => data.AddMonths(repeticoes),
            Recorrencia.Trimestral => data.AddMonths(3 * repeticoes),
            Recorrencia.Semestral => data.AddMonths(6 * repeticoes),
            Recorrencia.Anual => data.AddYears(repeticoes),
            _ => data
        };

    private static DateOnly AvancarData(DateOnly data, Recorrencia recorrencia, int repeticoes) =>
        recorrencia switch
        {
            Recorrencia.Diaria => data.AddDays(repeticoes),
            Recorrencia.Semanal => data.AddDays(7 * repeticoes),
            Recorrencia.Quinzenal => data.AddDays(15 * repeticoes),
            Recorrencia.Mensal => data.AddMonths(repeticoes),
            Recorrencia.Trimestral => data.AddMonths(3 * repeticoes),
            Recorrencia.Semestral => data.AddMonths(6 * repeticoes),
            Recorrencia.Anual => data.AddYears(repeticoes),
            _ => data
        };

    private static void ValidarComum(string descricao, DateTime dataLancamento, DateOnly dataVencimento, TipoDespesa tipoDespesa, TipoPagamento tipoPagamento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if ((tipoPagamento != TipoPagamento.CartaoCredito || dataVencimento != default) && dataVencimento < DateOnly.FromDateTime(dataLancamento))
            throw new DomainException("periodo_invalido");
        if (!Enum.IsDefined(tipoDespesa) || !Enum.IsDefined(tipoPagamento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
        if (PagamentoCartao(tipoPagamento) && recorrenciaFixa) throw new DomainException("recorrencia_fixa_invalida");
        if (recorrenciaFixa && recorrencia == Recorrencia.Unica) throw new DomainException("recorrencia_fixa_invalida");
        if (!recorrenciaFixa && recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia <= 0)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
    }

    private static bool PagamentoCartao(TipoPagamento tipoPagamento) =>
        tipoPagamento is TipoPagamento.CartaoCredito or TipoPagamento.CartaoDebito;

    private static bool ContaObrigatoria(TipoPagamento tipoPagamento) =>
        tipoPagamento is TipoPagamento.Pix or TipoPagamento.Transferencia;

    private static int? ResolverQuantidadeRecorrencia(TipoPagamento tipoPagamento, int? quantidadeRecorrencia, int? quantidadeParcelas)
    {
        if (!PagamentoCartao(tipoPagamento))
            return quantidadeRecorrencia;

        var parcelas = quantidadeParcelas;
        if (!parcelas.HasValue || parcelas <= 0)
            throw new DomainException("quantidade_parcelas_invalida");

        return parcelas;
    }

    private static Recorrencia ResolverRecorrencia(TipoPagamento tipoPagamento, Recorrencia recorrencia) =>
        PagamentoCartao(tipoPagamento) ? Recorrencia.Mensal : recorrencia;

    private static bool ResolverRecorrenciaFixa(TipoPagamento tipoPagamento, bool recorrenciaFixa) =>
        PagamentoCartao(tipoPagamento) ? false : recorrenciaFixa;

    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) =>
        valorTotal - desconto + acrescimo + imposto + juros;

    private async Task<(long? ContaBancariaId, long? CartaoId)> ResolverVinculoPagamentoAsync(
        TipoPagamento tipoPagamento,
        long? contaBancariaId,
        long? cartaoId,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (contaBancariaId.HasValue && cartaoId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (PagamentoCartao(tipoPagamento) && contaBancariaId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (PagamentoCartao(tipoPagamento) && !cartaoId.HasValue)
            throw new DomainException("cartao_obrigatorio");

        if (!PagamentoCartao(tipoPagamento) && cartaoId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (ContaObrigatoria(tipoPagamento) && !contaBancariaId.HasValue)
            throw new DomainException("conta_bancaria_obrigatoria");

        if (contaBancariaId.HasValue &&
            await contaRepository.ObterPorIdAsync(contaBancariaId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("conta_bancaria_invalida");

        if (cartaoId.HasValue &&
            await cartaoRepository.ObterPorIdAsync(cartaoId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("cartao_invalido");

        return (contaBancariaId, cartaoId);
    }

    private async Task<long?> ResolverContaDestinoTransferenciaAsync(
        TipoPagamento tipoPagamento,
        long? contaBancariaOrigemId,
        long? contaDestinoId,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (tipoPagamento is not (TipoPagamento.Transferencia or TipoPagamento.Pix))
        {
            if (contaDestinoId.HasValue)
                throw new DomainException("conta_destino_invalida");
            return null;
        }

        if (!contaDestinoId.HasValue)
            return null;

        if (!contaBancariaOrigemId.HasValue || contaDestinoId.Value == contaBancariaOrigemId.Value)
            throw new DomainException("conta_destino_invalida");

        if (await contaRepository.ObterPorIdAsync(contaDestinoId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("conta_bancaria_invalida");

        return contaDestinoId;
    }

    private async Task<Despesa> SincronizarTransacaoEntreContasAsync(
        Despesa origem,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (origem.DespesaOrigemId.HasValue)
            return origem;

        Receita? espelho = null;
        if (origem.ReceitaTransferenciaId.HasValue)
            espelho = await receitaRepository.ObterPorIdAsync(origem.ReceitaTransferenciaId.Value, usuarioAutenticadoId, cancellationToken);

        if (!EhTransacaoEntreContas(origem.TipoPagamento, origem.ContaBancariaId, origem.ContaDestinoId, origem.CartaoId))
        {
            if (espelho is not null && espelho.Status != StatusReceita.Cancelada)
            {
                espelho.Status = StatusReceita.Cancelada;
                espelho.DataEfetivacao = null;
                espelho.ValorEfetivacao = null;
                espelho.Logs.Add(new ReceitaLog
                {
                    ReceitaId = espelho.Id,
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Receita espelhada cancelada por alteracao da transacao de origem."
                });
                await receitaRepository.AtualizarAsync(espelho, cancellationToken);
            }

            if (origem.ReceitaTransferenciaId.HasValue)
            {
                origem.ReceitaTransferenciaId = null;
                origem = await repository.AtualizarAsync(origem, cancellationToken);
            }

            return origem;
        }

        if (espelho is null)
        {
            var receitaRecorrenciaOrigemId = await ResolverReceitaRecorrenciaOrigemIdParaEspelhoAsync(origem, usuarioAutenticadoId, cancellationToken);
            var novoEspelho = CriarReceitaEspelhoTransacaoEntreContas(origem, usuarioAutenticadoId);
            novoEspelho.ReceitaRecorrenciaOrigemId = receitaRecorrenciaOrigemId;
            var espelhoCriado = await receitaRepository.CriarAsync(novoEspelho, cancellationToken);
            if (espelhoCriado.Recorrencia != Recorrencia.Unica && !espelhoCriado.ReceitaRecorrenciaOrigemId.HasValue)
            {
                espelhoCriado.ReceitaRecorrenciaOrigemId = espelhoCriado.Id;
                espelhoCriado = await receitaRepository.AtualizarAsync(espelhoCriado, cancellationToken);
            }

            origem.ReceitaTransferenciaId = espelhoCriado.Id;
            origem = await repository.AtualizarAsync(origem, cancellationToken);
            return origem;
        }

        AplicarSnapshotNoEspelhoReceita(origem, espelho);
        var receitaRecorrenciaOrigemIdAtual = await ResolverReceitaRecorrenciaOrigemIdParaEspelhoAsync(origem, usuarioAutenticadoId, cancellationToken);
        espelho.ReceitaRecorrenciaOrigemId = espelho.Recorrencia == Recorrencia.Unica
            ? null
            : receitaRecorrenciaOrigemIdAtual ?? espelho.Id;
        if (espelho.DespesaTransferenciaId != origem.Id)
            espelho.DespesaTransferenciaId = origem.Id;
        await receitaRepository.AtualizarAsync(espelho, cancellationToken);

        if (origem.ReceitaTransferenciaId != espelho.Id)
        {
            origem.ReceitaTransferenciaId = espelho.Id;
            origem = await repository.AtualizarAsync(origem, cancellationToken);
        }

        return origem;
    }

    private async Task<long?> ResolverReceitaRecorrenciaOrigemIdParaEspelhoAsync(
        Despesa origem,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (origem.Recorrencia == Recorrencia.Unica)
            return null;

        var despesaBaseId = origem.DespesaRecorrenciaOrigemId ?? origem.Id;
        if (despesaBaseId == origem.Id)
            return origem.ReceitaTransferenciaId;

        var receitas = await receitaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        var receitaBase = receitas
            .Where(x => x.ReceitaOrigemId is null && x.DespesaTransferenciaId == despesaBaseId)
            .OrderBy(x => x.Id)
            .FirstOrDefault();

        return receitaBase?.ReceitaRecorrenciaOrigemId ?? receitaBase?.Id;
    }

    private static bool EhTransacaoEntreContas(TipoPagamento tipoPagamento, long? contaBancariaOrigemId, long? contaDestinoId, long? cartaoId) =>
        tipoPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix
        && contaBancariaOrigemId.HasValue
        && contaDestinoId.HasValue
        && !cartaoId.HasValue;

    private static Receita CriarReceitaEspelhoTransacaoEntreContas(Despesa origem, int usuarioAutenticadoId) =>
        new()
        {
            UsuarioCadastroId = origem.UsuarioCadastroId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            Competencia = origem.Competencia,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            DataEfetivacao = origem.DataEfetivacao,
            TipoReceita = TipoReceita.Outros,
            TipoRecebimento = ConverterParaTipoRecebimento(origem.TipoPagamento),
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ValorTotal = origem.ValorTotal,
            ValorLiquido = origem.ValorLiquido,
            Desconto = origem.Desconto,
            Acrescimo = origem.Acrescimo,
            Imposto = origem.Imposto,
            Juros = origem.Juros,
            ValorEfetivacao = origem.ValorEfetivacao,
            Status = ConverterStatusParaReceita(origem.Status),
            ContaBancariaId = origem.ContaDestinoId,
            ContaDestinoId = origem.ContaBancariaId,
            CartaoId = null,
            DespesaTransferenciaId = origem.Id,
            Logs =
            [
                new ReceitaLog
                {
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Receita espelhada criada automaticamente por transacao entre contas."
                }
            ]
        };

    private static void AplicarSnapshotNoEspelhoReceita(Despesa origem, Receita espelho)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.Competencia = origem.Competencia;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.DataEfetivacao = origem.DataEfetivacao;
        espelho.TipoRecebimento = ConverterParaTipoRecebimento(origem.TipoPagamento);
        espelho.Recorrencia = origem.Recorrencia;
        espelho.RecorrenciaFixa = origem.RecorrenciaFixa;
        espelho.QuantidadeRecorrencia = origem.QuantidadeRecorrencia;
        espelho.ValorTotal = origem.ValorTotal;
        espelho.ValorLiquido = origem.ValorLiquido;
        espelho.Desconto = origem.Desconto;
        espelho.Acrescimo = origem.Acrescimo;
        espelho.Imposto = origem.Imposto;
        espelho.Juros = origem.Juros;
        espelho.ValorEfetivacao = origem.ValorEfetivacao;
        espelho.Status = ConverterStatusParaReceita(origem.Status);
        espelho.ContaBancariaId = origem.ContaDestinoId;
        espelho.ContaDestinoId = origem.ContaBancariaId;
        espelho.CartaoId = null;
    }

    private static TipoRecebimento ConverterParaTipoRecebimento(TipoPagamento tipoPagamento) =>
        tipoPagamento switch
        {
            TipoPagamento.Pix => TipoRecebimento.Pix,
            TipoPagamento.Transferencia => TipoRecebimento.Transferencia,
            _ => TipoRecebimento.Transferencia
        };

    private static StatusReceita ConverterStatusParaReceita(StatusDespesa status) =>
        status switch
        {
            StatusDespesa.Pendente => StatusReceita.Pendente,
            StatusDespesa.Efetivada => StatusReceita.Efetivada,
            StatusDespesa.Cancelada => StatusReceita.Cancelada,
            _ => StatusReceita.Pendente
        };

    private async Task ValidarAreasRateioAsync(IReadOnlyCollection<DespesaAreaRateioRequest> areasRateio, CancellationToken cancellationToken)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => x.AreaId <= 0 || x.SubAreaId <= 0))
            throw new DomainException("area_subarea_invalida");

        var subAreas = await areaRepository.ObterSubAreasPorIdsAsync(areasRateio.Select(x => x.SubAreaId).Distinct().ToArray(), cancellationToken);
        var subAreasById = subAreas.ToDictionary(x => x.Id);

        foreach (var item in areasRateio)
        {
            if (!subAreasById.TryGetValue(item.SubAreaId, out var subArea) || subArea.AreaId != item.AreaId)
                throw new DomainException("relacao_area_subarea_invalida");

            if (subArea.Area.Tipo != TipoAreaFinanceira.Despesa)
                throw new DomainException("area_subarea_invalida");
        }
    }

    private static void ValidarRateioAmigos(IReadOnlyCollection<AmigoRateioRequest> amigos, decimal valorTotal)
    {
        if (amigos.Count == 0) return;

        if (amigos.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_amigos_invalido");

        if (amigos.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_amigos_invalido");
    }

    private static decimal ObterValorTotalRateioAmigos(
        IReadOnlyCollection<AmigoRateioRequest>? amigosRateio,
        decimal? valorTotalRateioAmigos,
        decimal valorLiquido)
    {
        if (amigosRateio is null || amigosRateio.Count == 0)
            return 0m;

        if (!valorTotalRateioAmigos.HasValue || valorTotalRateioAmigos.Value <= valorLiquido)
            throw new DomainException("rateio_amigos_invalido");

        return valorTotalRateioAmigos.Value;
    }

    private static TipoRateioAmigos? ObterTipoRateioAmigos(
        IReadOnlyCollection<AmigoRateioRequest>? amigosRateio,
        TipoRateioAmigos? tipoRateioAmigosRequest)
    {
        if (amigosRateio is null || amigosRateio.Count == 0)
            return null;

        if (tipoRateioAmigosRequest.HasValue)
            return tipoRateioAmigosRequest.Value;

        var possuiValorInformado = amigosRateio.Any(x => x.Valor.HasValue);
        var possuiValorNaoInformado = amigosRateio.Any(x => !x.Valor.HasValue);
        if (possuiValorInformado && possuiValorNaoInformado)
            throw new DomainException("rateio_amigos_invalido");

        return possuiValorInformado ? TipoRateioAmigos.Comum : TipoRateioAmigos.Igualitario;
    }

    private static void ValidarRateioAreas(IReadOnlyCollection<DespesaAreaRateioRequest> areasRateio, decimal valorTotal)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_area_invalido");

        if (areasRateio.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_area_invalido");
    }

    private static IReadOnlyCollection<AmigoRateioRequest> NormalizarAmigos(IReadOnlyCollection<AmigoRateioRequest>? amigosRateio, decimal valorTotal)
    {
        if (amigosRateio is null || amigosRateio.Count == 0)
            return [];

        var normalizados = amigosRateio.Where(x => x.AmigoId > 0).ToArray();

        if (normalizados.Length != amigosRateio.Count || normalizados.Select(x => x.AmigoId).Distinct().Count() != normalizados.Length)
            throw new DomainException("rateio_amigos_invalido");

        var possuiValorInformado = normalizados.Any(x => x.Valor.HasValue);
        var possuiValorNaoInformado = normalizados.Any(x => !x.Valor.HasValue);
        if (possuiValorInformado && possuiValorNaoInformado)
            throw new DomainException("rateio_amigos_invalido");

        if (!possuiValorInformado)
            return DistribuirRateioIgualitario(normalizados, valorTotal);

        return normalizados;
    }

    private static IReadOnlyCollection<AmigoRateioRequest> DistribuirRateioIgualitario(IReadOnlyCollection<AmigoRateioRequest> amigos, decimal valorTotal)
    {
        if (amigos.Count == 0)
            return [];

        var valorBase = decimal.Round(valorTotal / amigos.Count, 2, MidpointRounding.AwayFromZero);
        var ajusteUltimo = valorTotal - (valorBase * amigos.Count);

        return amigos
            .Select((x, indice) =>
            {
                var valor = indice == amigos.Count - 1 ? valorBase + ajusteUltimo : valorBase;
                return new AmigoRateioRequest(x.AmigoId, valor);
            })
            .ToArray();
    }

    private async Task<IReadOnlyCollection<AmigoRateioValidado>> ValidarAmigosAceitosAsync(
        IReadOnlyCollection<AmigoRateioRequest> amigos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (amigos.Count == 0)
            return [];

        var amigosIdsAceitos = await amizadeRepository.ListarIdsAmigosAceitosAsync(usuarioAutenticadoId, cancellationToken);
        var amigosAceitos = amigosIdsAceitos.ToHashSet();
        amigosAceitos.Add(usuarioAutenticadoId);

        if (amigos.Any(x => !amigosAceitos.Contains(x.AmigoId)))
            throw new DomainException("amigo_rateio_invalido");

        var usuariosAtivos = await usuarioRepository.ListarAtivosAsync(cancellationToken);
        var usuariosPorId = usuariosAtivos.ToDictionary(x => x.Id);

        if (amigos.Any(x => !usuariosPorId.ContainsKey(x.AmigoId)))
            throw new DomainException("amigo_rateio_invalido");

        return amigos
            .Select(x => new AmigoRateioValidado(x.AmigoId, usuariosPorId[x.AmigoId].Nome, x.Valor!.Value))
            .ToArray();
    }

    private async Task CriarEspelhosRateioAsync(
        Despesa origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var amigosParaEspelho = amigos.Where(x => x.AmigoId != origem.UsuarioCadastroId).ToArray();
        if (amigosParaEspelho.Length == 0)
            return;

        foreach (var amigo in amigosParaEspelho)
        {
            var espelho = CriarEspelhoDespesa(origem, amigo, areasRateioOrigem);
            await repository.CriarAsync(espelho, cancellationToken);
        }
    }

    private async Task SincronizarEspelhosRateioAsync(
        Despesa origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var espelhos = await repository.ListarEspelhosPorOrigemAsync(origem.Id, cancellationToken);
        var amigosParaEspelho = amigos.Where(x => x.AmigoId != origem.UsuarioCadastroId).ToArray();
        if (espelhos.Count == 0 && amigosParaEspelho.Length == 0)
            return;

        var amigosIds = amigosParaEspelho.Select(x => x.AmigoId).ToHashSet();

        foreach (var espelho in espelhos.Where(x => !amigosIds.Contains(x.UsuarioCadastroId) && x.Status != StatusDespesa.Cancelada))
        {
            espelho.Status = StatusDespesa.Cancelada;
            espelho.DataEfetivacao = null;
            espelho.ValorEfetivacao = null;
            espelho.Logs.Add(new DespesaLog
            {
                DespesaId = espelho.Id,
                UsuarioCadastroId = origem.UsuarioCadastroId,
                Acao = AcaoLogs.Atualizacao,
                Descricao = "Rateio removido pelo autor e espelho cancelado."
            });
            await repository.AtualizarAsync(espelho, cancellationToken);
        }

        foreach (var amigo in amigosParaEspelho)
        {
            var espelho = espelhos
                .Where(x => x.UsuarioCadastroId == amigo.AmigoId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (espelho is null || espelho.Status == StatusDespesa.Cancelada)
            {
                await repository.CriarAsync(CriarEspelhoDespesa(origem, amigo, areasRateioOrigem), cancellationToken);
                continue;
            }

            if (espelho.Status == StatusDespesa.PendenteAprovacao || espelho.Status == StatusDespesa.Rejeitado)
            {
                AplicarSnapshotNoEspelho(espelho, origem, amigo, areasRateioOrigem);
                espelho.Status = StatusDespesa.PendenteAprovacao;
                espelho.Logs.Add(new DespesaLog
                {
                    DespesaId = espelho.Id,
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Rateio reenviado para aprovacao."
                });
                await repository.AtualizarAsync(espelho, cancellationToken);
            }
        }
    }

    private static Despesa CriarEspelhoDespesa(
        Despesa origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem)
    {
        return new Despesa
        {
            DespesaOrigemId = origem.Id,
            UsuarioCadastroId = amigo.AmigoId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            TipoDespesa = origem.TipoDespesa,
            TipoPagamento = origem.TipoPagamento,
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ValorTotal = amigo.Valor,
            ValorTotalRateioAmigos = null,
            TipoRateioAmigos = null,
            ValorLiquido = amigo.Valor,
            Desconto = 0m,
            Acrescimo = 0m,
            Imposto = 0m,
            Juros = 0m,
            Status = StatusDespesa.PendenteAprovacao,
            ContaBancariaId = null,
            CartaoId = null,
            Documentos = [],
            AmigosRateio = [],
            AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, amigo.AmigoId),
            Logs =
            [
                new DespesaLog
                {
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Despesa compartilhada aguardando aprovacao."
                }
            ]
        };
    }

    private static void AplicarSnapshotNoEspelho(
        Despesa espelho,
        Despesa origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.TipoDespesa = origem.TipoDespesa;
        espelho.TipoPagamento = origem.TipoPagamento;
        espelho.Recorrencia = origem.Recorrencia;
        espelho.RecorrenciaFixa = origem.RecorrenciaFixa;
        espelho.QuantidadeRecorrencia = origem.QuantidadeRecorrencia;
        espelho.ContaBancariaId = null;
        espelho.CartaoId = null;
        espelho.ValorTotal = amigo.Valor;
        espelho.ValorTotalRateioAmigos = null;
        espelho.TipoRateioAmigos = null;
        espelho.ValorLiquido = amigo.Valor;
        espelho.Desconto = 0m;
        espelho.Acrescimo = 0m;
        espelho.Imposto = 0m;
        espelho.Juros = 0m;
        espelho.DataEfetivacao = null;
        espelho.ValorEfetivacao = null;
        espelho.AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, espelho.UsuarioCadastroId, espelho.Id);
    }

    private static List<DespesaAreaRateio> DistribuirAreasProporcionalmente(
        IReadOnlyCollection<DespesaAreaRateioRequest> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId,
        long? despesaId = null)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem
            .Where(x => x.Valor.HasValue && x.Valor > 0)
            .ToArray();

        if (areas.Length == 0)
            return [];

        var resultado = new List<DespesaAreaRateio>(areas.Length);
        var acumulado = 0m;
        var fator = valorEspelho / valorTotalOrigem;

        for (var i = 0; i < areas.Length; i++)
        {
            var item = areas[i];
            var valor = i == areas.Length - 1
                ? Math.Round(valorEspelho - acumulado, 2, MidpointRounding.AwayFromZero)
                : Math.Round(item.Valor!.Value * fator, 2, MidpointRounding.AwayFromZero);

            if (valor < 0)
                valor = 0;

            acumulado += valor;
            resultado.Add(new DespesaAreaRateio
            {
                DespesaId = despesaId ?? 0,
                UsuarioCadastroId = usuarioCadastroId,
                AreaId = item.AreaId,
                SubAreaId = item.SubAreaId,
                Valor = valor
            });
        }

        return resultado;
    }

    private async Task<List<Documento>> SalvarDocumentosAsync(
        IReadOnlyCollection<DocumentoRequest> documentos,
        int usuarioAutenticadoId,
        long? despesaId = null,
        long? receitaId = null,
        long? reembolsoId = null,
        CancellationToken cancellationToken = default)
    {
        if (documentos.Count == 0)
            return [];

        var salvos = await documentoStorageService.SalvarAsync(documentos, cancellationToken);
        return salvos.Select(x => new Documento
        {
            UsuarioCadastroId = usuarioAutenticadoId,
            NomeArquivo = x.NomeArquivo,
            CaminhoArquivo = x.Caminho,
            ContentType = x.ContentType,
            TamanhoBytes = x.TamanhoBytes,
            DespesaId = despesaId,
            ReceitaId = receitaId,
            ReembolsoId = reembolsoId
        }).ToList();
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        var observacaoNormalizada = observacao?.Trim();
        return string.IsNullOrWhiteSpace(observacaoNormalizada) ? null : observacaoNormalizada;
    }

    private static DespesaListaDto MapLista(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.Competencia,
            despesa.DataLancamento,
            despesa.DataVencimento,
            despesa.DataEfetivacao,
            despesa.TipoDespesa,
            despesa.TipoPagamento,
            despesa.ValorTotal,
            despesa.ValorLiquido,
            despesa.ValorEfetivacao,
            despesa.Status.ToString().ToLowerInvariant(),
            despesa.ContaBancariaId,
            despesa.ContaDestinoId,
            despesa.CartaoId);

    private static bool EhTransacaoEntreContas(Despesa despesa) =>
        despesa.ReceitaTransferenciaId.HasValue ||
        (
            despesa.TipoPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix &&
            despesa.ContaBancariaId.HasValue &&
            despesa.ContaDestinoId.HasValue &&
            !despesa.CartaoId.HasValue
        );

    private static DespesaDto Map(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.Observacao,
            despesa.Competencia,
            despesa.DataLancamento,
            despesa.DataVencimento,
            despesa.DataEfetivacao,
            despesa.TipoDespesa,
            despesa.TipoPagamento,
            despesa.Recorrencia,
            despesa.QuantidadeRecorrencia,
            despesa.RecorrenciaFixa,
            despesa.ValorTotal,
            despesa.ValorTotalRateioAmigos,
            despesa.ValorLiquido,
            despesa.Desconto,
            despesa.Acrescimo,
            despesa.Imposto,
            despesa.Juros,
            despesa.ValorEfetivacao,
            despesa.Status.ToString().ToLowerInvariant(),
            despesa.TipoRateioAmigos,
            despesa.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoId, x.AmigoNome, x.Valor)).ToArray(),
            despesa.AreasRateio.Select(x => new DespesaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            despesa.ContaBancariaId,
            despesa.ContaDestinoId,
            despesa.CartaoId,
            despesa.Documentos.Select(x => new DocumentoDto(x.NomeArquivo, x.CaminhoArquivo, x.ContentType, x.TamanhoBytes)).ToArray(),
            despesa.Logs.Select(x => new DespesaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());

    private static string ResolverCompetencia(string? competencia, DateTime? referencia = null)
    {
        var data = referencia ?? DataHoraBrasil.Agora();

        if (string.IsNullOrWhiteSpace(competencia))
            return new DateTime(data.Year, data.Month, 1).ToString("yyyy-MM");

        var periodo = CompetenciaPeriodoHelper.Resolver(competencia, null, null);
        var competenciaData = periodo.DataInicio?.ToDateTime(TimeOnly.MinValue) ?? new DateTime(data.Year, data.Month, 1);
        return competenciaData.ToString("yyyy-MM");
    }
}
