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

public sealed partial class DespesaService(
    IDespesaRepository repository,
    IContaBancariaRepository contaRepository,
    ICartaoRepository cartaoRepository,
    IAreaRepository areaRepository,
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IDocumentoStorageService documentoStorageService,
    IRecorrenciaBackgroundPublisher recorrenciaBackgroundPublisher)
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
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.Vinculo, null, null, usuarioAutenticadoId, cancellationToken);
        var documentos = await SalvarDocumentosAsync(req.Documentos ?? [], usuarioAutenticadoId, cancellationToken: cancellationToken);

        var despesa = new Despesa
        {
            Descricao = req.Descricao.Trim(),
            Observacao = req.Observacao,
            DataLancamento = req.DataLancamento,
            DataVencimento = req.DataVencimento,
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
            CartaoId = vinculo.CartaoId,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusDespesa.Pendente,
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
            Logs = [new DespesaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Despesa criada com status pendente." }]
        };

        var despesaCriada = await repository.CriarAsync(despesa, cancellationToken);
        await CriarEspelhosRateioAsync(despesaCriada, amigosValidados, req.AreasSubAreasRateio ?? [], cancellationToken);

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
                req.DataVencimento,
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
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(escopoRecorrencia)) throw new DomainException("escopo_recorrencia_invalido");

        var quantidadeRecorrencia = ResolverQuantidadeRecorrencia(req.TipoPagamento, req.QuantidadeRecorrencia, req.QuantidadeParcelas);
        var recorrencia = ResolverRecorrencia(req.TipoPagamento, req.Recorrencia);
        var recorrenciaFixa = ResolverRecorrenciaFixa(req.TipoPagamento, req.RecorrenciaFixa);
        ValidarComum(req.Descricao, req.DataLancamento, req.DataVencimento, req.TipoDespesa, req.TipoPagamento, recorrencia, recorrenciaFixa, quantidadeRecorrencia, req.ValorTotal);
        await ValidarAreasRateioAsync(req.AreasSubAreasRateio ?? [], cancellationToken);
        var liquido = Liquido(req.ValorTotal, req.Desconto, req.Acrescimo, req.Imposto, req.Juros);
        var tipoRateioAmigos = ObterTipoRateioAmigos(req.AmigosRateio, req.TipoRateioAmigos);
        var valorTotalRateioAmigos = ObterValorTotalRateioAmigos(req.AmigosRateio, req.ValorTotalRateioAmigos, liquido);
        var amigos = NormalizarAmigos(req.AmigosRateio, valorTotalRateioAmigos);
        ValidarRateioAmigos(amigos, valorTotalRateioAmigos);
        ValidarRateioAreas(req.AreasSubAreasRateio ?? [], req.ValorTotal);
        var amigosValidados = await ValidarAmigosAceitosAsync(amigos, usuarioAutenticadoId, cancellationToken);
        var vinculo = await ResolverVinculoPagamentoAsync(req.TipoPagamento, req.Vinculo, despesa.ContaBancariaId, despesa.CartaoId, usuarioAutenticadoId, cancellationToken);

        var serie = await ListarSerieRecorrenteAsync(despesa, usuarioAutenticadoId, cancellationToken);
        var alvos = SelecionarAlvosPorEscopo(serie, despesa, escopoRecorrencia);
        var indicePorId = serie
            .Select((item, indice) => new { item.Id, Indice = indice })
            .ToDictionary(x => x.Id, x => x.Indice);
        var indiceBase = indicePorId.GetValueOrDefault(despesa.Id, 0);

        Despesa? despesaAtualizada = null;

        foreach (var alvo in alvos)
        {
            var deslocamento = indicePorId.GetValueOrDefault(alvo.Id, indiceBase) - indiceBase;

            alvo.Descricao = req.Descricao.Trim();
            alvo.Observacao = req.Observacao;
            alvo.DataLancamento = AvancarData(req.DataLancamento, recorrencia, deslocamento);
            alvo.DataVencimento = AvancarData(req.DataVencimento, recorrencia, deslocamento);
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
            alvo.CartaoId = vinculo.CartaoId;
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

            if (atualizado.Id == id)
                despesaAtualizada = atualizado;
        }

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
        if (despesa.Status != StatusDespesa.Pendente) throw new DomainException("status_invalido");
        if (!Enum.IsDefined(req.TipoPagamento) || req.ValorTotal <= 0) throw new DomainException("dados_invalidos");
        if (req.DataEfetivacao < despesa.DataLancamento) throw new DomainException("periodo_invalido");
        if (req.ContaBancariaId.HasValue && req.CartaoId.HasValue) throw new DomainException("forma_pagamento_invalida");
        var vinculo = await ResolverVinculoPagamentoAsync(
            req.TipoPagamento,
            req.Vinculo,
            req.ContaBancariaId ?? despesa.ContaBancariaId,
            req.CartaoId ?? despesa.CartaoId,
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
        despesa.CartaoId = vinculo.CartaoId;
        despesa.ValorLiquido = liquido;
        despesa.ValorEfetivacao = liquido;
        despesa.Status = StatusDespesa.Efetivada;
        if (req.Documentos is not null)
            despesa.Documentos = await SalvarDocumentosAsync(req.Documentos, usuarioAutenticadoId, despesa.Id, cancellationToken: cancellationToken);
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa efetivada." });

        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            req.DataEfetivacao,
            valorAntesTransacao,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            despesaAtualizada.ValorEfetivacao ?? despesaAtualizada.ValorLiquido,
            "Efetivacao de despesa",
            despesaAtualizada.TipoPagamento,
            despesaAtualizada.ContaBancariaId,
            despesaAtualizada.CartaoId,
            cancellationToken: cancellationToken);

        return Map(despesaAtualizada);
    }

    public async Task<DespesaDto> CancelarAsync(
        long id,
        EscopoRecorrencia escopoRecorrencia = EscopoRecorrencia.ApenasEssa,
        CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
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

        return Map(despesaAtualizada ?? despesa);
    }

    public async Task<DespesaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var despesa = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("despesa_nao_encontrada");
        if (despesa.Status != StatusDespesa.Efetivada) throw new DomainException("status_invalido");
        var valorAntesTransacao = despesa.ValorEfetivacao ?? despesa.ValorLiquido;
        despesa.Status = StatusDespesa.Pendente;
        despesa.DataEfetivacao = null;
        despesa.ValorEfetivacao = null;
        despesa.Logs.Add(new DespesaLog { DespesaId = despesa.Id, UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Despesa estornada." });
        var despesaAtualizada = await repository.AtualizarAsync(despesa, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaAtualizada.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de despesa",
            despesa.TipoPagamento,
            cancellationToken: cancellationToken);

        return Map(despesaAtualizada);
    }

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
            .GroupBy(x => new
            {
                ChaveSerie = x.DespesaRecorrenciaOrigemId ?? x.Id,
                x.DataHoraCadastro
            })
            .ToArray();

        var periodoCompetencia = CompetenciaPeriodoHelper.Resolver(competencia, null, null);

        foreach (var grupo in candidatas)
        {
            var origem = grupo
                .OrderBy(x => x.DataLancamento)
                .ThenBy(x => x.Id)
                .First();

            var alvoBase = grupo.Max(x => x.QuantidadeRecorrencia.GetValueOrDefault(1));
            if (origem.RecorrenciaFixa)
                alvoBase = Math.Max(100, alvoBase);

            if (alvoBase <= 1)
                continue;

            if (SeriePossuiLacuna(grupo, origem, alvoBase))
            {
                await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, alvoBase, cancellationToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(competencia))
                continue;

            if (!origem.RecorrenciaFixa && alvoBase >= 100)
                continue;

            var dataUltima = AvancarData(origem.DataLancamento, origem.Recorrencia, alvoBase - 1);
            if (dataUltima < periodoCompetencia.DataInicio || dataUltima > periodoCompetencia.DataFim)
                continue;

            var novaQuantidade = origem.RecorrenciaFixa
                ? alvoBase + 100
                : Math.Min(100, alvoBase + 10);

            await PublicarRecorrenciaDaOrigemAsync(usuarioAutenticadoId, origem, novaQuantidade, cancellationToken);
        }
    }

    private static bool SeriePossuiLacuna(IEnumerable<Despesa> grupo, Despesa origem, int alvo)
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
            origem.CartaoId,
            origem.ValorTotalRateioAmigos,
            origem.TipoRateioAmigos,
            [],
            [],
            []);

        await recorrenciaBackgroundPublisher.PublicarDespesaAsync(mensagem, cancellationToken);
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

    private static void ValidarComum(string descricao, DateOnly dataLancamento, DateOnly dataVencimento, TipoDespesa tipoDespesa, TipoPagamento tipoPagamento, Recorrencia recorrencia, bool recorrenciaFixa, int? quantidadeRecorrencia, decimal valorTotal)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("descricao_obrigatoria");
        if (valorTotal <= 0) throw new DomainException("valor_total_invalido");
        if (dataVencimento < dataLancamento) throw new DomainException("periodo_invalido");
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

        var parcelas = quantidadeParcelas ?? quantidadeRecorrencia;
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
        MeioFinanceiroVinculoRequest? vinculo,
        long? contaBancariaIdLegado,
        long? cartaoIdLegado,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var contaBancariaId = vinculo?.ContaBancariaId ?? contaBancariaIdLegado;
        var cartaoId = vinculo?.CartaoId ?? cartaoIdLegado;

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

    private static DespesaListaDto MapLista(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.DataLancamento,
            despesa.DataVencimento,
            despesa.DataEfetivacao,
            despesa.TipoDespesa,
            despesa.TipoPagamento,
            despesa.ValorTotal,
            despesa.ValorLiquido,
            despesa.ValorEfetivacao,
            despesa.Status.ToString().ToLowerInvariant(),
            new MeioFinanceiroVinculoDto(despesa.ContaBancariaId, despesa.CartaoId));

    private static DespesaDto Map(Despesa despesa) =>
        new(
            despesa.Id,
            despesa.Descricao,
            despesa.Observacao,
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
            despesa.CartaoId,
            despesa.Documentos.Select(x => new DocumentoDto(x.NomeArquivo, x.CaminhoArquivo, x.ContentType, x.TamanhoBytes)).ToArray(),
            new MeioFinanceiroVinculoDto(despesa.ContaBancariaId, despesa.CartaoId),
            despesa.Logs.Select(x => new DespesaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
