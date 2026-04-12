using System.Reflection;
using Core.Application.Contracts.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Infrastructure.Messaging;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Tests.Integration.Messaging;

public sealed class RecorrenciaBackgroundConsumerServiceTests
{
    [Fact]
    public async Task DeveExpandirRecorrenciaFixaDeDespesaDe100Para200_SemDuplicar()
    {
        var dbName = Guid.NewGuid().ToString("N");
        using var scope = await CriarScope(dbName);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = CriarConsumerService(scope.ServiceProvider);

        var dataCadastroOrigem = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc);
        var dataLancamentoOrigem = new DateOnly(2026, 1, 10);

        context.Despesas.Add(new Despesa
        {
            UsuarioCadastroId = 99,
            Descricao = "Plano anual",
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoDespesa = TipoDespesa.Servicos,
            TipoPagamento = TipoPagamento.Pix,
            Recorrencia = Recorrencia.Mensal,
            RecorrenciaFixa = true,
            QuantidadeRecorrencia = 100,
            ValorTotal = 50m,
            ValorLiquido = 50m,
            Status = StatusDespesa.Pendente
        });
        await context.SaveChangesAsync();

        var mensagem100 = new DespesaRecorrenciaBackgroundMessage(
            99,
            1,
            "Plano anual",
            null,
            dataCadastroOrigem,
            dataLancamentoOrigem,
            dataLancamentoOrigem,
            TipoDespesa.Servicos,
            TipoPagamento.Pix,
            Recorrencia.Mensal,
            true,
            100,
            50m,
            0m,
            0m,
            0m,
            0m,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            []);

        await InvocarProcessarDespesaAsync(service, mensagem100);

        var total100 = await context.Despesas.CountAsync(x => x.UsuarioCadastroId == 99 && x.Descricao == "Plano anual");
        Assert.Equal(100, total100);

        var mensagem200 = mensagem100 with { QuantidadeRecorrencia = 200 };
        await InvocarProcessarDespesaAsync(service, mensagem200);

        var despesas = await context.Despesas
            .Where(x => x.UsuarioCadastroId == 99 && x.Descricao == "Plano anual")
            .ToListAsync();

