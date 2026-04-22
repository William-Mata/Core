using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;
using System.Globalization;

namespace Core.Application.Services.Financeiro;

public sealed class HistoricoTransacaoFinanceiraConsultaService(
    IHistoricoTransacaoFinanceiraRepository repository,
    IDespesaRepository despesaRepository,
    IReceitaRepository receitaRepository,
    IReembolsoRepository reembolsoRepository,
    IContaBancariaRepository contaBancariaRepository,
    ICartaoRepository cartaoRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<HistoricoTransacaoFinanceiraListaDto>> ListarAsync(
        ListarHistoricoTransacaoFinanceiraRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantidadeRegistros <= 0)
            throw new DomainException("quantidade_registros_invalida");

        if (!Enum.IsDefined(request.OrdemRegistros))
            throw new DomainException("ordem_registros_invalida");

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var historicos = await repository.ListarPorUsuarioAsync(
            usuarioAutenticadoId,
            request.QuantidadeRegistros,
            request.OrdemRegistros,
            cancellationToken);

        if (historicos.Count == 0)
            return [];

        var contasById = await ObterContasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var cartoesById = await ObterCartoesPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var despesasById = await ObterDespesasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var receitasById = await ObterReceitasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var reembolsosById = await ObterReembolsosPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);

        return historicos
            .Select(historico => Map(
                historico,
                contasById.GetValueOrDefault(historico.ContaBancariaId ?? 0),
                cartoesById.GetValueOrDefault(historico.CartaoId ?? 0),
                despesasById.GetValueOrDefault(historico.TransacaoId),
                receitasById.GetValueOrDefault(historico.TransacaoId),
                reembolsosById.GetValueOrDefault(historico.TransacaoId)))
            .ToArray();
    }

    public async Task<ResumoHistoricoTransacaoFinanceiraDto> ObterResumoAsync(
        int? ano,
        CancellationToken cancellationToken = default)
    {
        if (ano.HasValue && ano.Value <= 0)
            throw new DomainException("ano_invalido");

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var historicos = (await repository.ListarPorUsuarioResumoAsync(usuarioAutenticadoId, ano, cancellationToken))
            .Where(x => !EhMovimentacaoEntreContas(x))
            .ToArray();
        var totais = CalcularTotais(historicos);

        return new ResumoHistoricoTransacaoFinanceiraDto(
            ano,
            totais.TotalReceitas,
            totais.TotalDespesas,
            totais.TotalReembolsos,
            totais.TotalEstornos,
            totais.TotalGeral);
    }

    public async Task<IReadOnlyCollection<ResumoHistoricoTransacaoFinanceiraMesDto>> ObterResumoPorAnoAsync(
        int ano,
        CancellationToken cancellationToken = default)
    {
        if (ano <= 0)
            throw new DomainException("ano_invalido");

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var historicos = (await repository.ListarPorUsuarioResumoAsync(usuarioAutenticadoId, ano, cancellationToken))
            .Where(x => !EhMovimentacaoEntreContas(x))
            .ToArray();
        var culturaPtBr = new CultureInfo("pt-BR");

        return Enumerable.Range(1, 12)
            .Select(mes =>
            {
                var historicosMes = historicos.Where(x => x.DataTransacao.Month == mes).ToArray();
                var totaisMes = CalcularTotais(historicosMes);

                return new ResumoHistoricoTransacaoFinanceiraMesDto(
                    culturaPtBr.TextInfo.ToTitleCase(culturaPtBr.DateTimeFormat.GetMonthName(mes)),
                    totaisMes.TotalReceitas,
                    totaisMes.TotalDespesas,
                    totaisMes.TotalReembolsos,
                    totaisMes.TotalEstornos);
            })
            .ToArray();
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task<Dictionary<long, string>> ObterContasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var contaIds = historicos
            .Where(x => x.ContaBancariaId.HasValue)
            .Select(x => x.ContaBancariaId!.Value)
            .Distinct()
            .ToArray();

        var contasById = new Dictionary<long, string>(contaIds.Length);
        foreach (var contaId in contaIds)
        {
            var conta = await contaBancariaRepository.ObterPorIdAsync(contaId, usuarioAutenticadoId, cancellationToken);
            if (conta is not null)
                contasById[conta.Id] = conta.Descricao;
        }

        return contasById;
    }

    private async Task<Dictionary<long, string>> ObterCartoesPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var cartaoIds = historicos
            .Where(x => x.CartaoId.HasValue)
            .Select(x => x.CartaoId!.Value)
            .Distinct()
            .ToArray();

        var cartoesById = new Dictionary<long, string>(cartaoIds.Length);
        foreach (var cartaoId in cartaoIds)
        {
            var cartao = await cartaoRepository.ObterPorIdAsync(cartaoId, usuarioAutenticadoId, cancellationToken);
            if (cartao is not null)
                cartoesById[cartao.Id] = cartao.Descricao;
        }

        return cartoesById;
    }

    private async Task<Dictionary<long, Despesa>> ObterDespesasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var despesaIds = historicos
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Despesa)
            .Select(x => x.TransacaoId)
            .Distinct()
            .ToArray();

        if (despesaIds.Length == 0)
            return [];

        var despesas = await despesaRepository.ObterPorIdsAsync(despesaIds, usuarioAutenticadoId, cancellationToken);
        return despesas.ToDictionary(x => x.Id, x => x);
    }

    private async Task<Dictionary<long, Receita>> ObterReceitasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var receitaIds = historicos
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Receita)
            .Select(x => x.TransacaoId)
            .Distinct()
            .ToArray();

        var receitasById = new Dictionary<long, Receita>(receitaIds.Length);
        foreach (var receitaId in receitaIds)
        {
            var receita = await receitaRepository.ObterPorIdAsync(receitaId, usuarioAutenticadoId, cancellationToken);
            if (receita is not null)
                receitasById[receita.Id] = receita;
        }

        return receitasById;
    }

    private async Task<Dictionary<long, Reembolso>> ObterReembolsosPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var reembolsoIds = historicos
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Reembolso)
            .Select(x => x.TransacaoId)
            .Distinct()
            .ToArray();

        var reembolsosById = new Dictionary<long, Reembolso>(reembolsoIds.Length);
        foreach (var reembolsoId in reembolsoIds)
        {
            var reembolso = await reembolsoRepository.ObterPorIdAsync(reembolsoId, usuarioAutenticadoId, cancellationToken);
            if (reembolso is not null)
                reembolsosById[reembolso.Id] = reembolso;
        }

        return reembolsosById;
    }

    private static HistoricoTransacaoFinanceiraListaDto Map(
        HistoricoTransacaoFinanceira historico,
        string? contaBancaria,
        string? cartao,
        Despesa? despesa,
        Receita? receita,
        Reembolso? reembolso)
    {
        var tipoOrigem = historico.TipoTransacao.ToString().ToLowerInvariant();
        var tipoTransacao = historico.TipoOperacao == TipoOperacaoTransacaoFinanceira.Estorno
            ? $"estorno {tipoOrigem}"
            : tipoOrigem;
        var descricao = historico.TipoTransacao switch
        {
            TipoTransacaoFinanceira.Despesa => despesa?.Descricao,
            TipoTransacaoFinanceira.Receita => receita?.Descricao,
            TipoTransacaoFinanceira.Reembolso => reembolso?.Descricao,
            _ => null
        };

        return new HistoricoTransacaoFinanceiraListaDto(
            historico.TransacaoId,
            tipoTransacao,
            historico.ValorTransacao,
            descricao ?? historico.Descricao,
            historico.DataTransacao,
            historico.TipoPagamento?.ToString() ?? historico.TipoRecebimento?.ToString(),
            contaBancaria,
            cartao,
            despesa?.TipoDespesa.ToString(),
            receita?.TipoReceita.ToString());
    }

    private static (decimal TotalReceitas, decimal TotalDespesas, decimal TotalReembolsos, decimal TotalEstornos, decimal TotalGeral) CalcularTotais(
        IEnumerable<HistoricoTransacaoFinanceira> historicos)
    {
        var totais = historicos.ToArray();

        var totalReceitas = totais
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Receita && x.TipoOperacao == TipoOperacaoTransacaoFinanceira.Efetivacao)
            .Sum(x => x.ValorTransacao);

        var totalDespesas = totais
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Despesa && x.TipoOperacao == TipoOperacaoTransacaoFinanceira.Efetivacao)
            .Sum(x => x.ValorTransacao);

        var totalReembolsos = totais
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Reembolso && x.TipoOperacao == TipoOperacaoTransacaoFinanceira.Efetivacao)
            .Sum(x => x.ValorTransacao);

        var totalEstornos = totais
            .Where(x => x.TipoOperacao == TipoOperacaoTransacaoFinanceira.Estorno)
            .Sum(x => x.ValorTransacao);

        return (
            totalReceitas,
            totalDespesas,
            totalReembolsos,
            totalEstornos,
            totalReceitas + totalDespesas + totalReembolsos + totalEstornos);
    }

    private static bool EhMovimentacaoEntreContas(HistoricoTransacaoFinanceira historico) =>
        (historico.TipoPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix
            || historico.TipoRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix)
        && historico.ContaBancariaId.HasValue
        && historico.ContaDestinoId.HasValue
        && !historico.CartaoId.HasValue;
}
