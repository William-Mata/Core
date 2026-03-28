using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories;

public sealed class TentativaLoginRepository(AppDbContext dbContext) : ITentativaLoginRepository
{
    public Task<TentativaLoginInvalida?> ObterAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.TentativasLoginInvalidas.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public async Task<int> IncrementarAsync(string email, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.TentativasLoginInvalidas.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (item is null)
        {
            item = new TentativaLoginInvalida { UsuarioCadastroId = 1, Email = email, TentativasInvalidas = 1, AtualizadoEmUtc = DateTime.UtcNow };
            dbContext.TentativasLoginInvalidas.Add(item);
        }
        else
        {
            item.TentativasInvalidas += 1;
            item.AtualizadoEmUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return item.TentativasInvalidas;
    }

    public async Task ZerarAsync(string email, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.TentativasLoginInvalidas.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (item is null) return;

        dbContext.TentativasLoginInvalidas.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
