using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;

namespace Core.Tests.Unit.Application;

public sealed class AutenticacaoServiceTests
{
    [Fact]
    public async Task DeveGerarJwt_NoLoginComSucesso()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Email = "admin@core.com",
            Nome = "Admin",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 1,
            SenhaHash = "hash"
        };

        var authRepo = new AuthRepoFake
        {
            UsuarioPorEmail = usuario,
            UsuarioPorCredenciais = usuario
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());
        var response = await service.EntrarAsync(new EntrarRequest("admin@core.com", "Admin@123456"));

        Assert.Equal("jwt-token-valido", response.AccessToken);
    }

    [Fact]
    public async Task DeveBloquearLogin_AposCincoTentativasInvalidas()
    {
        var authRepo = new AuthRepoFake();
        var tentativaRepo = new TentativaRepoFake();
        var service = new AutenticacaoService(authRepo, tentativaRepo, new TokenServiceFake());

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<DomainException>(() => service.EntrarAsync(new EntrarRequest("admin@core.com", "errada")));
        }

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EntrarAsync(new EntrarRequest("admin@core.com", "errada")));
        Assert.Equal("login_bloqueado", ex.Message);
    }

    [Fact]
    public async Task DeveExigirCriacaoDeSenha_NoPrimeiroAcesso()
    {
        var authRepo = new AuthRepoFake
        {
            UsuarioPorEmail = new Usuario
            {
                Id = 1,
                Email = "admin@core.com",
                Nome = "Admin",
                Ativo = true,
                PrimeiroAcesso = true,
                SenhaHash = "qualquer"
            }
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.EntrarAsync(new EntrarRequest("admin@core.com", "1234567890")));
        Assert.Equal("primeiro_acesso_requer_criacao_senha", ex.Message);
    }

    [Fact]
    public async Task DeveCriarPrimeiraSenha_ComSucesso()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Email = "admin@core.com",
            Nome = "Admin",
            Ativo = true,
            PrimeiroAcesso = true,
            SenhaHash = string.Empty
        };

        var authRepo = new AuthRepoFake
        {
            UsuarioPorEmail = usuario
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());
        var mensagem = await service.CriarPrimeiraSenhaAsync(new CriarPrimeiraSenhaRequest("admin@core.com", "1234567890", "1234567890"));

        Assert.Equal("Senha criada com sucesso.", mensagem);
        Assert.True(authRepo.DefiniuPrimeiraSenha);
    }

    [Fact]
    public async Task DeveImpedirCriacaoDaPrimeiraSenha_QuandoJaEstiverDefinida()
    {
        var authRepo = new AuthRepoFake
        {
            UsuarioPorEmail = new Usuario
            {
                Id = 1,
                Email = "admin@core.com",
                Nome = "Admin",
                Ativo = true,
                PrimeiroAcesso = false,
                SenhaHash = "hash"
            }
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarPrimeiraSenhaAsync(new CriarPrimeiraSenhaRequest("admin@core.com", "1234567890", "1234567890")));

        Assert.Equal("primeira_senha_ja_definida", ex.Message);
    }

    [Fact]
    public async Task DeveRenovarToken_ComSucesso()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Email = "admin@core.com",
            Nome = "Admin",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 1,
            SenhaHash = "hash"
        };

        var authRepo = new AuthRepoFake
        {
            UsuarioPorId = usuario,
            RefreshTokenValido = new RefreshToken
            {
                Id = 10,
                UsuarioId = 1,
                UsuarioCadastroId = 1,
                Token = "refresh-token-valido",
                ExpiraEmUtc = DateTime.UtcNow.AddDays(1)
            }
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());
        var response = await service.RenovarTokenAsync(new RenovarTokenRequest("refresh-token-valido"));

        Assert.Equal("jwt-token-valido", response.AccessToken);
        Assert.NotEqual("refresh-token-valido", response.RefreshToken);
        Assert.True(authRepo.RevogouRefreshToken);
        Assert.NotNull(authRepo.RefreshTokenSalvo);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoRefreshTokenForInvalido()
    {
        var authRepo = new AuthRepoFake();
        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.RenovarTokenAsync(new RenovarTokenRequest("token-invalido")));

        Assert.Equal("refresh_token_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveMapearModulosTelasEFuncionalidades_NoLoginCompleto()
    {
        var modulo = new Modulo { Id = 1, Nome = "Geral", Status = true };
        var moduloInativo = new Modulo { Id = 2, Nome = "Administracao", Status = false };
        var tela = new Tela { Id = 2, Nome = "Painel do Usuario", Status = true, ModuloId = 1, Modulo = modulo };
        var telaInativa = new Tela { Id = 30, Nome = "Administracao", Status = false, ModuloId = 2, Modulo = moduloInativo };
        var funcionalidade = new Funcionalidade { Id = 7, Nome = "Visualizar", Status = true, TelaId = 2, Tela = tela };
        var funcionalidadeInativa = new Funcionalidade { Id = 8, Nome = "Criar", Status = false, TelaId = 30, Tela = telaInativa };

        var usuario = new Usuario
        {
            Id = 1,
            Email = "admin@core.com",
            Nome = "Admin",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 1,
            SenhaHash = "hash",
            Modulos = [new UsuarioModulo { UsuarioId = 1, ModuloId = 1, Status = true, Modulo = modulo }],
            Telas = [new UsuarioTela { UsuarioId = 1, TelaId = 2, Status = true, Tela = tela }],
            Funcionalidades = [new UsuarioFuncionalidade { UsuarioId = 1, FuncionalidadeId = 7, Status = true, Funcionalidade = funcionalidade }]
        };

        var authRepo = new AuthRepoFake
        {
            UsuarioPorEmail = usuario,
            UsuarioPorCredenciais = usuario,
            Modulos = [modulo, moduloInativo],
            Telas = [tela, telaInativa],
            Funcionalidades = [funcionalidade, funcionalidadeInativa]
        };

        var service = new AutenticacaoService(authRepo, new TentativaRepoFake(), new TokenServiceFake());
        var response = await service.EntrarAsync(new EntrarRequest("admin@core.com", "Admin@123456"));

        var modulos = response.Usuario.ModulosAtivos.ToArray();
        var moduloResponse = modulos[0];
        var telaResponse = Assert.Single(moduloResponse.Telas);
        var funcionalidadeResponse = Assert.Single(telaResponse.Funcionalidades);
        var moduloInativoResponse = modulos[1];
        var telaInativaResponse = Assert.Single(moduloInativoResponse.Telas);
        var funcionalidadeInativaResponse = Assert.Single(telaInativaResponse.Funcionalidades);

        Assert.Equal(1, moduloResponse.Id);
        Assert.Equal("Geral", moduloResponse.Nome);
        Assert.Equal(1, moduloResponse.Status);
        Assert.Equal(2, telaResponse.Id);
        Assert.Equal("Painel do Usuario", telaResponse.Nome);
        Assert.Equal(1, telaResponse.Status);
        Assert.Equal(7, funcionalidadeResponse.Id);
        Assert.Equal("Visualizar", funcionalidadeResponse.Nome);
        Assert.Equal(1, funcionalidadeResponse.Status);
        Assert.Equal(2, moduloInativoResponse.Id);
        Assert.Equal(0, moduloInativoResponse.Status);
        Assert.Equal(30, telaInativaResponse.Id);
        Assert.Equal(0, telaInativaResponse.Status);
        Assert.Equal(8, funcionalidadeInativaResponse.Id);
        Assert.Equal(0, funcionalidadeInativaResponse.Status);
    }

    private sealed class AuthRepoFake : IAutenticacaoRepository
    {
        public Usuario? UsuarioPorEmail { get; set; }
        public Usuario? UsuarioPorCredenciais { get; set; }
        public Usuario? UsuarioPorId { get; set; }
        public RefreshToken? RefreshTokenValido { get; set; }
        public IReadOnlyCollection<Modulo> Modulos { get; set; } = [];
        public IReadOnlyCollection<Tela> Telas { get; set; } = [];
        public IReadOnlyCollection<Funcionalidade> Funcionalidades { get; set; } = [];
        public bool DefiniuPrimeiraSenha { get; private set; }
        public bool RevogouRefreshToken { get; private set; }
        public RefreshToken? RefreshTokenSalvo { get; private set; }

        public Task<Usuario?> ObterUsuarioAtivoPorCredenciaisAsync(string email, string senha, CancellationToken cancellationToken = default)
            => Task.FromResult(UsuarioPorCredenciais);

        public Task<Usuario?> ObterUsuarioAtivoPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(UsuarioPorEmail);

        public Task<Usuario?> ObterUsuarioAtivoPorIdAsync(int usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(UsuarioPorId);

        public Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Modulos);

        public Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Telas);

        public Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Funcionalidades);

        public Task DefinirPrimeiraSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default)
        {
            DefiniuPrimeiraSenha = true;
            usuario.PrimeiroAcesso = false;
            usuario.SenhaHash = "hash";
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> ObterRefreshTokenValidoAsync(string token, CancellationToken cancellationToken = default)
            => Task.FromResult(RefreshTokenValido);

        public Task SalvarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            RefreshTokenSalvo = refreshToken;
            return Task.CompletedTask;
        }

        public Task RevogarRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            RevogouRefreshToken = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TentativaRepoFake : ITentativaLoginRepository
    {
        private readonly Dictionary<string, int> _dados = new();

        public Task<TentativaLoginInvalida?> ObterAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_dados.TryGetValue(email, out var v) ? new TentativaLoginInvalida { Email = email, TentativasInvalidas = v } : null);

        public Task<int> IncrementarAsync(string email, CancellationToken cancellationToken = default)
        {
            _dados[email] = _dados.TryGetValue(email, out var atual) ? atual + 1 : 1;
            return Task.FromResult(_dados[email]);
        }

        public Task ZerarAsync(string email, CancellationToken cancellationToken = default)
        {
            _dados.Remove(email);
            return Task.CompletedTask;
        }
    }

    private sealed class TokenServiceFake : ITokenService
    {
        public string GerarAccessToken(Usuario usuario, DateTime expiracaoUtc) => "jwt-token-valido";
    }
}
