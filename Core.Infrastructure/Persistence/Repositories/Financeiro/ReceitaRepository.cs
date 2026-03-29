using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ReceitaRepository(AppDbContext dbContext) : IReceitaRepository
{
    public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.Receitas
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio).ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio).ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataLancamento)
            .ToListAsync(cancellationToken);

    public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Receitas
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio).ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio).ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default)
    {
        dbContext.Receitas.Add(receita);
        await dbContext.SaveChangesAsync(cancellationToken);
        return receita;
    }

    public async Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default)
    {
        var documentosAtuais = await dbContext.Set<Documento>()
            .Where(x => x.ReceitaId == receita.Id)
            .ToListAsync(cancellationToken);

        dbContext.Set<Documento>().RemoveRange(documentosAtuais);
        dbContext.Receitas.Update(receita);
        await dbContext.SaveChangesAsync(cancellationToken);
        return receita;
    }
}
