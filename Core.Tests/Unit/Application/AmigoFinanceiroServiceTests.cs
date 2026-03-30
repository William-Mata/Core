using Core.Application.DTOs.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Entities.Administracao;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class AmigoFinanceiroServiceTests
{
    [Fact]
    public async Task DeveRetornarErro_QuandoUsuarioNaoAutenticado()
    {
        var service = CriarService(new AmizadeRepositoryFake(), new UsuarioRepositoryFake(), null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ListarAmigosAsync());

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveListarSomenteAmigosAceitos()
    {
        var amizadeRepository = new AmizadeRepositoryFake
        {
            AmigosAceitos =
            [
                new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com" },
                new Usuario { Id = 3, Nome = "Bianca", Email = "bianca@email.com" }
            ]
        };

        var service = CriarService(amizadeRepository, new UsuarioRepositoryFake(), 1);

        var resultado = await service.ListarAmigosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, x => x.Id == 2);
        Assert.Contains(resultado, x => x.Id == 3);
    }

    [Fact]
    public async Task NaoDeveListarUsuariosSemAmizadeAceita_MesmoComUsuariosAtivosOuConvitePendente()
    {
        var amizadeRepository = new AmizadeRepositoryFake
        {
            ConvitePorId = new ConviteAmizade
            {
                Id = 1,
                UsuarioOrigemId = 2,
                UsuarioDestinoId = 1,
                UsuarioCadastroId = 2,
                Status = StatusConviteAmizade.Pendente
            }
        };
        var usuarioRepository = new UsuarioRepositoryFake
        {
            Usuarios =
            [
                new Usuario { Id = 1, Nome = "William", Email = "william@email.com", Ativo = true },
                new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com", Ativo = true },
                new Usuario { Id = 3, Nome = "Bianca", Email = "bianca@email.com", Ativo = true }
            ]
        };
        var service = CriarService(amizadeRepository, usuarioRepository, 1);

        var resultado = await service.ListarAmigosAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task DeveRejeitarEnvioDeConvite_QuandoJaExisteAmizade()
    {
        var amizadeRepository = new AmizadeRepositoryFake { ExisteAmizade = true };
        var usuarioRepository = new UsuarioRepositoryFake
        {
            Usuarios =
            [
                new Usuario { Id = 1, Nome = "William", Email = "william@email.com", Ativo = true },
                new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com", Ativo = true }
            ]
        };

        var service = CriarService(amizadeRepository, usuarioRepository, 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnviarConviteAsync(new EnviarConviteAmizadeRequest(2)));

        Assert.Equal("amizade_ja_existente", ex.Message);
    }

    [Fact]
    public async Task DeveAceitarConviteECriarAmizade()
    {
        var convite = new ConviteAmizade
        {
            Id = 10,
            UsuarioOrigemId = 2,
            UsuarioDestinoId = 1,
            UsuarioCadastroId = 2,
            Status = StatusConviteAmizade.Pendente
        };

        var amizadeRepository = new AmizadeRepositoryFake
        {
            ConvitePorId = convite
        };
        var usuarioRepository = new UsuarioRepositoryFake
        {
            Usuarios =
            [
                new Usuario { Id = 1, Nome = "William", Email = "william@email.com", Ativo = true },
                new Usuario { Id = 2, Nome = "Alex", Email = "alex@email.com", Ativo = true }
            ]
        };

        var service = CriarService(amizadeRepository, usuarioRepository, 1);

        var resposta = await service.AceitarConviteAsync(10);

        Assert.Equal("aceito", resposta.Status);
        Assert.NotNull(amizadeRepository.AmizadeCriada);
        Assert.Equal(1, amizadeRepository.AmizadeCriada!.UsuarioAId);
        Assert.Equal(2, amizadeRepository.AmizadeCriada.UsuarioBId);
    }

    private static AmigoFinanceiroService CriarService(IAmizadeRepository amizadeRepository, IUsuarioRepository usuarioRepository, int? usuarioId) =>
        new(amizadeRepository, usuarioRepository, new UsuarioAutenticadoProviderFake(usuarioId));

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class AmizadeRepositoryFake : IAmizadeRepository
    {
        public IReadOnlyCollection<Usuario> AmigosAceitos { get; set; } = [];
        public ConviteAmizade? ConvitePorId { get; set; }
        public bool ExisteAmizade { get; set; }
        public Amizade? AmizadeCriada { get; private set; }

        public Task<IReadOnlyCollection<Usuario>> ListarAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AmigosAceitos);

        public Task<IReadOnlyCollection<int>> ListarIdsAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<int>>(AmigosAceitos.Select(x => x.Id).ToArray());

        public Task<IReadOnlyCollection<ConviteAmizade>> ListarConvitesPendentesAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ConviteAmizade>>(ConvitePorId is null ? [] : [ConvitePorId]);

        public Task<ConviteAmizade?> ObterConvitePorIdAsync(long conviteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ConvitePorId?.Id == conviteId ? ConvitePorId : null);

        public Task<ConviteAmizade?> ObterConvitePendenteAsync(int usuarioOrigemId, int usuarioDestinoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConviteAmizade?>(null);

        public Task<bool> ExisteAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExisteAmizade);

        public Task<Amizade?> ObterAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Amizade?>(null);

        public Task<ConviteAmizade> CriarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default) =>
            Task.FromResult(convite);

        public Task<ConviteAmizade> AtualizarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default)
        {
            ConvitePorId = convite;
            return Task.FromResult(convite);
        }

        public Task<Amizade> CriarAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default)
        {
            AmizadeCriada = amizade;
            return Task.FromResult(amizade);
        }

        public Task ExcluirAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public IReadOnlyCollection<Usuario> Usuarios { get; set; } = [];

        public Task<IReadOnlyCollection<Usuario>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.Where(x => x.Ativo).ToArray() as IReadOnlyCollection<Usuario>);

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Email == email));

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
}
