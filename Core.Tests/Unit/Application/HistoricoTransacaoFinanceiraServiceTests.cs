using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class HistoricoTransacaoFinanceiraServiceTests
{
    [Fact]
    public async Task DeveRegistrarEfetivacaoComContaBancaria()
    {
        var repository = new HistoricoRepositoryFake();
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            10,
            99,
            new DateOnly(2026, 3, 28),
            100m,
            40m,
            60m,
            "Efetivacao de despesa",
            tipoPagamento: TipoPagamento.Pix,
            contaBancariaId: 7);

        var historico = Assert.Single(repository.HistoricosCriados);
        Assert.Equal(TipoOperacaoTransacaoFinanceira.Efetivacao, historico.TipoOperacao);
        Assert.Equal(TipoContaTransacaoFinanceira.ContaBancaria, historico.TipoConta);
        Assert.Equal(7, historico.ContaBancariaId);
        Assert.Null(historico.CartaoId);
    }

    [Fact]
    public async Task DeveRegistrarEfetivacaoComCartao_QuandoTipoPagamentoForCartaoCredito()
    {
        var repository = new HistoricoRepositoryFake();
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            20,
            99,
            new DateOnly(2026, 3, 28),
            100m,
            100m,
            0m,
            "Efetivacao de despesa no cartao",
            tipoPagamento: TipoPagamento.CartaoCredito);

        var historico = Assert.Single(repository.HistoricosCriados);
        Assert.Equal(TipoContaTransacaoFinanceira.Cartao, historico.TipoConta);
        Assert.Null(historico.ContaBancariaId);
        Assert.Null(historico.CartaoId);
    }

    [Fact]
    public async Task DeveUsarUltimoHistorico_QuandoRegistrarEstornoSemContaOuCartao()
    {
        var repository = new HistoricoRepositoryFake
        {
            UltimoHistorico = new HistoricoTransacaoFinanceira
            {
                ContaBancariaId = 12,
                TipoPagamento = TipoPagamento.Transferencia
            }
        };
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Receita,
            30,
            77,
            new DateOnly(2026, 3, 28),
            400m,
            200m,
            200m,
            "Estorno de receita");

        var historico = Assert.Single(repository.HistoricosCriados);
        Assert.Equal(TipoOperacaoTransacaoFinanceira.Estorno, historico.TipoOperacao);
        Assert.Equal(TipoContaTransacaoFinanceira.ContaBancaria, historico.TipoConta);
        Assert.Equal(12, historico.ContaBancariaId);
        Assert.Equal(TipoPagamento.Transferencia, historico.TipoPagamento);
    }

    [Fact]
    public async Task DeveRegistrarEstornoComTipoContaNaoInformado_QuandoNaoHouverReferenciaAnterior()
    {
        var repository = new HistoricoRepositoryFake();
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Reembolso,
            40,
            88,
            new DateOnly(2026, 3, 28),
            100m,
            100m,
            0m,
            "Estorno de reembolso");

        var historico = Assert.Single(repository.HistoricosCriados);
        Assert.Equal(TipoContaTransacaoFinanceira.NaoInformado, historico.TipoConta);
        Assert.Null(historico.ContaBancariaId);
        Assert.Null(historico.CartaoId);
    }

    private sealed class HistoricoRepositoryFake : IHistoricoTransacaoFinanceiraRepository
    {
        public List<HistoricoTransacaoFinanceira> HistoricosCriados { get; } = [];
        public HistoricoTransacaoFinanceira? UltimoHistorico { get; set; }

        public Task<HistoricoTransacaoFinanceira> CriarAsync(HistoricoTransacaoFinanceira historico, CancellationToken cancellationToken = default)
        {
            HistoricosCriados.Add(historico);
            return Task.FromResult(historico);
        }

        public Task<HistoricoTransacaoFinanceira?> ObterUltimoPorTransacaoAsync(TipoTransacaoFinanceira tipoTransacao, long transacaoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(UltimoHistorico);

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioAsync(int usuarioOperacaoId, int quantidadeRegistros, OrdemRegistrosHistoricoTransacaoFinanceira ordemRegistros, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorUsuarioResumoAsync(int usuarioOperacaoId, int? ano, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorContaBancariaCompetenciaAsync(long contaBancariaId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());

        public Task<List<HistoricoTransacaoFinanceira>> ListarPorCartaoCompetenciaAsync(long cartaoId, int usuarioOperacaoId, string? competencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HistoricoTransacaoFinanceira>());
    }
}
