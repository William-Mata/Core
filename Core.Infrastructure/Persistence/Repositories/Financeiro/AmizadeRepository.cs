using Core.Domain.Entities.Administracao;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class AmizadeRepository(AppDbContext dbContext) : IAmizadeRepository
{
    public async Task<IReadOnlyCollection<Usuario>> ListarAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var amigosIds = await ListarIdsAmigosAceitosAsync(usuarioId, cancellationToken);
        if (amigosIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Usuarios
            .AsNoTracking()
            .Where(x => x.Ativo && amigosIds.Contains(x.Id))
            .OrderBy(x => x.Nome)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> ListarIdsAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Amizades
            .AsNoTracking()
            .Where(x => x.UsuarioAId == usuarioId || x.UsuarioBId == usuarioId)
            .Select(x => x.UsuarioAId == usuarioId ? x.UsuarioBId : x.UsuarioAId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConviteAmizade>> ListarConvitesPendentesAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ConvitesAmizade
            .AsNoTracking()
            .Where(x => x.Status == StatusConviteAmizade.Pendente)
            .Where(x => x.UsuarioOrigemId == usuarioId || x.UsuarioDestinoId == usuarioId)
            .OrderByDescending(x => x.DataHoraCadastro)
            .ToArrayAsync(cancellationToken);
    }

    public Task<ConviteAmizade?> ObterConvitePorIdAsync(long conviteId, CancellationToken cancellationToken = default) =>
        dbContext.ConvitesAmizade
            .FirstOrDefaultAsync(x => x.Id == conviteId, cancellationToken);

    public Task<ConviteAmizade?> ObterConvitePendenteAsync(int usuarioOrigemId, int usuarioDestinoId, CancellationToken cancellationToken = default) =>
        dbContext.ConvitesAmizade
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UsuarioOrigemId == usuarioOrigemId &&
                     x.UsuarioDestinoId == usuarioDestinoId &&
                     x.Status == StatusConviteAmizade.Pendente,
                cancellationToken);

    public Task<bool> ExisteAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default)
    {
        var (usuarioAId, usuarioBId) = OrdenarPar(usuarioId, amigoId);
        return dbContext.Amizades
            .AsNoTracking()
            .AnyAsync(x => x.UsuarioAId == usuarioAId && x.UsuarioBId == usuarioBId, cancellationToken);
    }

    public Task<Amizade?> ObterAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default)
    {
        var (usuarioAId, usuarioBId) = OrdenarPar(usuarioId, amigoId);
        return dbContext.Amizades
            .FirstOrDefaultAsync(x => x.UsuarioAId == usuarioAId && x.UsuarioBId == usuarioBId, cancellationToken);
    }

    public async Task<ConviteAmizade> CriarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default)
    {
        dbContext.ConvitesAmizade.Add(convite);
        await dbContext.SaveChangesAsync(cancellationToken);
        return convite;
    }

    public async Task<ConviteAmizade> AtualizarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default)
    {
        dbContext.ConvitesAmizade.Update(convite);
        await dbContext.SaveChangesAsync(cancellationToken);
        return convite;
    }

    public async Task<Amizade> CriarAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default)
    {
        dbContext.Amizades.Add(amizade);
        await dbContext.SaveChangesAsync(cancellationToken);
        return amizade;
    }

    public async Task ExcluirAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default)
    {
        dbContext.Amizades.Remove(amizade);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (int UsuarioAId, int UsuarioBId) OrdenarPar(int usuarioId, int amigoId) =>
        usuarioId < amigoId ? (usuarioId, amigoId) : (amigoId, usuarioId);
}
