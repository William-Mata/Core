using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class CartaoRepository(AppDbContext dbContext) : ICartaoRepository
{
    public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.Cartoes
            .Include(x => x.Lancamentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataVencimentoCartao)
            .ToListAsync(cancellationToken);

    public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Cartoes
            .Include(x => x.Lancamentos)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Cartao> CriarAsync(Cartao cartao, CancellationToken cancellationToken = default)
    {
        dbContext.Cartoes.Add(cartao);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cartao;
    }

    public async Task<Cartao> AtualizarAsync(Cartao cartao, CancellationToken cancellationToken = default)
    {
        dbContext.Cartoes.Update(cartao);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cartao;
    }
}
