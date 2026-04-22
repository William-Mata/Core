using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Repositories.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Integration.Repositories.Financeiro;

public sealed class ReembolsoRepositoryTests
{
    [Fact]
    public async Task DeveDetectarDespesaVinculadaEmOutroReembolso()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);

        context.Despesas.Add(new Despesa
        {
            Id = 1,
            UsuarioCadastroId = 1,
            Descricao = "Combustivel",
            DataLancamento = new DateTime(2026, 3, 15, 0, 0, 0),
            DataVencimento = new DateOnly(2026, 3, 15),
            TipoDespesa = TipoDespesa.Transporte,
            TipoPagamento = TipoPagamento.Pix,
            Recorrencia = Recorrencia.Unica,
            ValorTotal = 100m,
            ValorLiquido = 100m,
            Status = StatusDespesa.Pendente
        });

        context.Reembolsos.Add(new Reembolso
        {
            Id = 10,
            UsuarioCadastroId = 1,
            Descricao = "Viagem comercial",
            Solicitante = "Joao Silva",
            DataLancamento = new DateTime(2026, 3, 18, 0, 0, 0),
            ValorTotal = 100m,
            Status = StatusReembolso.Aguardando,
            Despesas =
            [
                new ReembolsoDespesa
                {
                    UsuarioCadastroId = 1,
                    DespesaId = 1
                }
            ]
        });

        await context.SaveChangesAsync();

        var repository = new ReembolsoRepository(context);
        var existeConflito = await repository.ExisteDespesaVinculadaEmOutroReembolsoAsync([1], null);

        Assert.True(existeConflito);
    }
}
