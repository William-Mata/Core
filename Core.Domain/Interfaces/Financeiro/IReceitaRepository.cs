using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IReceitaRepository
{
    Task<List<Receita>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Receita?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Receita> CriarAsync(Receita receita, CancellationToken cancellationToken = default);
    Task<Receita> AtualizarAsync(Receita receita, CancellationToken cancellationToken = default);
}
