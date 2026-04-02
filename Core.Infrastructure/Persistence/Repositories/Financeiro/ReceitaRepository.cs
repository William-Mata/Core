using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ReceitaRepository(AppDbContext dbContext) : IReceitaRepository
{
    public Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default) =>
        ListarAsync(filtroId: null, descricao: null, competencia: null, dataInicio: null, dataFim: null, cancellationToken);

    public Task<List<Receita>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(null, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    public Task<List<Receita>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(usuarioCadastroId, filtroId, descricao, competencia, dataInicio, dataFim, cancellationToken);

    public Task<List<Receita>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        dbContext.Receitas
            .Where(x => x.UsuarioCadastroId == usuarioCadastroId)
            .Where(x => x.ReceitaOrigemId.HasValue && x.Status == Core.Domain.Enums.StatusReceita.PendenteAprovacao)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio).ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio).ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataLancamento)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Receita>> ListarEspelhosPorOrigemAsync(long receitaOrigemId, CancellationToken cancellationToken = default) =>
        dbContext.Receitas
            .Where(x => x.ReceitaOrigemId == receitaOrigemId)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio).ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio).ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .ToListAsync(cancellationToken);

    private Task<List<Receita>> ListarCoreAsync(int? usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken)
    {
        var competenciaMesAno = CompetenciaFiltroHelper.ResolverMesAno(competencia);
        var query = dbContext.Receitas
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Where(x => string.IsNullOrWhiteSpace(filtroId) || x.Id.ToString().Contains(filtroId.Trim()))
            .Where(x => string.IsNullOrWhiteSpace(descricao) || x.Descricao.Contains(descricao.Trim()))
            .Where(x => !dataInicio.HasValue || x.DataLancamento >= dataInicio.Value)
            .Where(x => !dataFim.HasValue || x.DataLancamento <= dataFim.Value);

        if (competenciaMesAno.HasValue)
        {
            query = query.Where(x =>
                x.DataLancamento.Year == competenciaMesAno.Value.Ano &&
                x.DataLancamento.Month == competenciaMesAno.Value.Mes);
        }

        return query
            .OrderByDescending(x => x.DataLancamento)
            .ToListAsync(cancellationToken);
    }

    public Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, null, cancellationToken);

    public Task<Receita?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, usuarioCadastroId, cancellationToken);

    private Task<Receita?> ObterCoreAsync(long id, int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.Receitas
            .Where(x => x.Id == id)
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.AmigosRateio)
            .Include(x => x.AreasRateio).ThenInclude(x => x.Area)
            .Include(x => x.AreasRateio).ThenInclude(x => x.SubArea)
            .Include(x => x.Documentos)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(cancellationToken);

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
