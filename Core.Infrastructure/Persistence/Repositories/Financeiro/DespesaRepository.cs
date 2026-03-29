using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class DespesaRepository(AppDbContext dbContext) : IDespesaRepository
{
    public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataLancamento)
            .ToListAsync(cancellationToken);

    public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
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
        var documentosAtuais = await dbContext.Set<Documento>()
            .Where(x => x.DespesaId == despesa.Id)
            .ToListAsync(cancellationToken);

        dbContext.Set<Documento>().RemoveRange(documentosAtuais);
        dbContext.Despesas.Update(despesa);
        await dbContext.SaveChangesAsync(cancellationToken);
        return despesa;
    }
}
