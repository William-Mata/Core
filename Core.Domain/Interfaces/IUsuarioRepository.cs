using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default);
    Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task SincronizarPermissoesAsync(int usuarioId, int usuarioCadastroId, IReadOnlyCollection<int> modulosAtivosIds, IReadOnlyCollection<int> telasAtivasIds, IReadOnlyCollection<int> funcionalidadesAtivasIds, CancellationToken cancellationToken = default);
    Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default);
}
