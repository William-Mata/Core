using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ITentativaLoginRepository
{
    Task<TentativaLoginInvalida?> ObterAsync(string email, CancellationToken cancellationToken = default);
    Task<int> IncrementarAsync(string email, CancellationToken cancellationToken = default);
    Task ZerarAsync(string email, CancellationToken cancellationToken = default);
}
