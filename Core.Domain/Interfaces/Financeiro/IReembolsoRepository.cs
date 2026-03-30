using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IReembolsoRepository
{
    Task<List<Reembolso>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default);
    Task<List<Reembolso>> ListarAsync(int usuarioCadastroId, string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default);
    Task<Reembolso?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Reembolso?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<Reembolso> CriarAsync(Reembolso reembolso, CancellationToken cancellationToken = default);
    Task<Reembolso> AtualizarAsync(Reembolso reembolso, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Reembolso reembolso, CancellationToken cancellationToken = default);
    Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteDespesaVinculadaEmOutroReembolsoAsync(int usuarioCadastroId, IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken = default);
}
