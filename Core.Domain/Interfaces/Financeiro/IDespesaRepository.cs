using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IDespesaRepository
{
    Task<List<Despesa>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Despesa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Despesa> CriarAsync(Despesa despesa, CancellationToken cancellationToken = default);
    Task<Despesa> AtualizarAsync(Despesa despesa, CancellationToken cancellationToken = default);
}
