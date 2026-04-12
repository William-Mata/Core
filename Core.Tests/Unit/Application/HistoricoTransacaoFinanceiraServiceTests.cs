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
                ContaDestinoId = 13,
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

        Assert.Equal(2, repository.HistoricosCriados.Count);
        var historico = repository.HistoricosCriados[0];
        Assert.Equal(TipoOperacaoTransacaoFinanceira.Estorno, historico.TipoOperacao);
        Assert.Equal(TipoContaTransacaoFinanceira.ContaBancaria, historico.TipoConta);
        Assert.Equal(12, historico.ContaBancariaId);
        Assert.Equal(13, historico.ContaDestinoId);
        Assert.Equal(TipoPagamento.Transferencia, historico.TipoPagamento);

        var historicoEspelho = repository.HistoricosCriados[1];
        Assert.Equal(TipoTransacaoFinanceira.Despesa, historicoEspelho.TipoTransacao);
        Assert.Equal(13, historicoEspelho.ContaBancariaId);
        Assert.Equal(12, historicoEspelho.ContaDestinoId);
    }

    [Fact]
    public async Task DeveRegistrarMovimentacaoEspelhada_QuandoEfetivarTransferenciaComContaDestino()
    {
        var repository = new HistoricoRepositoryFake();
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Despesa,
            50,
            99,
            new DateOnly(2026, 3, 29),
            0m,
            120m,
            120m,
            "Efetivacao de despesa",
            tipoPagamento: TipoPagamento.Transferencia,
            contaBancariaId: 10,
            contaDestinoId: 20);

        Assert.Equal(2, repository.HistoricosCriados.Count);

        var historicoOrigem = repository.HistoricosCriados[0];
        Assert.Equal(TipoTransacaoFinanceira.Despesa, historicoOrigem.TipoTransacao);
        Assert.Equal(10, historicoOrigem.ContaBancariaId);
        Assert.Equal(20, historicoOrigem.ContaDestinoId);

        var historicoEspelho = repository.HistoricosCriados[1];
        Assert.Equal(TipoTransacaoFinanceira.Receita, historicoEspelho.TipoTransacao);
        Assert.Equal(20, historicoEspelho.ContaBancariaId);
        Assert.Equal(10, historicoEspelho.ContaDestinoId);
        Assert.Equal(TipoRecebimento.Transferencia, historicoEspelho.TipoRecebimento);
    }

    [Fact]
    public async Task DeveRegistrarMovimentacaoEspelhada_QuandoEfetivarPixComContaDestino()
    {
        var repository = new HistoricoRepositoryFake();
        var service = new HistoricoTransacaoFinanceiraService(repository);

        await service.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Receita,
            51,
            99,
            new DateOnly(2026, 3, 30),
            0m,
            80m,
            80m,
            "Efetivacao de receita",
            tipoRecebimento: TipoRecebimento.Pix,
            contaBancariaId: 20,
            contaDestinoId: 10);

        Assert.Equal(2, repository.HistoricosCriados.Count);

        var historicoOrigem = repository.HistoricosCriados[0];
        Assert.Equal(TipoTransacaoFinanceira.Receita, historicoOrigem.TipoTransacao);
        Assert.Equal(TipoRecebimento.Pix, historicoOrigem.TipoRecebimento);
        Assert.Equal(20, historicoOrigem.ContaBancariaId);
        Assert.Equal(10, historicoOrigem.ContaDestinoId);

        var historicoEspelho = repository.HistoricosCriados[1];
        Assert.Equal(TipoTransacaoFinanceira.Despesa, historicoEspelho.TipoTransacao);
        Assert.Equal(TipoPagamento.Pix, historicoEspelho.TipoPagamento);
        Assert.Equal(10, historicoEspelho.ContaBancariaId);
        Assert.Equal(20, historicoEspelho.ContaDestinoId);
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
