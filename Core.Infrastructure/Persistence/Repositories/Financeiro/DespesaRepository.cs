using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class DespesaRepository(AppDbContext dbContext) : IDespesaRepository
{
    public Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default) =>
        ListarAsync(filtroId: null, descricao: null, competencia: null, dataInicio: null, dataFim: null, cancellationToken);

    public Task<List<Despesa>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(null, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    public Task<List<Despesa>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    public Task<List<Despesa>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Where(x => x.UsuarioCadastroId == usuarioCadastroId)
            .Where(x => x.DespesaOrigemId.HasValue && x.Status == StatusDespesa.PendenteAprovacao)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataLancamento)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Despesa>> ListarEspelhosPorOrigemAsync(long despesaOrigemId, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Where(x => x.DespesaOrigemId == despesaOrigemId)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .ToListAsync(cancellationToken);

    private Task<List<Despesa>> ListarCoreAsync(int? usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var dataInicioInclusiva = dataInicio?.ToDateTime(TimeOnly.MinValue);
        var dataFimExclusiva = dataFim?.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var query = dbContext.Despesas
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Where(x => string.IsNullOrWhiteSpace(filtroId) || x.Id.ToString().Contains(filtroId.Trim()))
            .Where(x => string.IsNullOrWhiteSpace(descricao) || x.Descricao.Contains(descricao.Trim()))
            .Where(x => !dataInicioInclusiva.HasValue || x.DataLancamento >= dataInicioInclusiva.Value)
            .Where(x => !dataFimExclusiva.HasValue || x.DataLancamento < dataFimExclusiva.Value);

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

    public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public Task<List<Despesa>> ObterPorIdsAsync(IReadOnlyCollection<long> ids, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        dbContext.Despesas
            .Where(x => ids.Contains(x.Id) && x.UsuarioCadastroId == usuarioCadastroId)
            .ToListAsync(cancellationToken);

    public Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, null, cancellationToken);

    public Task<Despesa?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, usuarioCadastroId, cancellationToken);

    private Task<Despesa?> ObterCoreAsync(long id, int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.Despesas
            .Where(x => x.Id == id)
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio)
                .ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(cancellationToken);

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
