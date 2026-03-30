using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface ICartaoRepository
{
    Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default);
    Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default);
    Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default);
}
