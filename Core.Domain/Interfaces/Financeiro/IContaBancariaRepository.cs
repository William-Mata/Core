using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IContaBancariaRepository
{
    Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default);
    Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default);
    Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default);
}
