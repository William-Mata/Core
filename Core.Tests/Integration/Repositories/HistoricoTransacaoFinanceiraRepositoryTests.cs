using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Repositories.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Integration.Repositories;

public sealed class HistoricoTransacaoFinanceiraRepositoryTests
{
    [Fact]
    public async Task DeveSubtrairSaldoDaConta_QuandoEfetivarDespesa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        context.ContasBancarias.Add(new ContaBancaria
        {
            Id = 1,
            UsuarioCadastroId = 1,
            Descricao = "Conta principal",
            Banco = "Banco",
            Agencia = "0001",
            Numero = "12345-6",
            SaldoInicial = 100m,
            SaldoAtual = 100m,
            DataAbertura = new DateOnly(2026, 1, 1),
            Status = StatusContaBancaria.Ativa
        });
        await context.SaveChangesAsync();

        var repository = new HistoricoTransacaoFinanceiraRepository(context);

        var historico = await repository.CriarAsync(new HistoricoTransacaoFinanceira
        {
            UsuarioOperacaoId = 1,
            TipoTransacao = TipoTransacaoFinanceira.Despesa,
            TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
            TransacaoId = 10,
            ContaBancariaId = 1,
            DataTransacao = new DateOnly(2026, 3, 20),
            Descricao = "Efetivacao de despesa",
            ValorAntesTransacao = 0m,
            ValorTransacao = 30m,
            ValorDepoisTransacao = 0m
        });

