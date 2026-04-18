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
    IDespesaRepository despesaRepository,
    IReceitaRepository receitaRepository,
    IReembolsoRepository reembolsoRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
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

    public async Task<FaturaCartaoListaDto> EfetivarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var fatura = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("fatura_nao_encontrada");
        if (fatura.Status == StatusFaturaCartao.Estornada) throw new DomainException("status_invalido");

        await AplicarFechamentoAutomaticoAsync(fatura, usuarioAutenticadoId, cancellationToken);
        await RecalcularTotalInternoAsync(fatura, usuarioAutenticadoId, cancellationToken);

        fatura.Status = StatusFaturaCartao.Efetivada;
        fatura.DataEfetivacao = DataHoraBrasil.Hoje();
        fatura.DataEstorno = null;
        fatura = await repository.AtualizarAsync(fatura, cancellationToken);
        return MapLista(fatura);
    }

    public async Task<FaturaCartaoListaDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var fatura = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("fatura_nao_encontrada");
        if (fatura.Status != StatusFaturaCartao.Efetivada) throw new DomainException("status_invalido");

        fatura.Status = StatusFaturaCartao.Estornada;
        fatura.DataEstorno = DataHoraBrasil.Hoje();
        fatura = await repository.AtualizarAsync(fatura, cancellationToken);
        return MapLista(fatura);
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

        if (fatura.Status is StatusFaturaCartao.Efetivada or StatusFaturaCartao.Estornada)
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
        if (fatura.Status != StatusFaturaCartao.Aberta)
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