        Assert.Equal(200, despesas.Count);
        Assert.All(despesas, x => Assert.Equal(dataCadastroOrigem, x.DataHoraCadastro));
    }

    [Fact]
    public async Task DeveExpandirRecorrenciaFixaDeReceitaDe100Para200_SemDuplicar()
    {
        var dbName = Guid.NewGuid().ToString("N");
        using var scope = await CriarScope(dbName);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = CriarConsumerService(scope.ServiceProvider);

        var dataCadastroOrigem = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        var dataLancamentoOrigem = new DateOnly(2026, 1, 5);

        context.Receitas.Add(new Receita
        {
            UsuarioCadastroId = 77,
            Descricao = "Contrato fixo",
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoReceita = TipoReceita.Freelance,
            TipoRecebimento = TipoRecebimento.Dinheiro,
            Recorrencia = Recorrencia.Mensal,
            RecorrenciaFixa = true,
            QuantidadeRecorrencia = 100,
            ValorTotal = 100m,
            ValorLiquido = 100m,
            Status = StatusReceita.Pendente
        });
        await context.SaveChangesAsync();

        var mensagem100 = new ReceitaRecorrenciaBackgroundMessage(
            77,
            "Contrato fixo",
            null,
            dataCadastroOrigem,
            dataLancamentoOrigem,
            dataLancamentoOrigem,
            TipoReceita.Freelance,
            TipoRecebimento.Dinheiro,
            Recorrencia.Mensal,
            true,
            100,
            100m,
            0m,
            0m,
            0m,
            0m,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            []);

        await InvocarProcessarReceitaAsync(service, mensagem100);

        var total100 = await context.Receitas.CountAsync(x => x.UsuarioCadastroId == 77 && x.Descricao == "Contrato fixo");
        Assert.Equal(100, total100);

        var mensagem200 = mensagem100 with { QuantidadeRecorrencia = 200 };
        await InvocarProcessarReceitaAsync(service, mensagem200);

        var receitas = await context.Receitas
            .Where(x => x.UsuarioCadastroId == 77 && x.Descricao == "Contrato fixo")
            .ToListAsync();

        Assert.Equal(200, receitas.Count);
        Assert.All(receitas, x => Assert.Equal(dataCadastroOrigem, x.DataHoraCadastro));
    }

    [Fact]
    public async Task DeveCriarReceitasEspelho_ParaRecorrenciasDeDespesaPixEntreContas()
    {
        var dbName = Guid.NewGuid().ToString("N");
        using var scope = await CriarScope(dbName);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = CriarConsumerService(scope.ServiceProvider);

        var dataCadastroOrigem = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc);
        var dataLancamentoOrigem = new DateOnly(2026, 1, 10);

        var despesaBase = new Despesa
        {
            Id = 1,
            UsuarioCadastroId = 99,
            Descricao = "Transferencia recorrente",
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoDespesa = TipoDespesa.Outros,
            TipoPagamento = TipoPagamento.Pix,
            Recorrencia = Recorrencia.Mensal,
            QuantidadeRecorrencia = 3,
            ValorTotal = 50m,
            ValorLiquido = 50m,
            ContaBancariaId = 10,
            ContaDestinoId = 20,
            Status = StatusDespesa.Pendente
        };

        context.Despesas.Add(despesaBase);
        context.Receitas.Add(new Receita
        {
            UsuarioCadastroId = 99,
            Descricao = despesaBase.Descricao,
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoReceita = TipoReceita.Outros,
            TipoRecebimento = TipoRecebimento.Pix,
            Recorrencia = Recorrencia.Mensal,
            QuantidadeRecorrencia = 3,
            ValorTotal = 50m,
            ValorLiquido = 50m,
            ContaBancariaId = 20,
            ContaDestinoId = 10,
            DespesaTransferenciaId = 1,
            Status = StatusReceita.Pendente
        });
        await context.SaveChangesAsync();

        var mensagem = new DespesaRecorrenciaBackgroundMessage(
            99,
            1,
            "Transferencia recorrente",
            null,
            dataCadastroOrigem,
            dataLancamentoOrigem,
            dataLancamentoOrigem,
            TipoDespesa.Outros,
            TipoPagamento.Pix,
            Recorrencia.Mensal,
            false,
            3,
            50m,
            0m,
            0m,
            0m,
            0m,
            10,
            20,
            null,
            null,
            null,
            [],
            [],
            []);

        await InvocarProcessarDespesaAsync(service, mensagem);

        var despesasSerie = await context.Despesas
            .Where(x => x.UsuarioCadastroId == 99 && x.Descricao == "Transferencia recorrente" && x.DespesaOrigemId == null)
            .OrderBy(x => x.DataLancamento)
            .ToListAsync();
        var receitasSerie = await context.Receitas
            .Where(x => x.UsuarioCadastroId == 99 && x.Descricao == "Transferencia recorrente")
            .OrderBy(x => x.DataLancamento)
            .ToListAsync();

        Assert.Equal(3, despesasSerie.Count);
        Assert.Equal(3, receitasSerie.Count);
        Assert.Equal(2, despesasSerie.Count(x => x.ReceitaTransferenciaId.HasValue));
        Assert.All(receitasSerie, x => Assert.Equal(TipoRecebimento.Pix, x.TipoRecebimento));
        Assert.All(receitasSerie, x => Assert.Equal(20, x.ContaBancariaId));
        Assert.All(receitasSerie, x => Assert.Equal(10, x.ContaDestinoId));
    }

    [Fact]
    public async Task DeveCriarDespesasEspelho_ParaRecorrenciasDeReceitaPixEntreContas()
    {
        var dbName = Guid.NewGuid().ToString("N");
        using var scope = await CriarScope(dbName);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = CriarConsumerService(scope.ServiceProvider);

        var dataCadastroOrigem = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var dataLancamentoOrigem = new DateOnly(2026, 1, 15);

        var receitaBase = new Receita
        {
            Id = 1,
            UsuarioCadastroId = 99,
            Descricao = "Entrada recorrente",
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoReceita = TipoReceita.Outros,
            TipoRecebimento = TipoRecebimento.Pix,
            Recorrencia = Recorrencia.Mensal,
            QuantidadeRecorrencia = 3,
            ValorTotal = 80m,
            ValorLiquido = 80m,
            ContaBancariaId = 20,
            ContaDestinoId = 10,
            Status = StatusReceita.Pendente
        };

        context.Receitas.Add(receitaBase);
        context.Despesas.Add(new Despesa
        {
            UsuarioCadastroId = 99,
            Descricao = receitaBase.Descricao,
            DataHoraCadastro = dataCadastroOrigem,
            DataLancamento = dataLancamentoOrigem,
            DataVencimento = dataLancamentoOrigem,
            TipoDespesa = TipoDespesa.Outros,
            TipoPagamento = TipoPagamento.Pix,
            Recorrencia = Recorrencia.Mensal,
            QuantidadeRecorrencia = 3,
            ValorTotal = 80m,
            ValorLiquido = 80m,
            ContaBancariaId = 10,
            ContaDestinoId = 20,
            ReceitaTransferenciaId = 1,
            Status = StatusDespesa.Pendente
        });
        await context.SaveChangesAsync();

        var mensagem = new ReceitaRecorrenciaBackgroundMessage(
            99,
            "Entrada recorrente",
            null,
            dataCadastroOrigem,
            dataLancamentoOrigem,
            dataLancamentoOrigem,
            TipoReceita.Outros,
            TipoRecebimento.Pix,
            Recorrencia.Mensal,
            false,
            3,
            80m,
            0m,
            0m,
            0m,
            0m,
            20,
            10,
            null,
            null,
            null,
            [],
            [],
            []);

        await InvocarProcessarReceitaAsync(service, mensagem);

        var receitasSerie = await context.Receitas
            .Where(x => x.UsuarioCadastroId == 99 && x.Descricao == "Entrada recorrente" && x.ReceitaOrigemId == null)
            .OrderBy(x => x.DataLancamento)
            .ToListAsync();
        var despesasSerie = await context.Despesas
            .Where(x => x.UsuarioCadastroId == 99 && x.Descricao == "Entrada recorrente")
            .OrderBy(x => x.DataLancamento)
            .ToListAsync();

        Assert.Equal(3, receitasSerie.Count);
        Assert.Equal(3, despesasSerie.Count);
        Assert.Equal(2, receitasSerie.Count(x => x.DespesaTransferenciaId.HasValue));
        Assert.All(despesasSerie, x => Assert.Equal(TipoPagamento.Pix, x.TipoPagamento));
        Assert.All(despesasSerie, x => Assert.Equal(10, x.ContaBancariaId));
        Assert.All(despesasSerie, x => Assert.Equal(20, x.ContaDestinoId));
    }

    private static RabbitMqRecorrenciaBackgroundConsumerService CriarConsumerService(IServiceProvider provider)
    {
        var options = Options.Create(new RabbitMqOptions());
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<RabbitMqRecorrenciaBackgroundConsumerService>>();
        return new RabbitMqRecorrenciaBackgroundConsumerService(options, scopeFactory, logger);
    }

    private static async Task<IServiceScope> CriarScope(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        return scope;
    }

    private static async Task InvocarProcessarDespesaAsync(RabbitMqRecorrenciaBackgroundConsumerService service, DespesaRecorrenciaBackgroundMessage message)
    {
        var method = typeof(RabbitMqRecorrenciaBackgroundConsumerService).GetMethod("ProcessarDespesaAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Metodo ProcessarDespesaAsync nao encontrado.");

        var task = (Task?)method.Invoke(service, [message, CancellationToken.None])
            ?? throw new InvalidOperationException("Invocacao de ProcessarDespesaAsync retornou nulo.");

        await task;
    }

    private static async Task InvocarProcessarReceitaAsync(RabbitMqRecorrenciaBackgroundConsumerService service, ReceitaRecorrenciaBackgroundMessage message)
    {
        var method = typeof(RabbitMqRecorrenciaBackgroundConsumerService).GetMethod("ProcessarReceitaAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Metodo ProcessarReceitaAsync nao encontrado.");

        var task = (Task?)method.Invoke(service, [message, CancellationToken.None])
            ?? throw new InvalidOperationException("Invocacao de ProcessarReceitaAsync retornou nulo.");

        await task;
    }
}
