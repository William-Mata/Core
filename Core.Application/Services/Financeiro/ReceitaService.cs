using Core.Application.Contracts.Financeiro;
using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed partial class ReceitaService(
    IReceitaRepository repository,
    IDespesaRepository despesaRepository,
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

    public async Task<IReadOnlyCollection<ReceitaListaDto>> ListarAsync(ListarReceitasRequest request, CancellationToken cancellationToken = default)
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
            var receitas = await repository.ListarPorUsuarioAsync(
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

            return receitas
                .Where(x => !EhTransacaoEntreContas(x))
                .Where(x => !request.DesconsiderarVinculadosCartaoCredito || !x.FaturaCartaoId.HasValue)
                .Where(x => !request.DesconsiderarCancelados || x.Status != StatusReceita.Cancelada)
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

    public async Task<IReadOnlyCollection<ReceitaDto>> ListarPendentesAprovacaoAsync(CancellationToken cancellationToken = default)
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

    public async Task<ReceitaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada"));

    public async Task<ReceitaDto> CriarAsync(CriarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancariaId, req.CartaoId, usuarioAutenticadoId, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio, req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoRecebimentoAsync(req.TipoRecebimento, req.ContaBancariaId, req.CartaoId, usuarioAutenticadoId, cancellationToken);
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            req.TipoRecebimento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);
        var competencia = ResolverCompetencia(req.Competencia);
        var faturaCartaoId = await ResolverFaturaCartaoIdAsync(vinculo.CartaoId, competencia, usuarioAutenticadoId, cancellationToken);
        var ehLancamentoCartao = vinculo.CartaoId.HasValue;
        var documentos = await SalvarDocumentosAsync(req.Documentos ?? [], usuarioAutenticadoId, cancellationToken: cancellationToken);

        var receita = new Receita
        {
            Descricao = req.Descricao.Trim(),
            Observacao = req.Observacao,
            Competencia = competencia,
            DataLancamento = req.DataLancamento,
            DataVencimento = req.DataVencimento,
            TipoReceita = req.TipoReceita,
            TipoRecebimento = req.TipoRecebimento,
            Recorrencia = req.Recorrencia,
            RecorrenciaFixa = req.RecorrenciaFixa,
            QuantidadeRecorrencia = req.QuantidadeRecorrencia,
            ValorTotal = req.ValorTotal,
            ValorTotalRateioAmigos = amigos.Count == 0 ? null : valorTotalRateioAmigos,
            TipoRateioAmigos = amigos.Count == 0 ? null : tipoRateioAmigos,
            ValorLiquido = liquido,
            Desconto = req.Desconto,
            Acrescimo = req.Acrescimo,
            Imposto = req.Imposto,
            Juros = req.Juros,
            UsuarioCadastroId = usuarioAutenticadoId,
            ContaBancariaId = vinculo.ContaBancariaId,
            ContaDestinoId = contaDestinoId,
            CartaoId = vinculo.CartaoId,
            FaturaCartaoId = faturaCartaoId,
            Status = ehLancamentoCartao ? StatusReceita.Efetivada : StatusReceita.Pendente,
            DataEfetivacao = ehLancamentoCartao ? req.DataLancamento : null,
            ValorEfetivacao = ehLancamentoCartao ? liquido : null,
            Documentos = documentos,
            AmigosRateio = amigosValidados.Select(x => new ReceitaAmigoRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList(),
            AreasRateio = req.AreasSubAreasRateio.Select(x => new ReceitaAreaRateio
            {
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList(),
            Logs =
            [
                new ReceitaLog
                {
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = ehLancamentoCartao
                        ? "Receita criada com status efetivado por lancamento em cartao."
                        : "Receita criada com status pendente."
                }
            ]
        };

        var receitaCriada = await repository.CriarAsync(receita, cancellationToken);
        await CriarEspelhosRateioAsync(receitaCriada, amigosValidados, req.AreasSubAreasRateio, cancellationToken);
        receitaCriada = await SincronizarTransacaoEntreContasAsync(receitaCriada, usuarioAutenticadoId, cancellationToken);
        if (ehLancamentoCartao)
        {
            await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
                TipoTransacaoFinanceira.Receita,
                receitaCriada.Id,
                usuarioAutenticadoId,
                receitaCriada.DataLancamento,
                0m,
                receitaCriada.ValorEfetivacao ?? receitaCriada.ValorLiquido,
                receitaCriada.ValorEfetivacao ?? receitaCriada.ValorLiquido,
                "Efetivacao de receita",
                null,
                receitaCriada.ContaBancariaId,
                receitaCriada.ContaDestinoId,
                receitaCriada.CartaoId,
                receitaCriada.TipoRecebimento,
                cancellationToken: cancellationToken);
        }
        await RecalcularFaturaAsync(receitaCriada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        var alvo = req.RecorrenciaFixa ? 100 : req.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo > 1)
        {
            var mensagem = new ReceitaRecorrenciaBackgroundMessage(
                usuarioAutenticadoId,
                req.Descricao.Trim(),
                req.Observacao,
                receitaCriada.DataHoraCadastro,
                req.DataLancamento,
                req.DataVencimento,
                req.TipoReceita,
                req.TipoRecebimento,
                req.Recorrencia,
                req.RecorrenciaFixa,
                req.RecorrenciaFixa ? 100 : req.QuantidadeRecorrencia,
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
                req.AreasSubAreasRateio.Select(x => new RateioAreaBackgroundMessage(x.AreaId, x.SubAreaId, x.Valor)).ToArray());

            await recorrenciaBackgroundPublisher.PublicarReceitaAsync(mensagem, cancellationToken);
        }

        return Map(receitaCriada);
    }

    public async Task<ReceitaDto> AtualizarAsync(
        long id,
        AtualizarReceitaRequest req,
        EscopoRecorrencia escopoRecorrencia = EscopoRecorrencia.ApenasEssa,
        CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (receita.Status != StatusReceita.Pendente &&
            !(receita.FaturaCartaoId.HasValue && receita.Status == StatusReceita.Efetivada))
            throw new DomainException("status_invalido");
        if (!Enum.IsDefined(escopoRecorrencia)) throw new DomainException("escopo_recorrencia_invalido");
        await ValidarFaturaParaAlteracaoAsync(receita.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        await ValidarComumAsync(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoReceita, req.TipoRecebimento, req.Recorrencia, req.RecorrenciaFixa, req.QuantidadeRecorrencia, req.ValorTotal, req.ContaBancariaId, req.CartaoId, usuarioAutenticadoId, cancellationToken);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio, cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio, req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoRecebimentoAsync(req.TipoRecebimento, req.ContaBancariaId ?? receita.ContaBancariaId, req.CartaoId ?? receita.CartaoId, usuarioAutenticadoId, cancellationToken);
        var contaDestinoIdBase = await ResolverContaDestinoTransferenciaAsync(
            req.TipoRecebimento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId ?? receita.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);

        var serie = await ListarSerieRecorrenteAsync(receita, usuarioAutenticadoId, cancellationToken);
        var faturasOriginais = serie.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).ToHashSet();
        var alvos = SelecionarAlvosPorEscopo(serie, receita, escopoRecorrencia);
        var indicePorId = serie
            .Select((item, indice) => new { item.Id, Indice = indice })
            .ToDictionary(x => x.Id, x => x.Indice);
        var indiceBase = indicePorId.GetValueOrDefault(receita.Id, 0);

        Receita? receitaAtualizada = null;

        foreach (var alvo in alvos)
        {
            var deslocamento = indicePorId.GetValueOrDefault(alvo.Id, indiceBase) - indiceBase;
            var dataLancamentoBase = AvancarData(req.DataLancamento, req.Recorrencia, deslocamento);

            alvo.Descricao = req.Descricao.Trim();
            alvo.Observacao = req.Observacao;
            alvo.Competencia = ResolverCompetencia(req.Competencia, dataLancamentoBase);
            alvo.DataLancamento = dataLancamentoBase;
            alvo.DataVencimento = AvancarData(req.DataVencimento, req.Recorrencia, deslocamento);
            alvo.TipoReceita = req.TipoReceita;
            alvo.TipoRecebimento = req.TipoRecebimento;
            alvo.Recorrencia = req.Recorrencia;
            alvo.RecorrenciaFixa = req.RecorrenciaFixa;
            alvo.QuantidadeRecorrencia = req.QuantidadeRecorrencia;
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
            if (vinculo.CartaoId.HasValue)
            {
                alvo.Status = StatusReceita.Efetivada;
                alvo.DataEfetivacao = alvo.DataLancamento;
                alvo.ValorEfetivacao = liquido;
            }
            if (req.Documentos is not null)
                alvo.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, receitaId: alvo.Id, cancellationToken: cancellationToken);
            alvo.AmigosRateio = amigosValidados.Select(x => new ReceitaAmigoRateio
            {
                ReceitaId = alvo.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                AmigoId = x.AmigoId,
                AmigoNome = x.Nome,
                Valor = x.Valor
            }).ToList();
            alvo.AreasRateio = req.AreasSubAreasRateio.Select(x => new ReceitaAreaRateio
            {
                ReceitaId = alvo.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                AreaId = x.AreaId,
                SubAreaId = x.SubAreaId,
                Valor = x.Valor
            }).ToList();
            alvo.Logs.Add(new ReceitaLog { ReceitaId = alvo.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita atualizada." });

            var atualizado = await repository.AtualizarAsync(alvo, cancellationToken);
            await SincronizarEspelhosRateioAsync(atualizado, amigosValidados, req.AreasSubAreasRateio, cancellationToken);
            atualizado = await SincronizarTransacaoEntreContasAsync(atualizado, usuarioAutenticadoId, cancellationToken);

            if (atualizado.Id == id)
                receitaAtualizada = atualizado;
        }

        var faturasAtualizadas = alvos.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).ToHashSet();
        foreach (var faturaId in faturasOriginais.Union(faturasAtualizadas))
            await RecalcularFaturaAsync(faturaId, usuarioAutenticadoId, cancellationToken);

        return Map(receitaAtualizada ?? receita);
    }

    public async Task<ReceitaDto> AprovarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (!receita.ReceitaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (receita.Status != StatusReceita.PendenteAprovacao) throw new DomainException("status_invalido");

        receita.Status = StatusReceita.Pendente;
        receita.Logs.Add(new ReceitaLog
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio aprovado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(receita, cancellationToken));
    }

    public async Task<ReceitaDto> RejeitarRateioAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        if (!receita.ReceitaOrigemId.HasValue) throw new DomainException("aprovacao_invalida");
        if (receita.Status != StatusReceita.PendenteAprovacao) throw new DomainException("status_invalido");

        receita.Status = StatusReceita.Rejeitado;
        receita.Logs.Add(new ReceitaLog
        {
            ReceitaId = receita.Id,
            UsuarioCadastroId = usuarioAutenticadoId,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Rateio rejeitado pelo amigo."
        });

        return Map(await repository.AtualizarAsync(receita, cancellationToken));
    }

    public async Task<ReceitaDto> EfetivarAsync(long id, EfetivarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(receita.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (receita.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(req.TipoRecebimento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < receita.DataLancamento) throw new DomainException("periodo_invalido");
        var vinculo = await ResolverVinculoRecebimentoAsync(req.TipoRecebimento, req.ContaBancariaId ?? receita.ContaBancariaId, req.CartaoId ?? receita.CartaoId, usuarioAutenticadoId, cancellationToken);
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            req.TipoRecebimento,
            vinculo.ContaBancariaId,
            req.ContaDestinoId ?? receita.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var valorAntesTransacao = receita.ValorEfetivacao ?? 0m;
        receita.DataEfetivacao = req.DataEfetivacao;
        receita.TipoRecebimento = req.TipoRecebimento;
        receita.ContaBancariaId = vinculo.ContaBancariaId;
        receita.ContaDestinoId = contaDestinoId;
        receita.CartaoId = vinculo.CartaoId;
        receita.FaturaCartaoId = await ResolverFaturaCartaoIdAsync(vinculo.CartaoId, receita.Competencia, usuarioAutenticadoId, cancellationToken);
        receita.ValorTotal = req.ValorTotal;
        receita.Desconto = req.Desconto;
        receita.Acrescimo = req.Acrescimo;
        receita.Imposto = req.Imposto;
        receita.Juros = req.Juros;
        receita.ValorLiquido = liquido;
        receita.ValorEfetivacao = liquido;
        receita.Status = StatusReceita.Efetivada;
        if (req.Documentos is not null)
            receita.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, receitaId: receita.Id, cancellationToken: cancellationToken);
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita efetivada." });
        var receitaAtualizada = await repository.AtualizarAsync(receita, cancellationToken);
        receitaAtualizada = await SincronizarTransacaoEntreContasAsync(receitaAtualizada, usuarioAutenticadoId, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Receita,
            receitaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEfetivacao,
            valorAntesTransacao,
            receitaAtualizada.ValorEfetivacao ?? receitaAtualizada.ValorLiquido,
            receitaAtualizada.ValorEfetivacao ?? receitaAtualizada.ValorLiquido,
            "Efetivacao de receita",
            null,
            receitaAtualizada.ContaBancariaId,
            contaDestinoId,
            receitaAtualizada.CartaoId,
            receitaAtualizada.TipoRecebimento,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(req.ObservacaoHistorico),
            transacaoIdEspelho: receitaAtualizada.DespesaTransferenciaId);

        await RecalcularFaturaAsync(receitaAtualizada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        return Map(receitaAtualizada);
    }

    public async Task<ReceitaDto> CancelarAsync(
        long id,
        EscopoRecorrencia escopoRecorrencia = EscopoRecorrencia.ApenasEssa,
        CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(receita.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (receita.FaturaCartaoId.HasValue && receita.Status == StatusReceita.Efetivada)
        {
            if (escopoRecorrencia != EscopoRecorrencia.ApenasEssa)
                throw new DomainException("status_invalido");

            var valorAntesTransacao = receita.ValorEfetivacao ?? receita.ValorLiquido;
            var dataEstorno = DateOnly.FromDateTime(DateTime.Now);
            receita.Status = StatusReceita.Cancelada;
            receita.DataEfetivacao = null;
            receita.ValorEfetivacao = null;
            receita.Logs.Add(new ReceitaLog
            {
                ReceitaId = receita.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                Acao = AcaoLogs.Exclusao,
                Descricao = "Receita estornada e cancelada."
            });

            var cancelada = await repository.AtualizarAsync(receita, cancellationToken);
            cancelada = await SincronizarTransacaoEntreContasAsync(cancelada, usuarioAutenticadoId, cancellationToken);
            await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
                TipoTransacaoFinanceira.Receita,
                cancelada.Id,
                usuarioAutenticadoId,
                dataEstorno,
                valorAntesTransacao,
                valorAntesTransacao,
                0m,
                "Estorno de receita",
                null,
                cancelada.ContaBancariaId,
                cancelada.ContaDestinoId,
                cancelada.CartaoId,
                cancelada.TipoRecebimento,
                cancellationToken: cancellationToken);
            await RecalcularFaturaAsync(cancelada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
            return Map(cancelada);
        }

        if (receita.Status != StatusReceita.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(escopoRecorrencia)) throw new DomainException("escopo_recorrencia_invalido");

        var serie = await ListarSerieRecorrenteAsync(receita, usuarioAutenticadoId, cancellationToken);
        var alvos = SelecionarAlvosPorEscopo(serie, receita, escopoRecorrencia);

        Receita? receitaAtualizada = null;

        foreach (var alvo in alvos)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            alvo.Status = StatusReceita.Cancelada;
            alvo.Logs.Add(new ReceitaLog { ReceitaId = alvo.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Receita cancelada." });
            Receita atualizado;
            try
            {
                atualizado = await repository.AtualizarAsync(alvo, cancellationToken);
                atualizado = await SincronizarTransacaoEntreContasAsync(atualizado, usuarioAutenticadoId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (atualizado.Id == id)
                receitaAtualizada = atualizado;
        }

        if (escopoRecorrencia == EscopoRecorrencia.TodasPendentes && receita.RecorrenciaFixa)
        {
            foreach (var item in serie.Where(x => x.RecorrenciaFixa))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                item.RecorrenciaFixa = false;
                try
                {
                    await repository.AtualizarAsync(item, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        foreach (var faturaId in alvos.Where(x => x.FaturaCartaoId.HasValue).Select(x => x.FaturaCartaoId!.Value).Distinct())
            await RecalcularFaturaAsync(faturaId, usuarioAutenticadoId, cancellationToken);

        return Map(receitaAtualizada ?? receita);
    }

    public async Task<ReceitaDto> EstornarAsync(long id, EstornarReceitaRequest req, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var receita = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("receita_nao_encontrada");
        await ValidarFaturaParaAlteracaoAsync(receita.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);
        if (receita.Status != StatusReceita.Efetivada) throw new DomainException("status_invalido");
        if (req.DataEstorno == default) throw new DomainException("data_estorno_obrigatoria");
        if (req.DataEstorno < receita.DataLancamento) throw new DomainException("periodo_invalido");
        if (receita.DataEfetivacao.HasValue && req.DataEstorno < receita.DataEfetivacao.Value) throw new DomainException("periodo_invalido");
        var contaDestinoId = await ResolverContaDestinoTransferenciaAsync(
            receita.TipoRecebimento,
            receita.ContaBancariaId,
            req.ContaDestinoId ?? receita.ContaDestinoId,
            usuarioAutenticadoId,
            cancellationToken);
        var valorAntesTransacao = receita.ValorEfetivacao ?? receita.ValorLiquido;
        receita.Status = StatusReceita.Pendente;
        receita.DataEfetivacao = null;
        receita.ValorEfetivacao = null;
        receita.Logs.Add(new ReceitaLog { ReceitaId = receita.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Receita estornada." });
        var receitaAtualizada = await repository.AtualizarAsync(receita, cancellationToken);
        receitaAtualizada = await SincronizarTransacaoEntreContasAsync(receitaAtualizada, usuarioAutenticadoId, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Receita,
            receitaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEstorno,
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de receita",
            null,
            receita.ContaBancariaId,
            contaDestinoId,
            receita.CartaoId,
            receita.TipoRecebimento,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(req.ObservacaoHistorico),
            ocultarDoHistorico: req.OcultarDoHistorico,
            transacaoIdEspelho: receitaAtualizada.DespesaTransferenciaId);

        await RecalcularFaturaAsync(receitaAtualizada.FaturaCartaoId, usuarioAutenticadoId, cancellationToken);

        return Map(receitaAtualizada);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private Task<long?> ResolverFaturaCartaoIdAsync(long? cartaoId, string competencia, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.ResolverFaturaIdParaTransacaoCartaoAsync(cartaoId, competencia, usuarioAutenticadoId, cancellationToken) ?? Task.FromResult<long?>(null);

    private Task RecalcularFaturaAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.RecalcularTotalPorFaturaIdAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken) ?? Task.CompletedTask;

    private Task ValidarFaturaParaAlteracaoAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken) =>
        faturaCartaoService?.ValidarFaturaPermiteAlteracaoAsync(faturaCartaoId, usuarioAutenticadoId, cancellationToken) ?? Task.CompletedTask;

    private async Task<List<Receita>> ListarSerieRecorrenteAsync(Receita referencia, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (referencia.Recorrencia == Recorrencia.Unica || referencia.ReceitaOrigemId.HasValue)
            return [referencia];

        var todas = await repository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        var serie = todas
            .Where(x => x.ReceitaOrigemId is null)
            .Where(x => x.Descricao == referencia.Descricao)
            .Where(x => x.TipoReceita == referencia.TipoReceita)
            .Where(x => x.TipoRecebimento == referencia.TipoRecebimento)
            .Where(x => x.Recorrencia == referencia.Recorrencia)
            .Where(x => x.RecorrenciaFixa == referencia.RecorrenciaFixa)
            .Where(x => x.ContaBancariaId == referencia.ContaBancariaId)
            .Where(x => x.CartaoId == referencia.CartaoId)
            .OrderBy(x => x.DataLancamento)
            .ThenBy(x => x.Id)
            .ToList();

        return serie.Any(x => x.Id == referencia.Id) ? serie : [referencia];
    }

    private static List<Receita> SelecionarAlvosPorEscopo(IReadOnlyList<Receita> serie, Receita referencia, EscopoRecorrencia escopoRecorrencia)
    {
        if (escopoRecorrencia == EscopoRecorrencia.ApenasEssa || referencia.Recorrencia == Recorrencia.Unica || referencia.ReceitaOrigemId.HasValue)
            return [referencia];

        var indiceBase = serie
            .Select((item, indice) => new { item.Id, Indice = indice })
            .FirstOrDefault(x => x.Id == referencia.Id)?.Indice ?? -1;

        if (indiceBase < 0)
            return [referencia];

        return escopoRecorrencia switch
        {
            EscopoRecorrencia.EssaEAsProximas => serie.Where((item, indice) => indice >= indiceBase && item.Status == StatusReceita.Pendente).ToList(),
            EscopoRecorrencia.TodasPendentes => serie.Where(item => item.Status == StatusReceita.Pendente).ToList(),
            _ => [referencia]
        };
    }

    private async Task VerificarUltimasRecorrenciasERecuperarFalhasAsync(
        int usuarioAutenticadoId,
        string? competencia,
        CancellationToken cancellationToken)
    {
        var todasReceitas = await repository.ListarPorUsuarioAsync(
            usuarioAutenticadoId,
            null,
            null,
            null,
            null,
            null,
            cancellationToken);

        var candidatas = todasReceitas
            .Where(x => x.ReceitaOrigemId is null)
            .Where(x => x.Recorrencia != Recorrencia.Unica)
            .GroupBy(x => new
            {
                x.Descricao,
                x.TipoReceita,
                x.TipoRecebimento,
                x.Recorrencia,
                x.RecorrenciaFixa,
                x.ContaBancariaId,
                x.CartaoId
            })
            .ToArray();

        var periodoCompetencia = CompetenciaPeriodoHelper.Resolver(competencia, null, null);

        foreach (var grupo in candidatas)
        {
            var origem = grupo
                .OrderBy(x => x.DataLancamento)
                .ThenBy(x => x.Id)
                .First();

            var alvoBase = origem.RecorrenciaFixa
                ? Math.Max(100, grupo.Max(x => x.QuantidadeRecorrencia.GetValueOrDefault(1)))
                : origem.QuantidadeRecorrencia.GetValueOrDefault(1);

            if (alvoBase <= 1)
                continue;

            if (SeriePossuiLacuna(grupo, origem, alvoBase))
            {
                await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, alvoBase, cancellationToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(competencia))
                continue;

            if (!origem.RecorrenciaFixa)
                continue;

            var dataUltima = AvancarData(origem.DataLancamento, origem.Recorrencia, alvoBase - 1);
            if (dataUltima < periodoCompetencia.DataInicio || dataUltima > periodoCompetencia.DataFim)
                continue;

            await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, alvoBase + 100, cancellationToken);
        }
    }

    private static bool SeriePossuiLacuna(IEnumerable<Receita> grupo, Receita origem, int alvo)
    {
        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamentoEsperada = AvancarData(origem.DataLancamento, origem.Recorrencia, numero - 1);
            var dataVencimentoEsperada = AvancarData(origem.DataVencimento, origem.Recorrencia, numero - 1);

            var existe = grupo.Any(x =>
                x.DataLancamento == dataLancamentoEsperada &&
                x.DataVencimento == dataVencimentoEsperada);

            if (!existe)
                return true;
        }

        return false;
    }

    private async Task PublicarRecorrenciaDaOrigemAsync(int usuarioAutenticadoId, Receita origem, int quantidadeRecorrencia, CancellationToken cancellationToken)
    {
        var mensagem = new ReceitaRecorrenciaBackgroundMessage(
            usuarioAutenticadoId,
            origem.Descricao,
            origem.Observacao,
            origem.DataHoraCadastro,
            origem.DataLancamento,
            origem.DataVencimento,
            origem.TipoReceita,
            origem.TipoRecebimento,
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

        await recorrenciaBackgroundPublisher.PublicarReceitaAsync(mensagem, cancellationToken);
    }

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

    private async Task ValidarComumAsync(string descricao, DateOnly dataLancamento, DateOnly dataVencimento, TipoReceita tipoReceita, TipoRecebimento tipoRecebimento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal, long? contaBancariaId, long? cartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVencimento < dataLancamento) throw new DomainException("periodo_invalido");
        if (!Enum.IsDefined(tipoReceita) || !Enum.IsDefined(tipoRecebimento) || !Enum.IsDefined(recorrencia)) throw new DomainException("enum_invalida");
        if (recorrenciaFixa && recorrencia == Recorrencia.Unica) throw new DomainException("recorrencia_fixa_invalida");
        if (!recorrenciaFixa && recorrencia is not Recorrencia.Unica && (!quantidadeRecorrencia.HasValue || quantidadeRecorrencia <= 0))
            throw new DomainException("quantidade_recorrencia_invalida");
        if (recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia <= 0)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (!recorrenciaFixa && quantidadeRecorrencia.HasValue && quantidadeRecorrencia > 100)
            throw new DomainException("quantidade_recorrencia_invalida");
        if (ContaObrigatoria(tipoRecebimento) && !contaBancariaId.HasValue)
            throw new DomainException("conta_bancaria_obrigatoria");
        if (PagamentoCartao(tipoRecebimento) && !cartaoId.HasValue)
            throw new DomainException("cartao_obrigatorio");
        if (contaBancariaId.HasValue &&
            await contaRepository.ObterPorIdAsync(contaBancariaId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("conta_bancaria_invalida");
        if (cartaoId.HasValue &&
            await cartaoRepository.ObterPorIdAsync(cartaoId.Value, usuarioAutenticadoId, cancellationToken) is null)
            throw new DomainException("cartao_invalido");
    }

    private async Task ValidarAreasRateioAsync(IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateio, CancellationToken cancellationToken)
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

            if (subArea.Area.Tipo != TipoAreaFinanceira.Receita)
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

    private static void ValidarRateioAreas(IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateio, decimal valorTotal)
    {
        if (areasRateio.Count == 0) return;

        if (areasRateio.Any(x => !x.Valor.HasValue || x.Valor <= 0))
            throw new DomainException("rateio_area_invalido");

        if (areasRateio.Sum(x => x.Valor!.Value) != valorTotal)
            throw new DomainException("rateio_area_invalido");
    }

    private static bool ContaObrigatoria(TipoRecebimento tipoRecebimento) =>
        tipoRecebimento is TipoRecebimento.Pix or TipoRecebimento.Transferencia;

    private static bool PagamentoCartao(TipoRecebimento tipoRecebimento) =>
        tipoRecebimento is TipoRecebimento.CartaoCredito or TipoRecebimento.CartaoDebito;

    private static decimal Liquido(decimal valorTotal, decimal desconto, decimal acrescimo, decimal imposto, decimal juros) => valorTotal - desconto + acrescimo + imposto + juros;

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
        Receita origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var amigosParaEspelho = amigos.Where(x => x.AmigoId != origem.UsuarioCadastroId).ToArray();
        if (amigosParaEspelho.Length == 0)
            return;

        foreach (var amigo in amigosParaEspelho)
        {
            var espelho = CriarEspelhoReceita(origem, amigo, areasRateioOrigem);
            await repository.CriarAsync(espelho, cancellationToken);
        }
    }

    private async Task SincronizarEspelhosRateioAsync(
        Receita origem,
        IReadOnlyCollection<AmigoRateioValidado> amigos,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        CancellationToken cancellationToken)
    {
        var espelhos = await repository.ListarEspelhosPorOrigemAsync(origem.Id, cancellationToken);
        var amigosParaEspelho = amigos.Where(x => x.AmigoId != origem.UsuarioCadastroId).ToArray();
        if (espelhos.Count == 0 && amigosParaEspelho.Length == 0)
            return;

        var amigosIds = amigosParaEspelho.Select(x => x.AmigoId).ToHashSet();

        foreach (var espelho in espelhos.Where(x => !amigosIds.Contains(x.UsuarioCadastroId) && x.Status != StatusReceita.Cancelada))
        {
            espelho.Status = StatusReceita.Cancelada;
            espelho.DataEfetivacao = null;
            espelho.ValorEfetivacao = null;
            espelho.Logs.Add(new ReceitaLog
            {
                ReceitaId = espelho.Id,
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

            if (espelho is null || espelho.Status == StatusReceita.Cancelada)
            {
                await repository.CriarAsync(CriarEspelhoReceita(origem, amigo, areasRateioOrigem), cancellationToken);
                continue;
            }

            if (espelho.Status == StatusReceita.PendenteAprovacao || espelho.Status == StatusReceita.Rejeitado)
            {
                AplicarSnapshotNoEspelho(espelho, origem, amigo, areasRateioOrigem);
                espelho.Status = StatusReceita.PendenteAprovacao;
                espelho.Logs.Add(new ReceitaLog
                {
                    ReceitaId = espelho.Id,
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Rateio reenviado para aprovacao."
                });
                await repository.AtualizarAsync(espelho, cancellationToken);
            }
        }
    }

    private static Receita CriarEspelhoReceita(
        Receita origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem)
    {
        return new Receita
        {
            ReceitaOrigemId = origem.Id,
            UsuarioCadastroId = amigo.AmigoId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            TipoReceita = origem.TipoReceita,
            TipoRecebimento = origem.TipoRecebimento,
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
            Status = StatusReceita.PendenteAprovacao,
            ContaBancariaId = null,
            CartaoId = null,
            Documentos = [],
            AmigosRateio = [],
            AreasRateio = DistribuirAreasProporcionalmente(areasRateioOrigem, origem.ValorTotal, amigo.Valor, amigo.AmigoId),
            Logs =
            [
                new ReceitaLog
                {
                    UsuarioCadastroId = origem.UsuarioCadastroId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Receita compartilhada aguardando aprovacao."
                }
            ]
        };
    }

    private static void AplicarSnapshotNoEspelho(
        Receita espelho,
        Receita origem,
        AmigoRateioValidado amigo,
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.TipoReceita = origem.TipoReceita;
        espelho.TipoRecebimento = origem.TipoRecebimento;
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

    private static List<ReceitaAreaRateio> DistribuirAreasProporcionalmente(
        IReadOnlyCollection<ReceitaAreaRateioRequest> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId,
        long? receitaId = null)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem.Where(x => x.Valor.HasValue && x.Valor > 0).ToArray();
        if (areas.Length == 0)
            return [];

        var resultado = new List<ReceitaAreaRateio>(areas.Length);
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
            resultado.Add(new ReceitaAreaRateio
            {
                ReceitaId = receitaId ?? 0,
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

    private async Task<(long? ContaBancariaId, long? CartaoId)> ResolverVinculoRecebimentoAsync(
        TipoRecebimento tipoRecebimento,
        long? contaBancariaId,
        long? cartaoId,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (contaBancariaId.HasValue && cartaoId.HasValue)
            throw new DomainException("forma_pagamento_invalida");

        if (PagamentoCartao(tipoRecebimento))
        {
            if (!cartaoId.HasValue)
                throw new DomainException("cartao_obrigatorio");
            if (contaBancariaId.HasValue)
                throw new DomainException("forma_pagamento_invalida");
        }
        else if (cartaoId.HasValue)
        {
            throw new DomainException("forma_pagamento_invalida");
        }

        if (ContaObrigatoria(tipoRecebimento) && !contaBancariaId.HasValue)
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
        TipoRecebimento tipoRecebimento,
        long? contaBancariaOrigemId,
        long? contaDestinoId,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (tipoRecebimento is not (TipoRecebimento.Transferencia or TipoRecebimento.Pix))
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

    private async Task<Receita> SincronizarTransacaoEntreContasAsync(
        Receita origem,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        if (origem.ReceitaOrigemId.HasValue)
            return origem;

        Despesa? espelho = null;
        if (origem.DespesaTransferenciaId.HasValue)
            espelho = await despesaRepository.ObterPorIdAsync(origem.DespesaTransferenciaId.Value, usuarioAutenticadoId, cancellationToken);

        if (!EhTransacaoEntreContas(origem.TipoRecebimento, origem.ContaBancariaId, origem.ContaDestinoId, origem.CartaoId))
        {
            if (espelho is not null && espelho.Status != StatusDespesa.Cancelada)
            {
                espelho.Status = StatusDespesa.Cancelada;
                espelho.DataEfetivacao = null;
                espelho.ValorEfetivacao = null;
                espelho.Logs.Add(new DespesaLog
                {
                    DespesaId = espelho.Id,
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Atualizacao,
                    Descricao = "Despesa espelhada cancelada por alteracao da transacao de origem."
                });
                await despesaRepository.AtualizarAsync(espelho, cancellationToken);
            }

            if (origem.DespesaTransferenciaId.HasValue)
            {
                origem.DespesaTransferenciaId = null;
                origem = await repository.AtualizarAsync(origem, cancellationToken);
            }

            return origem;
        }

        if (espelho is null)
        {
            var novoEspelho = CriarDespesaEspelhoTransacaoEntreContas(origem, usuarioAutenticadoId);
            var espelhoCriado = await despesaRepository.CriarAsync(novoEspelho, cancellationToken);
            origem.DespesaTransferenciaId = espelhoCriado.Id;
            origem = await repository.AtualizarAsync(origem, cancellationToken);
            return origem;
        }

        AplicarSnapshotNoEspelhoDespesa(origem, espelho);
        if (espelho.ReceitaTransferenciaId != origem.Id)
            espelho.ReceitaTransferenciaId = origem.Id;
        await despesaRepository.AtualizarAsync(espelho, cancellationToken);

        if (origem.DespesaTransferenciaId != espelho.Id)
        {
            origem.DespesaTransferenciaId = espelho.Id;
            origem = await repository.AtualizarAsync(origem, cancellationToken);
        }

        return origem;
    }

    private static bool EhTransacaoEntreContas(TipoRecebimento tipoRecebimento, long? contaBancariaOrigemId, long? contaDestinoId, long? cartaoId) =>
        tipoRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix
        && contaBancariaOrigemId.HasValue
        && contaDestinoId.HasValue
        && !cartaoId.HasValue;

    private static Despesa CriarDespesaEspelhoTransacaoEntreContas(Receita origem, int usuarioAutenticadoId) =>
        new()
        {
            UsuarioCadastroId = origem.UsuarioCadastroId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            DataEfetivacao = origem.DataEfetivacao,
            TipoDespesa = TipoDespesa.Outros,
            TipoPagamento = ConverterParaTipoPagamento(origem.TipoRecebimento),
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
            Status = ConverterStatusParaDespesa(origem.Status),
            ContaBancariaId = origem.ContaDestinoId,
            ContaDestinoId = origem.ContaBancariaId,
            CartaoId = null,
            ReceitaTransferenciaId = origem.Id,
            Logs =
            [
                new DespesaLog
                {
                    UsuarioCadastroId = usuarioAutenticadoId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Despesa espelhada criada automaticamente por transacao entre contas."
                }
            ]
        };

    private static void AplicarSnapshotNoEspelhoDespesa(Receita origem, Despesa espelho)
    {
        espelho.Descricao = origem.Descricao;
        espelho.Observacao = origem.Observacao;
        espelho.DataLancamento = origem.DataLancamento;
        espelho.DataVencimento = origem.DataVencimento;
        espelho.DataEfetivacao = origem.DataEfetivacao;
        espelho.TipoPagamento = ConverterParaTipoPagamento(origem.TipoRecebimento);
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
        espelho.Status = ConverterStatusParaDespesa(origem.Status);
        espelho.ContaBancariaId = origem.ContaDestinoId;
        espelho.ContaDestinoId = origem.ContaBancariaId;
        espelho.CartaoId = null;
    }

    private static TipoPagamento ConverterParaTipoPagamento(TipoRecebimento tipoRecebimento) =>
        tipoRecebimento switch
        {
            TipoRecebimento.Pix => TipoPagamento.Pix,
            TipoRecebimento.Transferencia => TipoPagamento.Transferencia,
            _ => TipoPagamento.Transferencia
        };

    private static StatusDespesa ConverterStatusParaDespesa(StatusReceita status) =>
        status switch
        {
            StatusReceita.Pendente => StatusDespesa.Pendente,
            StatusReceita.Efetivada => StatusDespesa.Efetivada,
            StatusReceita.Cancelada => StatusDespesa.Cancelada,
            _ => StatusDespesa.Pendente
        };

    private static string? NormalizarObservacao(string? observacao)
    {
        var observacaoNormalizada = observacao?.Trim();
        return string.IsNullOrWhiteSpace(observacaoNormalizada) ? null : observacaoNormalizada;
    }

    private static ReceitaListaDto MapLista(Receita receita) =>
        new(
            receita.Id,
            receita.Descricao,
            receita.Competencia,
            receita.DataLancamento,
            receita.DataVencimento,
            receita.DataEfetivacao,
            receita.TipoReceita,
            receita.TipoRecebimento,
            receita.ValorTotal,
            receita.ValorLiquido,
            receita.ValorEfetivacao,
            receita.Status.ToString().ToLowerInvariant(),
            receita.ContaBancariaId,
            receita.ContaDestinoId,
            receita.CartaoId);

    private static bool EhTransacaoEntreContas(Receita receita) =>
        receita.DespesaTransferenciaId.HasValue ||
        (
            receita.TipoRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix &&
            receita.ContaBancariaId.HasValue &&
            receita.ContaDestinoId.HasValue &&
            !receita.CartaoId.HasValue
        );

    private static ReceitaDto Map(Receita receita) =>
        new(
            receita.Id,
            receita.Descricao,
            receita.Observacao,
            receita.Competencia,
            receita.DataLancamento,
            receita.DataVencimento,
            receita.DataEfetivacao,
            receita.TipoReceita,
            receita.TipoRecebimento,
            receita.Recorrencia,
            receita.QuantidadeRecorrencia,
            receita.RecorrenciaFixa,
            receita.ValorTotal,
            receita.ValorTotalRateioAmigos,
            receita.ValorLiquido,
            receita.Desconto,
            receita.Acrescimo,
            receita.Imposto,
            receita.Juros,
            receita.ValorEfetivacao,
            receita.Status.ToString().ToLowerInvariant(),
            receita.AmigosRateio.Select(x => new AmigoRateioDto(x.AmigoId, x.AmigoNome, x.Valor)).ToArray(),
            receita.AreasRateio.Select(x => new ReceitaAreaRateioDto(
                x.AreaId,
                x.Area?.Nome ?? string.Empty,
                x.SubAreaId,
                x.SubArea?.Nome ?? string.Empty,
                x.Valor)).ToArray(),
            receita.ContaBancariaId,
            receita.ContaDestinoId,
            receita.CartaoId,
            receita.Documentos.Select(x => new DocumentoDto(x.NomeArquivo, x.CaminhoArquivo, x.ContentType, x.TamanhoBytes)).ToArray(),
            receita.Logs.Select(x => new ReceitaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());

    private static string ResolverCompetencia(string? competencia, DateOnly? referencia = null)
    {
        var data = referencia?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;

        if (string.IsNullOrWhiteSpace(competencia))
            return new DateTime(data.Year, data.Month, 1).ToString("yyyy-MM");

        var periodo = CompetenciaPeriodoHelper.Resolver(competencia, null, null);
        var competenciaData = periodo.DataInicio?.ToDateTime(TimeOnly.MinValue) ?? new DateTime(data.Year, data.Month, 1);
        return competenciaData.ToString("yyyy-MM");
    }
}
