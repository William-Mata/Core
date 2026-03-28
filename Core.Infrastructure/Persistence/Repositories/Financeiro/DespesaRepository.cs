using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class DespesaRepository(AppDbContext dbContext) : IDespesaRepository
{
    public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Include(x => x.AmigosRateio)
            .Include(x => x.TiposRateio)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataLancamento)
            .ToListAsync(cancellationToken);

    public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Include(x => x.AmigosRateio)
            .Include(x => x.TiposRateio)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default)
    {
        dbContext.Despesas.Add(despesa);
        await dbContext.SaveChangesAsync(cancellationToken);
        return despesa;
    }

    public async Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default)
    {
        dbContext.Despesas.Update(despesa);
        await dbContext.SaveChangesAsync(cancellationToken);
        return despesa;
    }
}
