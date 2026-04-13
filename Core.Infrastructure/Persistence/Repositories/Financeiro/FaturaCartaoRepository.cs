using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class FaturaCartaoRepository(AppDbContext dbContext) : IFaturaCartaoRepository
{
    public Task<List<FaturaCartao>> ListarPorUsuarioAsync(int usuarioCadastroId, long? cartaoId, string? competencia, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<FaturaCartao>()
            .Where(x => x.UsuarioCadastroId == usuarioCadastroId)
            .AsQueryable();

        if (cartaoId.HasValue)
            query = query.Where(x => x.CartaoId == cartaoId.Value);

        if (!string.IsNullOrWhiteSpace(competencia))
        {
            var competenciaNormalizada = NormalizarCompetencia(competencia);
            query = query.Where(x => x.Competencia == competenciaNormalizada);
        }

        return query
            .OrderByDescending(x => x.Competencia)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<FaturaCartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        dbContext.Set<FaturaCartao>()
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioCadastroId == usuarioCadastroId, cancellationToken);

    public Task<FaturaCartao?> ObterPorCartaoCompetenciaAsync(long cartaoId, int usuarioCadastroId, string competencia, CancellationToken cancellationToken = default)
    {
        var competenciaNormalizada = NormalizarCompetencia(competencia);
        return dbContext.Set<FaturaCartao>()
            .FirstOrDefaultAsync(
                x => x.CartaoId == cartaoId &&
                     x.UsuarioCadastroId == usuarioCadastroId &&
                     x.Competencia == competenciaNormalizada,
                cancellationToken);
    }

    public async Task<FaturaCartao> CriarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default)
    {
        dbContext.Set<FaturaCartao>().Add(fatura);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fatura;
    }

    public async Task<FaturaCartao> AtualizarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default)
    {
        dbContext.Set<FaturaCartao>().Update(fatura);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fatura;
    }

    private static string NormalizarCompetencia(string competencia)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        if (!competenciaMesAno.HasValue)
            return competencia.Trim();

        return $"{competenciaMesAno.Value.Ano:D4}-{competenciaMesAno.Value.Mes:D2}";
    }
}
