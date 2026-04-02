using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IReceitaRepository
{
    Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default);
    Task<List<Receita>> ListarAsync(string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default);
    Task<List<Receita>> ListarPorUsuarioAsync(int usuarioCadastroId, string? filtroId, string? descricao, string? competencia, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default);
    Task<List<Receita>> ListarPendentesAprovacaoPorUsuarioAsync(int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<List<Receita>> ListarEspelhosPorOrigemAsync(long receitaOrigemId, CancellationToken cancellationToken = default);
    Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Receita?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default);
    Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default);
}
