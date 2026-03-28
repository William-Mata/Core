using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Interfaces.Administracao;
using Core.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Administracao;

public sealed class AutenticacaoRepository(AppDbContext dbContext) : IAutenticacaoRepository
{
    public async Task<Usuario?> ObterUsuarioAtivoPorCredenciaisAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await QueryUsuarioCompleto()
            .FirstOrDefaultAsync(x => x.Email == email && x.Ativo, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        return SenhaHasher.Verificar(senha, usuario.SenhaHash) ? usuario : null;
    }

    public Task<Usuario?> ObterUsuarioAtivoPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        QueryUsuarioCompleto().FirstOrDefaultAsync(x => x.Email == email && x.Ativo, cancellationToken);

    public Task<Usuario?> ObterUsuarioAtivoPorIdAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        QueryUsuarioCompleto().FirstOrDefaultAsync(x => x.Id == usuarioId && x.Ativo, cancellationToken);

    public async Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Modulos
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Telas
            .AsNoTracking()
            .OrderBy(x => x.ModuloId)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Funcionalidades
            .AsNoTracking()
            .OrderBy(x => x.TelaId)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task DefinirPrimeiraSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default)
    {
        usuario.SenhaHash = SenhaHasher.Hash(senha);
        usuario.PrimeiroAcesso = false;
        dbContext.Usuarios.Update(usuario);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> ObterRefreshTokenValidoAsync(string token, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token && x.RevogadoEmUtc == null && x.ExpiraEmUtc > agora, cancellationToken);
    }

    public async Task SalvarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevogarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        dbContext.RefreshTokens.Update(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Usuario> QueryUsuarioCompleto() =>
        dbContext.Usuarios
            .Include(x => x.Modulos)
                .ThenInclude(x => x.Modulo!)
            .Include(x => x.Telas)
                .ThenInclude(x => x.Tela!)
                    .ThenInclude(x => x.Modulo!)
            .Include(x => x.Funcionalidades)
                .ThenInclude(x => x.Funcionalidade!)
                    .ThenInclude(x => x.Tela!)
                        .ThenInclude(x => x.Modulo!);
}
