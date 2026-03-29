using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ReembolsoRepository(AppDbContext dbContext) : IReembolsoRepository
{
    public Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Reembolso>()
            .Include(x => x.Despesas)
            .OrderByDescending(x => x.DataLancamento)
            .ThenByDescending(x => x.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtroId))
        {
            query = query.Where(x => x.Id.ToString().Contains(filtroId.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(descricao))
        {
            var termo = descricao.Trim();
            query = query.Where(x => x.Descricao.Contains(termo) || x.Solicitante.Contains(termo));
        }

        if (dataInicio.HasValue)
        {
            query = query.Where(x => x.DataLancamento >= dataInicio.Value);
        }

        if (dataFim.HasValue)
        {
            query = query.Where(x => x.DataLancamento <= dataFim.Value);
        }

        return query.ToListAsync(cancellationToken);
    }

    public Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Reembolso>()
            .Include(x => x.Despesas)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Reembolso> CriarAsync(Reembolso reembolso, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Reembolso>().Add(reembolso);
        await dbContext.SaveChangesAsync(cancellationToken);
        return reembolso;
    }

    public async Task<Reembolso> AtualizarAsync(Reembolso reembolso, CancellationToken cancellationToken = default)
    {
        var despesasAtuais = await dbContext.Set<ReembolsoDespesa>()
            .Where(x => x.ReembolsoId == reembolso.Id)
            .ToListAsync(cancellationToken);

        dbContext.Set<ReembolsoDespesa>().RemoveRange(despesasAtuais);
        dbContext.Entry(reembolso).State = EntityState.Modified;
        dbContext.Set<ReembolsoDespesa>().AddRange(reembolso.Despesas);

        await dbContext.SaveChangesAsync(cancellationToken);
        return reembolso;
    }

    public async Task ExcluirAsync(Reembolso reembolso, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Reembolso>().Remove(reembolso);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ReembolsoDespesa>()
            .AnyAsync(
                x => despesasIds.Contains(x.DespesaId) &&
                     (!reembolsoIgnoradoId.HasValue || x.ReembolsoId != reembolsoIgnoradoId.Value),
                cancellationToken);
}
