using Core.Application.DTOs;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services;

public sealed class DashboardService(IDespesaRepository despesaRepository, IReceitaRepository receitaRepository, IContaBancariaRepository contaRepository, ICartaoRepository cartaoRepository)
{
    public async Task<DashboardDto> ObterAsync(CancellationToken cancellationToken = default)
    {
        var despesas = (await despesaRepository.ListarAsync(cancellationToken)).Where(x => x.Status == StatusDespesa.Efetivada && x.DataEfetivacao.HasValue);
        var receitas = (await receitaRepository.ListarAsync(cancellationToken)).Where(x => x.Status == StatusReceita.Efetivada && x.DataEfetivacao.HasValue);

        var transacoesDespesa = despesas.Select(d => new TransacaoDashboardDto(
            d.Id, "despesa", d.ValorEfetivacao ?? d.ValorLiquido, d.Descricao, d.DataEfetivacao!.Value,
            ToCodigoPagamento(d.TipoPagamento), d.TipoPagamento, null, d.TipoPagamento == "cartaoCredito" ? "Cartao" : null, "Financeiro", d.TipoDespesa));

        var transacoesReceita = receitas.Select(r => new TransacaoDashboardDto(
            r.Id, "receita", r.ValorEfetivacao ?? r.ValorLiquido, r.Descricao, r.DataEfetivacao!.Value,
            ToCodigoPagamento(r.TipoRecebimento), r.TipoRecebimento, r.ContaBancariaId?.ToString(), null, "Financeiro", r.TipoReceita));

        var transacoes = transacoesDespesa.Concat(transacoesReceita)
            .OrderByDescending(x => x.DataEfetivacao)
            .ThenByDescending(x => x.Id)
            .Take(100)
            .ToArray();

        var contas = await contaRepository.ListarAsync(cancellationToken);
        var cartoes = await cartaoRepository.ListarAsync(cancellationToken);

        var balanco = contas.Select(c => new ItemBalancoDashboardDto($"conta-{c.Id}", "conta", c.Descricao, "Saldo atual da conta", c.SaldoAtual))
            .Concat(cartoes.Select(c => new ItemBalancoDashboardDto($"cartao-{c.Id}", "cartao", c.Descricao, "Saldo disponivel do cartao", c.SaldoDisponivel)))
            .ToArray();

        return new DashboardDto(transacoes, balanco);
    }

    private static string ToCodigoPagamento(string valor) =>
        valor switch
        {
            "cartaoCredito" => "CARTAO_CREDITO",
            "cartaoDebito" => "CARTAO_DEBITO",
            "contaCorrente" => "CONTA_CORRENTE",
            _ => valor.ToUpperInvariant()
        };
}
