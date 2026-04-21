using Core.Application.DTOs.Financeiro;
using Core.Application.Contracts.Financeiro;
using Core.Domain.Common;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class FaturaCartaoService(
    IFaturaCartaoRepository repository,
    ICartaoRepository cartaoRepository,
    IContaBancariaRepository contaBancariaRepository,
    IDespesaRepository despesaRepository,
    IReceitaRepository receitaRepository,
    IReembolsoRepository reembolsoRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService,
    IFaturaCartaoBackgroundPublisher? faturaCartaoBackgroundPublisher = null)
{
    private const int DiasAntesVencimentoParaFechamento = 7;

    public async Task<IReadOnlyCollection<FaturaCartaoListaDto>> ListarAsync(ListarFaturasCartaoRequest request, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var competencia = ResolverCompetenciaObrigatoria(request.Competencia);
        var faturas = await repository.ListarPorUsuarioAsync(usuarioAutenticadoId, request.CartaoId, competencia, cancellationToken);

        foreach (var fatura in faturas)
        {
            await AplicarFechamentoAutomaticoAsync(fatura, usuarioAutenticadoId, cancellationToken);
            await RecalcularTotalInternoAsync(fatura, usuarioAutenticadoId, cancellationToken);
        }

        return faturas.Select(MapLista).ToArray();
    }

    public async Task<IReadOnlyCollection<FaturaCartaoDetalheDto>> ListarDetalhesAsync(ListarFaturasCartaoDetalheRequest request, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return [];

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var competencia = ResolverCompetenciaObrigatoria(request.Competencia);
        await SolicitarGarantiaESaneamentoBackgroundAsync(usuarioAutenticadoId, competencia, cancellationToken);
        var faturas = await repository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, competencia, cancellationToken);
        var detalhes = new List<FaturaCartaoDetalheDto>(faturas.Count);

        foreach (var fatura in faturas)
        {
            await AplicarFechamentoAutomaticoAsync(fatura, usuarioAutenticadoId, cancellationToken);
            await RecalcularTotalInternoAsync(fatura, usuarioAutenticadoId, cancellationToken);

            var lancamentos = await ListarLancamentosInternoAsync(fatura.Id, usuarioAutenticadoId, request.TipoTransacao, cancellationToken);
            detalhes.Add(MapDetalhe(fatura, lancamentos));
        }

        return detalhes;
    }

    public async Task ProcessarGarantiaESaneamentoAsync(int usuarioAutenticadoId, string competencia, CancellationToken cancellationToken = default)
    {
        var competenciaNormalizada = ResolverCompetenciaObrigatoria(competencia);
        await GarantirFaturasESanearTransacoesCartaoAsync(usuarioAutenticadoId, competenciaNormalizada, cancellationToken);
    }

    public async Task<FaturaCartaoListaDto> EfetivarAsync(long id, EfetivarFaturaCartaoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var fatura = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("fatura_nao_encontrada");
        ValidarStatusEfetivacao(fatura.Status);

        await AplicarFechamentoAutomaticoAsync(fatura, usuarioAutenticadoId, cancellationToken);
        await RecalcularTotalInternoAsync(fatura, usuarioAutenticadoId, cancellationToken);
        await ValidarTransacoesDaFaturaEfetivadasAsync(fatura.Id, usuarioAutenticadoId, cancellationToken);

        if (fatura.ValorTotal <= 0m)
        {
            var dataEfetivacaoSemDespesa = request.DataEfetivacao == default ? DataHoraBrasil.Agora() : request.DataEfetivacao;
            fatura.Status = StatusFaturaCartao.Efetivada;
            fatura.DataEfetivacao = DateOnly.FromDateTime(dataEfetivacaoSemDespesa);
            fatura.DataEstorno = null;
            fatura = await repository.AtualizarAsync(fatura, cancellationToken);
            return MapLista(fatura);
        }

        ValidarRequestEfetivacao(request, fatura.ValorTotal);

        var conta = await contaBancariaRepository.ObterPorIdAsync(request.ContaBancariaId, usuarioAutenticadoId, cancellationToken);
        if (conta is null)
            throw new DomainException("conta_bancaria_invalida");

        var despesaPagamento = await ObterOuCriarDespesaPagamentoAsync(fatura, request, usuarioAutenticadoId, cancellationToken);
        var valorEfetivado = request.ValorEfetivacao;
        var dataEfetivacao = DateOnly.FromDateTime(request.DataEfetivacao);

        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaPagamento.Id,
            usuarioAutenticadoId,
            dataEfetivacao,
            0m,
            valorEfetivado,
            valorEfetivado,
            "Efetivacao de despesa",
            tipoPagamento: TipoPagamento.Transferencia,
            contaBancariaId: request.ContaBancariaId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(request.ObservacaoHistorico));

        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Receita,
            fatura.Id,
            usuarioAutenticadoId,
            dataEfetivacao,
            0m,
            valorEfetivado,
            valorEfetivado,
            "Efetivacao de pagamento de fatura",
            cartaoId: fatura.CartaoId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(request.ObservacaoHistorico));

        fatura.Status = StatusFaturaCartao.Efetivada;
        fatura.DataEfetivacao = dataEfetivacao;
        fatura.DataEstorno = null;
        fatura.DespesaPagamentoId = despesaPagamento.Id;
        fatura = await repository.AtualizarAsync(fatura, cancellationToken);
        return MapLista(fatura);
    }

    public async Task<FaturaCartaoListaDto> EstornarAsync(long id, EstornarFaturaCartaoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var fatura = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("fatura_nao_encontrada");
        if (fatura.Status != StatusFaturaCartao.Efetivada)
            throw new DomainException("status_invalido");
        if (request.DataEstorno == default)
            throw new DomainException("data_estorno_obrigatoria");
        var dataEstorno = DateOnly.FromDateTime(request.DataEstorno);
        if (fatura.DataEfetivacao.HasValue && dataEstorno < fatura.DataEfetivacao.Value)
            throw new DomainException("periodo_invalido");

        if (!fatura.DespesaPagamentoId.HasValue)
            throw new DomainException("despesa_pagamento_fatura_nao_encontrada");

        var despesaPagamento = await despesaRepository.ObterPorIdAsync(fatura.DespesaPagamentoId.Value, usuarioAutenticadoId, cancellationToken);
        if (despesaPagamento is null)
            throw new DomainException("despesa_pagamento_fatura_nao_encontrada");

        var valorEfetivado = despesaPagamento.ValorEfetivacao ?? despesaPagamento.ValorLiquido;
        despesaPagamento.Status = StatusDespesa.Pendente;
        despesaPagamento.DataEfetivacao = null;
        despesaPagamento.ValorEfetivacao = null;
        await despesaRepository.AtualizarAsync(despesaPagamento, cancellationToken);

        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Despesa,
            despesaPagamento.Id,
            usuarioAutenticadoId,
            dataEstorno,
            valorEfetivado,
            valorEfetivado,
            0m,
            "Estorno de despesa",
            tipoPagamento: TipoPagamento.Transferencia,
            contaBancariaId: despesaPagamento.ContaBancariaId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(request.ObservacaoHistorico),
            ocultarDoHistorico: request.OcultarDoHistorico);

        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Receita,
            fatura.Id,
            usuarioAutenticadoId,
            dataEstorno,
            valorEfetivado,
            valorEfetivado,
            0m,
            "Estorno de pagamento de fatura",
            cartaoId: fatura.CartaoId,
            cancellationToken: cancellationToken,
            observacao: NormalizarObservacao(request.ObservacaoHistorico),
            ocultarDoHistorico: request.OcultarDoHistorico);

        fatura.Status = StatusFaturaCartao.Estornada;
        fatura.DataEfetivacao = null;
        fatura.DataEstorno = dataEstorno;
        fatura = await repository.AtualizarAsync(fatura, cancellationToken);
        return MapLista(fatura);
    }

    public async Task GarantirFaturaEstornadaParaEstornoTransacaoAsync(
        long? faturaCartaoId,
        DateTime dataEstorno,
        bool ocultarDoHistorico,
        string? observacaoHistorico,
        CancellationToken cancellationToken = default)
    {
        if (!faturaCartaoId.HasValue)
            return;

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var fatura = await repository.ObterPorIdAsync(faturaCartaoId.Value, usuarioAutenticadoId, cancellationToken);
        if (fatura is null)
            return;

        if (fatura.Status == StatusFaturaCartao.Efetivada)
        {
            await EstornarAsync(
                fatura.Id,
                new EstornarFaturaCartaoRequest(dataEstorno, observacaoHistorico, ocultarDoHistorico),
                cancellationToken);
        }
    }

    public async Task<long?> ResolverFaturaIdParaTransacaoCartaoAsync(long? cartaoId, string competencia, int usuarioAutenticadoId, CancellationToken cancellationToken = default)
    {
        if (!cartaoId.HasValue)
            return null;

        var competenciaNormalizada = ResolverCompetencia(competencia);
        var fatura = await ObterOuCriarFaturaAsync(cartaoId.Value, usuarioAutenticadoId, competenciaNormalizada, cancellationToken);

        await AplicarFechamentoAutomaticoAsync(fatura, usuarioAutenticadoId, cancellationToken);
        return fatura.Id;
    }

    public async Task<DateOnly?> ObterDataVencimentoPorFaturaIdAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken = default)
    {
        if (!faturaCartaoId.HasValue)
            return null;

        var fatura = await repository.ObterPorIdAsync(faturaCartaoId.Value, usuarioAutenticadoId, cancellationToken);
        if (fatura is null)
            return null;

        if (fatura.DataVencimento.HasValue)
            return fatura.DataVencimento.Value;

        var dataVencimento = await ResolverDataVencimentoFaturaAsync(fatura.CartaoId, usuarioAutenticadoId, fatura.Competencia, cancellationToken);
        if (!dataVencimento.HasValue)
            return null;

        fatura.DataVencimento = dataVencimento.Value;
        await repository.AtualizarAsync(fatura, cancellationToken);
        return dataVencimento.Value;
    }

    public async Task ValidarFaturaPermiteAlteracaoAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken = default)
    {
        if (!faturaCartaoId.HasValue)
            return;

        var fatura = await repository.ObterPorIdAsync(faturaCartaoId.Value, usuarioAutenticadoId, cancellationToken);
        if (fatura is null)
            return;

        if (fatura.Status is StatusFaturaCartao.Efetivada )
            throw new DomainException("status_invalido");
    }

    public async Task RecalcularTotalPorFaturaIdAsync(long? faturaCartaoId, int usuarioAutenticadoId, CancellationToken cancellationToken = default)
    {
        if (!faturaCartaoId.HasValue)
            return;

        var fatura = await repository.ObterPorIdAsync(faturaCartaoId.Value, usuarioAutenticadoId, cancellationToken);
        if (fatura is null)
            return;

        await RecalcularTotalInternoAsync(fatura, usuarioAutenticadoId, cancellationToken);
    }

    private async Task<Despesa> ObterOuCriarDespesaPagamentoAsync(FaturaCartao fatura, EfetivarFaturaCartaoRequest request, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (fatura.DespesaPagamentoId.HasValue)
        {
            var despesaExistente = await despesaRepository.ObterPorIdAsync(fatura.DespesaPagamentoId.Value, usuarioAutenticadoId, cancellationToken);
            if (despesaExistente is not null)
            {
                var valorEfetivado = request.ValorEfetivacao;
                despesaExistente.Descricao = $"Pagamento de fatura cartao {fatura.Competencia}";
                despesaExistente.Observacao = $"Pagamento de fatura do cartao {fatura.CartaoId}.";
                despesaExistente.DataLancamento = request.DataEfetivacao;
                despesaExistente.DataVencimento = fatura.DataVencimento ?? DateOnly.FromDateTime(request.DataEfetivacao);
                despesaExistente.TipoDespesa = TipoDespesa.Servicos;
                despesaExistente.TipoPagamento = TipoPagamento.Transferencia;
                despesaExistente.Recorrencia = Recorrencia.Unica;
                despesaExistente.RecorrenciaFixa = false;
                despesaExistente.QuantidadeRecorrencia = null;
                despesaExistente.ValorTotal = valorEfetivado;
                despesaExistente.ValorLiquido = valorEfetivado;
                despesaExistente.Desconto = 0m;
                despesaExistente.Acrescimo = 0m;
                despesaExistente.Imposto = 0m;
                despesaExistente.Juros = 0m;
                despesaExistente.ValorEfetivacao = valorEfetivado;
                despesaExistente.Status = StatusDespesa.Efetivada;
                despesaExistente.ContaBancariaId = request.ContaBancariaId;
                despesaExistente.ContaDestinoId = null;
                despesaExistente.CartaoId = null;
                despesaExistente.FaturaCartaoId = null;
                despesaExistente.UsuarioCadastroId = usuarioAutenticadoId;
                despesaExistente = await despesaRepository.AtualizarAsync(despesaExistente, cancellationToken);
                return despesaExistente;
            }
        }

        var valor = request.ValorEfetivacao;
        return await despesaRepository.CriarAsync(new Despesa
        {
            UsuarioCadastroId = usuarioAutenticadoId,
            Descricao = $"Pagamento de fatura cartao {fatura.Competencia}",
            Observacao = $"Pagamento de fatura do cartao {fatura.CartaoId}.",
            Competencia = fatura.Competencia,
            DataLancamento = request.DataEfetivacao,
            DataVencimento = fatura.DataVencimento ?? DateOnly.FromDateTime(request.DataEfetivacao),
            DataEfetivacao = request.DataEfetivacao,
            TipoDespesa = TipoDespesa.Servicos,
            TipoPagamento = TipoPagamento.Transferencia,
            Recorrencia = Recorrencia.Unica,
            RecorrenciaFixa = false,
            QuantidadeRecorrencia = null,
            ValorTotal = valor,
            ValorLiquido = valor,
            Desconto = 0m,
            Acrescimo = 0m,
            Imposto = 0m,
            Juros = 0m,
            ValorEfetivacao = valor,
            Status = StatusDespesa.Efetivada,
            ContaBancariaId = request.ContaBancariaId,
            ContaDestinoId = null,
            CartaoId = null,
            FaturaCartaoId = null
        }, cancellationToken);
    }

    private static void ValidarStatusEfetivacao(StatusFaturaCartao status)
    {
        if (status is StatusFaturaCartao.Aberta or StatusFaturaCartao.Fechada or StatusFaturaCartao.Estornada or StatusFaturaCartao.Vencida)
            return;

        throw new DomainException("status_invalido");
    }

    private static void ValidarRequestEfetivacao(EfetivarFaturaCartaoRequest request, decimal valorTotalFatura)
    {
        if (request.DataEfetivacao == default)
            throw new DomainException("data_efetivacao_obrigatoria");
        if (request.ContaBancariaId <= 0)
            throw new DomainException("conta_bancaria_obrigatoria");
        if (request.ValorTotal <= 0 || request.ValorTotal != valorTotalFatura)
            throw new DomainException("valor_total_invalido");
        if (request.ValorEfetivacao < 0 || request.ValorEfetivacao > request.ValorTotal)
            throw new DomainException("valor_efetivacao_invalido");
    }

    private async Task ValidarTransacoesDaFaturaEfetivadasAsync(long faturaId, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        var despesas = await despesaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        if (despesas.Any(x =>
                x.FaturaCartaoId == faturaId &&
                x.Status != StatusDespesa.Cancelada &&
                x.Status != StatusDespesa.Efetivada))
        {
            throw new DomainException("fatura_transacoes_pendentes");
        }

        var receitas = await receitaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        if (receitas.Any(x =>
                x.FaturaCartaoId == faturaId &&
                x.Status != StatusReceita.Cancelada &&
                x.Status != StatusReceita.Efetivada))
        {
            throw new DomainException("fatura_transacoes_pendentes");
        }

        var reembolsos = await reembolsoRepository.ListarAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        if (reembolsos.Any(x =>
                x.FaturaCartaoId == faturaId &&
                x.Status != StatusReembolso.Cancelado &&
                x.Status != StatusReembolso.Pago))
        {
            throw new DomainException("fatura_transacoes_pendentes");
        }
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        var observacaoNormalizada = observacao?.Trim();
        return string.IsNullOrWhiteSpace(observacaoNormalizada) ? null : observacaoNormalizada;
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task SolicitarGarantiaESaneamentoBackgroundAsync(int usuarioAutenticadoId, string competencia, CancellationToken cancellationToken)
    {
        if (faturaCartaoBackgroundPublisher is null)
        {
            await GarantirFaturasESanearTransacoesCartaoAsync(usuarioAutenticadoId, competencia, cancellationToken);
            return;
        }

        await faturaCartaoBackgroundPublisher.PublicarGarantiaESaneamentoAsync(
            new FaturaCartaoGarantiaSaneamentoBackgroundMessage(usuarioAutenticadoId, competencia),
            cancellationToken);
    }

    private async Task GarantirFaturasESanearTransacoesCartaoAsync(int usuarioAutenticadoId, string competencia, CancellationToken cancellationToken)
    {
        var cartoesCredito = (await cartaoRepository.ListarAsync(usuarioAutenticadoId, cancellationToken))
            .Where(x => x.Tipo == TipoCartao.Credito)
            .ToArray();

        if (cartoesCredito.Length == 0)
            return;

        var competenciasParaGarantia = ObterCompetenciasParaGarantia(competencia);
        var faturasPorCartaoCompetencia = new Dictionary<(long CartaoId, string Competencia), FaturaCartao>();

        foreach (var cartao in cartoesCredito)
        {
            foreach (var competenciaGarantia in competenciasParaGarantia)
            {
                var fatura = await ObterOuCriarFaturaAsync(cartao.Id, usuarioAutenticadoId, competenciaGarantia, cancellationToken);
                faturasPorCartaoCompetencia[(cartao.Id, competenciaGarantia)] = fatura;
            }
        }

        var cartoesCreditoIds = cartoesCredito.Select(x => x.Id).ToHashSet();
        await SanearTransacoesOrfasCompetenciaAsync(usuarioAutenticadoId, competencia, cartoesCreditoIds, faturasPorCartaoCompetencia, cancellationToken);
    }

    private async Task SanearTransacoesOrfasCompetenciaAsync(
        int usuarioAutenticadoId,
        string competencia,
        HashSet<long> cartoesCreditoIds,
        Dictionary<(long CartaoId, string Competencia), FaturaCartao> faturasPorCartaoCompetencia,
        CancellationToken cancellationToken)
    {
        var despesas = await despesaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, competencia, null, null, cancellationToken);
        foreach (var despesa in despesas.Where(x => x.CartaoId.HasValue && !x.FaturaCartaoId.HasValue && cartoesCreditoIds.Contains(x.CartaoId.Value)))
        {
            if (!faturasPorCartaoCompetencia.TryGetValue((despesa.CartaoId!.Value, competencia), out var fatura))
                continue;

            despesa.FaturaCartaoId = fatura.Id;
            await despesaRepository.AtualizarAsync(despesa, cancellationToken);
        }

        var receitas = await receitaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, competencia, null, null, cancellationToken);
        foreach (var receita in receitas.Where(x => x.CartaoId.HasValue && !x.FaturaCartaoId.HasValue && cartoesCreditoIds.Contains(x.CartaoId.Value)))
        {
            if (!faturasPorCartaoCompetencia.TryGetValue((receita.CartaoId!.Value, competencia), out var fatura))
                continue;

            receita.FaturaCartaoId = fatura.Id;
            await receitaRepository.AtualizarAsync(receita, cancellationToken);
        }

        var reembolsos = await reembolsoRepository.ListarAsync(usuarioAutenticadoId, null, null, competencia, null, null, cancellationToken);
        foreach (var reembolso in reembolsos.Where(x => x.CartaoId.HasValue && !x.FaturaCartaoId.HasValue && cartoesCreditoIds.Contains(x.CartaoId.Value)))
        {
            if (!faturasPorCartaoCompetencia.TryGetValue((reembolso.CartaoId!.Value, competencia), out var fatura))
                continue;

            reembolso.FaturaCartaoId = fatura.Id;
            await reembolsoRepository.AtualizarAsync(reembolso, cancellationToken);
        }
    }

    private async Task<FaturaCartao> ObterOuCriarFaturaAsync(long cartaoId, int usuarioAutenticadoId, string competencia, CancellationToken cancellationToken)
    {
        var dataVencimento = await ResolverDataVencimentoFaturaAsync(cartaoId, usuarioAutenticadoId, competencia, cancellationToken);
        var fatura = await repository.ObterPorCartaoCompetenciaAsync(cartaoId, usuarioAutenticadoId, competencia, cancellationToken);
        if (fatura is not null)
        {
            if (!fatura.DataVencimento.HasValue && dataVencimento.HasValue)
            {
                fatura.DataVencimento = dataVencimento.Value;
                fatura = await repository.AtualizarAsync(fatura, cancellationToken);
            }

            return fatura;
        }

        return await repository.CriarAsync(new FaturaCartao
        {
            UsuarioCadastroId = usuarioAutenticadoId,
            CartaoId = cartaoId,
            Competencia = competencia,
            DataVencimento = dataVencimento,
            Status = StatusFaturaCartao.Aberta,
            ValorTotal = 0m
        }, cancellationToken);
    }

    private async Task RecalcularTotalInternoAsync(FaturaCartao fatura, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        var lancamentos = await ListarLancamentosInternoAsync(fatura.Id, usuarioAutenticadoId, null, cancellationToken);
        var valorTotal = lancamentos.Sum(x => x.Valor);
        if (fatura.ValorTotal == valorTotal)
            return;

        fatura.ValorTotal = valorTotal;
        await repository.AtualizarAsync(fatura, cancellationToken);
    }

    private async Task<IReadOnlyCollection<FaturaCartaoLancamentoDto>> ListarLancamentosInternoAsync(long faturaId, int usuarioAutenticadoId, string? tipoTransacao, CancellationToken cancellationToken)
    {
        var tipoFiltro = NormalizarTipoTransacaoFiltro(tipoTransacao);
        var despesas = await despesaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        var receitas = await receitaRepository.ListarPorUsuarioAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);
        var reembolsos = await reembolsoRepository.ListarAsync(usuarioAutenticadoId, null, null, null, null, null, cancellationToken);

        var lancamentosDespesa = despesas
            .Where(x => x.FaturaCartaoId == faturaId)
            .Where(x => x.Status != StatusDespesa.Cancelada)
            .Select(x => new FaturaCartaoLancamentoDto(
                "despesa",
                x.Id,
                x.Descricao,
                x.Competencia,
                x.DataLancamento,
                x.DataEfetivacao,
                x.ValorEfetivacao ?? x.ValorLiquido,
                x.Status.ToString().ToLowerInvariant()));

        var lancamentosReceita = receitas
                .Where(x => x.FaturaCartaoId == faturaId)
                .Where(x => x.Status != StatusReceita.Cancelada)
                .Select(x => new FaturaCartaoLancamentoDto(
                    "receita",
                    x.Id,
                    x.Descricao,
                    x.Competencia,
                    x.DataLancamento,
                    x.DataEfetivacao,
                    -(x.ValorEfetivacao ?? x.ValorLiquido),
                    x.Status.ToString().ToLowerInvariant()));

        var lancamentosReembolso = reembolsos
                .Where(x => x.FaturaCartaoId == faturaId)
                .Where(x => x.Status != StatusReembolso.Cancelado)
                .Select(x => new FaturaCartaoLancamentoDto(
                    "reembolso",
                    x.Id,
                    x.Descricao,
                    x.Competencia,
                    x.DataLancamento,
                    x.DataEfetivacao,
                    -x.ValorTotal,
                    x.Status.ToString().ToLowerInvariant()));

        IEnumerable<FaturaCartaoLancamentoDto> lancamentosBase = tipoFiltro switch
        {
            "despesa" => lancamentosDespesa,
            "receita" => lancamentosReceita,
            "reembolso" => lancamentosReembolso,
            _ => lancamentosDespesa.Concat(lancamentosReceita).Concat(lancamentosReembolso)
        };

        var lancamentos = lancamentosBase
            .OrderBy(x => x.DataLancamento)
            .ThenBy(x => x.TransacaoId)
            .ToArray();

        return lancamentos;
    }

    private async Task AplicarFechamentoAutomaticoAsync(FaturaCartao fatura, int usuarioAutenticadoId, CancellationToken cancellationToken)
    {
        if (fatura.Status is not StatusFaturaCartao.Aberta and not StatusFaturaCartao.Fechada and not StatusFaturaCartao.Vencida)
            return;

        var dataVencimento = fatura.DataVencimento ??
                             await ResolverDataVencimentoFaturaAsync(fatura.CartaoId, usuarioAutenticadoId, fatura.Competencia, cancellationToken);
        if (!dataVencimento.HasValue)
            return;

        var precisaPersistirDataVencimento = !fatura.DataVencimento.HasValue;
        if (precisaPersistirDataVencimento)
            fatura.DataVencimento = dataVencimento.Value;

        var dataFechamentoBase = dataVencimento.Value.AddDays(-DiasAntesVencimentoParaFechamento);
        var dataFechamento = AjustarParaDiaUtilAnteriorOuIgual(dataFechamentoBase);
        var hoje = DataHoraBrasil.Hoje();

        if (hoje > dataVencimento.Value)
        {
            fatura.Status = StatusFaturaCartao.Vencida;
            fatura.DataFechamento = dataFechamento;
            await repository.AtualizarAsync(fatura, cancellationToken);
            return;
        }

        if (hoje < dataFechamento)
        {
            if (precisaPersistirDataVencimento)
                await repository.AtualizarAsync(fatura, cancellationToken);
            return;
        }

        fatura.Status = StatusFaturaCartao.Fechada;
        fatura.DataFechamento = dataFechamento;
        await repository.AtualizarAsync(fatura, cancellationToken);
    }

    private async Task<DateOnly?> ResolverDataVencimentoFaturaAsync(long cartaoId, int usuarioAutenticadoId, string competencia, CancellationToken cancellationToken)
    {
        var cartao = await cartaoRepository.ObterPorIdAsync(cartaoId, usuarioAutenticadoId, cancellationToken);
        if (cartao?.DiaVencimento is null)
            return null;

        var periodo = CompetenciaPeriodoHelper.Resolver(competencia, null, null);
        var dataBase = periodo.DataInicio ?? DataHoraBrasil.Hoje();
        var diaVencimento = cartao.DiaVencimento.Value.Day;
        var ultimoDiaMes = DateTime.DaysInMonth(dataBase.Year, dataBase.Month);
        var dataVencimentoBase = new DateOnly(dataBase.Year, dataBase.Month, Math.Min(diaVencimento, ultimoDiaMes));
        return AjustarParaProximoDiaUtil(dataVencimentoBase);
    }

    private static DateOnly AjustarParaProximoDiaUtil(DateOnly data)
    {
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            data = data.AddDays(1);

        return data;
    }

    private static DateOnly AjustarParaDiaUtilAnteriorOuIgual(DateOnly data)
    {
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            data = data.AddDays(-1);

        return data;
    }

    private static string ResolverCompetencia(string competencia)
    {
        if (string.IsNullOrWhiteSpace(competencia))
            return DataHoraBrasil.Agora().ToString("yyyy-MM");

        var periodo = CompetenciaPeriodoHelper.Resolver(competencia, null, null);
        var competenciaData = periodo.DataInicio?.ToDateTime(TimeOnly.MinValue) ?? DataHoraBrasil.Agora();
        return competenciaData.ToString("yyyy-MM");
    }

    private static string ResolverCompetenciaObrigatoria(string? competencia)
    {
        if (string.IsNullOrWhiteSpace(competencia))
            throw new DomainException("competencia_obrigatoria");

        return ResolverCompetencia(competencia);
    }

    private static string[] ObterCompetenciasParaGarantia(string competencia)
    {
        var periodo = CompetenciaPeriodoHelper.Resolver(competencia, null, null);
        var dataBase = periodo.DataInicio?.ToDateTime(TimeOnly.MinValue) ?? DataHoraBrasil.Agora();
        return
        [
            dataBase.ToString("yyyy-MM"),
            dataBase.AddMonths(1).ToString("yyyy-MM"),
            dataBase.AddMonths(2).ToString("yyyy-MM"),
            dataBase.AddMonths(3).ToString("yyyy-MM")
        ];
    }

    private static string? NormalizarTipoTransacaoFiltro(string? tipoTransacao)
    {
        if (string.IsNullOrWhiteSpace(tipoTransacao))
            return null;

        var tipo = tipoTransacao.Trim().ToLowerInvariant();
        return tipo is "despesa" or "receita" or "reembolso"
            ? tipo
            : throw new DomainException("tipo_transacao_invalido");
    }

    private static FaturaCartaoListaDto MapLista(FaturaCartao fatura) =>
        new(
            fatura.Id,
            fatura.CartaoId,
            fatura.Competencia,
            fatura.DataVencimento,
            fatura.ValorTotal,
            fatura.Status.ToString().ToLowerInvariant(),
            fatura.DataFechamento,
            fatura.DataEfetivacao,
            fatura.DataEstorno);

    private static FaturaCartaoDetalheDto MapDetalhe(FaturaCartao fatura, IReadOnlyCollection<FaturaCartaoLancamentoDto> lancamentos)
    {
        var valorTotalTransacoes = lancamentos.Sum(x => x.Valor);
        return new(
            fatura.Id,
            fatura.CartaoId,
            fatura.Competencia,
            fatura.DataVencimento,
            fatura.ValorTotal,
            valorTotalTransacoes,
            fatura.Status.ToString().ToLowerInvariant(),
            fatura.DataFechamento,
            fatura.DataEfetivacao,
            fatura.DataEstorno,
            lancamentos);
    }
}