        var conta = await context.ContasBancarias.FirstAsync(x => x.Id == 1);
        Assert.Equal(70m, conta.SaldoAtual);
        Assert.Equal(100m, historico.ValorAntesTransacao);
        Assert.Equal(-30m, historico.ValorTransacao);
        Assert.Equal(70m, historico.ValorDepoisTransacao);
    }

    [Fact]
    public async Task DeveSomarSaldoDaConta_QuandoEstornarDespesa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        context.ContasBancarias.Add(new ContaBancaria
        {
            Id = 2,
            UsuarioCadastroId = 1,
            Descricao = "Conta principal",
            Banco = "Banco",
            Agencia = "0001",
            Numero = "12345-6",
            SaldoInicial = 100m,
            SaldoAtual = 70m,
            DataAbertura = new DateOnly(2026, 1, 1),
            Status = StatusContaBancaria.Ativa
        });
        await context.SaveChangesAsync();

        var repository = new HistoricoTransacaoFinanceiraRepository(context);

        var historico = await repository.CriarAsync(new HistoricoTransacaoFinanceira
        {
            UsuarioOperacaoId = 1,
            TipoTransacao = TipoTransacaoFinanceira.Despesa,
            TipoOperacao = TipoOperacaoTransacaoFinanceira.Estorno,
            TransacaoId = 11,
            ContaBancariaId = 2,
            DataTransacao = new DateOnly(2026, 3, 21),
            Descricao = "Estorno de despesa",
            ValorAntesTransacao = 0m,
            ValorTransacao = 30m,
            ValorDepoisTransacao = 0m
        });

        var conta = await context.ContasBancarias.FirstAsync(x => x.Id == 2);
        Assert.Equal(100m, conta.SaldoAtual);
        Assert.Equal(70m, historico.ValorAntesTransacao);
        Assert.Equal(30m, historico.ValorTransacao);
        Assert.Equal(100m, historico.ValorDepoisTransacao);
    }

    [Fact]
    public async Task DevePermitirSaldoNegativoNoCartao_QuandoEstornarReembolso()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        context.Cartoes.Add(new Cartao
        {
            Id = 3,
            UsuarioCadastroId = 1,
            Descricao = "Cartao viagens",
            Bandeira = "Visa",
            Tipo = TipoCartao.Credito,
            Limite = 1000m,
            SaldoDisponivel = 10m,
            DiaVencimento = new DateOnly(2026, 3, 10),
            DataVencimentoCartao = new DateOnly(2026, 3, 30),
            Status = StatusCartao.Ativo
        });
        await context.SaveChangesAsync();

        var repository = new HistoricoTransacaoFinanceiraRepository(context);

        var historico = await repository.CriarAsync(new HistoricoTransacaoFinanceira
        {
            UsuarioOperacaoId = 1,
            TipoTransacao = TipoTransacaoFinanceira.Reembolso,
            TipoOperacao = TipoOperacaoTransacaoFinanceira.Estorno,
            TransacaoId = 12,
            CartaoId = 3,
            DataTransacao = new DateOnly(2026, 3, 22),
            Descricao = "Estorno de reembolso",
            ValorAntesTransacao = 0m,
            ValorTransacao = 20m,
            ValorDepoisTransacao = 0m
        });

        var cartao = await context.Cartoes.FirstAsync(x => x.Id == 3);
        Assert.Equal(-10m, cartao.SaldoDisponivel);
        Assert.Equal(10m, historico.ValorAntesTransacao);
        Assert.Equal(-20m, historico.ValorTransacao);
        Assert.Equal(-10m, historico.ValorDepoisTransacao);
    }

    [Fact]
    public async Task DeveRespeitarQuantidadeEOrdemMaisRecentes_QuandoListarPorUsuario()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        context.HistoricosTransacoesFinanceiras.AddRange(
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 7,
                TipoTransacao = TipoTransacaoFinanceira.Despesa,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 100,
                DataTransacao = new DateOnly(2026, 3, 20),
                Descricao = "Historico 1",
                ValorTransacao = -10m
            },
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 7,
                TipoTransacao = TipoTransacaoFinanceira.Receita,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 101,
                DataTransacao = new DateOnly(2026, 3, 22),
                Descricao = "Historico 2",
                ValorTransacao = 15m
            },
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 7,
                TipoTransacao = TipoTransacaoFinanceira.Reembolso,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 102,
                DataTransacao = new DateOnly(2026, 3, 24),
                Descricao = "Historico 3",
                ValorTransacao = 20m
            },
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 8,
                TipoTransacao = TipoTransacaoFinanceira.Despesa,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 103,
                DataTransacao = new DateOnly(2026, 3, 26),
                Descricao = "Historico outro usuario",
                ValorTransacao = -5m
            });
        await context.SaveChangesAsync();

        var repository = new HistoricoTransacaoFinanceiraRepository(context);

        var historicos = await repository.ListarPorUsuarioAsync(
            7,
            2,
            OrdemRegistrosHistoricoTransacaoFinanceira.MaisRecentes,
            CancellationToken.None);

        Assert.Equal(2, historicos.Count);
        Assert.Equal(102, historicos[0].TransacaoId);
        Assert.Equal(101, historicos[1].TransacaoId);
    }

    [Fact]
    public async Task DeveRespeitarOrdemMaisAntigos_QuandoListarPorUsuario()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);
        context.HistoricosTransacoesFinanceiras.AddRange(
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 9,
                TipoTransacao = TipoTransacaoFinanceira.Despesa,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 200,
                DataTransacao = new DateOnly(2026, 1, 10),
                Descricao = "Historico 1",
                ValorTransacao = -10m
            },
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 9,
                TipoTransacao = TipoTransacaoFinanceira.Receita,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 201,
                DataTransacao = new DateOnly(2026, 1, 11),
                Descricao = "Historico 2",
                ValorTransacao = 10m
            },
            new HistoricoTransacaoFinanceira
            {
                UsuarioOperacaoId = 9,
                TipoTransacao = TipoTransacaoFinanceira.Reembolso,
                TipoOperacao = TipoOperacaoTransacaoFinanceira.Efetivacao,
                TransacaoId = 202,
                DataTransacao = new DateOnly(2026, 1, 12),
                Descricao = "Historico 3",
                ValorTransacao = 30m
            });
        await context.SaveChangesAsync();

        var repository = new HistoricoTransacaoFinanceiraRepository(context);

        var historicos = await repository.ListarPorUsuarioAsync(
            9,
            3,
            OrdemRegistrosHistoricoTransacaoFinanceira.MaisAntigos,
            CancellationToken.None);

        Assert.Equal(3, historicos.Count);
        Assert.Equal(200, historicos[0].TransacaoId);
        Assert.Equal(201, historicos[1].TransacaoId);
        Assert.Equal(202, historicos[2].TransacaoId);
    }
}
