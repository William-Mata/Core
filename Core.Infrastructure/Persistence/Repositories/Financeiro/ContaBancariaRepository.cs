using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ContaBancariaRepository(AppDbContext dbContext) : IContaBancariaRepository
{
    public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.ContasBancarias
            .Include(x => x.Extrato)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataAbertura)
            .ToListAsync(cancellationToken);

    public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.ContasBancarias
            .Include(x => x.Extrato)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
    {
        dbContext.ContasBancarias.Add(conta);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conta;
    }

    public async Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
    {
        dbContext.ContasBancarias.Update(conta);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conta;
    }
}
