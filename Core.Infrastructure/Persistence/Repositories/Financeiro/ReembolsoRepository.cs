using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ReembolsoRepository(AppDbContext dbContext) : IReembolsoRepository
{
    public Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
        => ListarCoreAsync(null, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    public Task<List<Reembolso>> ListarAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
        => ListarCoreAsync(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    private Task<List<Reembolso>> ListarCoreAsync(int? usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var query = dbContext.Set<Reembolso>().AsQueryable();

        if (usuarioCadastroId.HasValue)
        {
            query = query.Where(x => x.UsuarioCadastroId == usuarioCadastroId.Value);
        }

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
            var dataInicioInclusiva = dataInicio.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.DataLancamento >= dataInicioInclusiva);
        }

        if (dataFim.HasValue)
        {
            var dataFimExclusiva = dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.DataLancamento < dataFimExclusiva);
        }

        if (competenciaMesAno.HasValue)
        {
            var competenciaNormalizada = $"{competenciaMesAno.Value.Ano:D4}-{competenciaMesAno.Value.Mes:D2}";
            query = query.Where(x => x.Competencia == competenciaNormalizada);
        }

        return query
            .OrderByDescending(x => x.DataLancamento)
            .ThenByDescending(x => x.DataEfetivacao.HasValue)
            .ThenByDescending(x => x.DataEfetivacao)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, null, cancellationToken);

    public Task<Reembolso?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, usuarioCadastroId, cancellationToken);

    private Task<Reembolso?> ObterCoreAsync(long id, int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.Set<Reembolso>()
            .Where(x => x.Id == id)
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.Despesas)
            .Include(x => x.Documentos)
            .FirstOrDefaultAsync(cancellationToken);

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
        var documentosAtuais = await dbContext.Set<Documento>()
            .Where(x => x.ReembolsoId == reembolso.Id)
            .ToListAsync(cancellationToken);

        dbContext.Set<ReembolsoDespesa>().RemoveRange(despesasAtuais);
        dbContext.Set<Documento>().RemoveRange(documentosAtuais);
        dbContext.Entry(reembolso).State = EntityState.Modified;
        dbContext.Set<ReembolsoDespesa>().AddRange(reembolso.Despesas);
        dbContext.Set<Documento>().AddRange(reembolso.Documentos);

        await dbContext.SaveChangesAsync(cancellationToken);
        return reembolso;
    }

    public async Task ExcluirAsync(Reembolso reembolso, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Reembolso>().Remove(reembolso);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) =>
        ExisteDespesaVinculadaEmOutroReembolsoAsyncCore(null, despesasIds, reembolsoIgnoradoId, cancellationToken);

    public Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(int usuarioCadastroId, IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default) =>
        ExisteDespesaVinculadaEmOutroReembolsoAsyncCore(usuarioCadastroId, despesasIds, reembolsoIgnoradoId, cancellationToken);

    private Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsyncCore(int? usuarioCadastroId, IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken) =>
        dbContext.Set<Reembolso>()
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Where(x => !reembolsoIgnoradoId.HasValue || x.Id != reembolsoIgnoradoId.Value)
            .SelectMany(x => x.Despesas)
            .AnyAsync(x => despesasIds.Contains(x.DespesaId), cancellationToken);
}
