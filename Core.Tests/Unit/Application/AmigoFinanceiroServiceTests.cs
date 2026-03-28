using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Administracao;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;

namespace Core.Tests.Unit.Application;

public sealed class AmigoFinanceiroServiceTests
{
    [Fact]
    public async Task DeveRetornarErro_QuandoUsuarioNaoAutenticado()
    {
        var service = new AmigoFinanceiroService(new UsuarioRepositoryFake(), new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ListarAmigosAsync());

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveListarAmigosAtivos_ExcluindoUsuarioAutenticado()
    {
        var service = new AmigoFinanceiroService(
            new UsuarioRepositoryFake
            {
                Ativos =
                [
                    new Usuario { Id = 1, Nome = "William", Email = "william@email.com" },
                    new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com" },
                    new Usuario { Id = 3, Nome = "Bianca", Email = "bianca@email.com" }
                ]
            },
            new UsuarioAutenticadoProviderFake(1));

        var resultado = await service.ListarAmigosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.DoesNotContain(resultado, x => x.Id == 1);
        Assert.Contains(resultado, x => x.Nome == "Alex" && x.Email == "alex@email.com");
        Assert.Contains(resultado, x => x.Nome == "Bianca" && x.Email == "bianca@email.com");
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public IReadOnlyCollection<Usuario> Ativos { get; set; } = [];

        public Task<IReadOnlyCollection<Usuario>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Ativos);

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Usuario?>(null);

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<Usuario?>(null);

        public Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Modulo>>(Array.Empty<Modulo>());

        public Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Tela>>(Array.Empty<Tela>());

        public Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Funcionalidade>>(Array.Empty<Funcionalidade>());

        public Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task SincronizarPermissoesAsync(int usuarioId, int usuarioCadastroId, IReadOnlyCollection<int> modulosAtivosIds, IReadOnlyCollection<int> telasAtivasIds, IReadOnlyCollection<int> funcionalidadesAtivasIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }
}
