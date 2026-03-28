using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;

namespace Core.Domain.Interfaces.Administracao;

public interface IAutenticacaoRepository
{
    Task<Usuario?> ObterUsuarioAtivoPorCredenciaisAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioAtivoPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioAtivoPorIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default);
    Task DefinirPrimeiraSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default);
    Task<RefreshToken?> ObterRefreshTokenValidoAsync(string token, CancellationToken cancellationToken = default);
    Task SalvarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevogarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
