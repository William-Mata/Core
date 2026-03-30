using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class CartaoRepository(AppDbContext dbContext) : ICartaoRepository
{
    public Task<List<Cartao>> ListarAsync(CancellationToken cancellationToken = default) =>
        ListarCoreAsync(null, cancellationToken);

    public Task<List<Cartao>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(usuarioCadastroId, cancellationToken);

    private Task<List<Cartao>> ListarCoreAsync(int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.Cartoes
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.Lancamentos)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataVencimentoCartao)
            .ToListAsync(cancellationToken);

    public Task<Cartao?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, null, cancellationToken);

    public Task<Cartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, usuarioCadastroId, cancellationToken);

    private Task<Cartao?> ObterCoreAsync(long id, int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.Cartoes
            .Where(x => x.Id == id)
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.Lancamentos)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(cancellationToken);

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
