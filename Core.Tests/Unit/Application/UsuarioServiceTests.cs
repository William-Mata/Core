using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;

namespace Core.Tests.Unit.Application;

public sealed class UsuarioServiceTests
{
    [Fact]
    public async Task DeveImpedirCriacao_QuandoUsuarioNaoEstiverAutenticado()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "novo@empresa.com", "USER")));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveCriarUsuario_ComPrimeiroAcesso()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var response = await service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "novo@empresa.com", "USER"));

        Assert.True(response.Sucesso);
        Assert.Equal("Usuario criado com sucesso", response.Mensagem);
        Assert.Equal("USER", response.Dados.Perfil);
        Assert.True(repository.UsuarioCriado?.PrimeiroAcesso);
    }

    [Fact]
    public async Task DeveCriarUsuario_ComStatusEPermissoesInformadas()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var request = new SalvarUsuarioRequest(
            "Novo Usuario",
            "novo@empresa.com",
            "USER",
            true,
            [
                new SalvarModuloUsuarioRequest(
                    "1",
                    "Geral",
                    true,
                    [
                        new SalvarTelaUsuarioRequest(
                            "2",
                            "Painel do Usuario",
                            true,
                            [
                                new SalvarFuncionalidadeUsuarioRequest("1", "Visualizar", true)
                            ])
                    ]),
                new SalvarModuloUsuarioRequest(
                    "3",
                    "Financeiro",
                    true,
                    [
                        new SalvarTelaUsuarioRequest(
                            "100",
                            "Despesas",
                            true,
                            [
                                new SalvarFuncionalidadeUsuarioRequest("4", "Excluir", true)
                            ])
                    ])
            ]);

        var response = await service.CriarAsync(request);

        Assert.True(response.Sucesso);
        Assert.True(repository.UsuarioCriado?.Ativo);
        Assert.Equal([1, 3], repository.ModulosAtivosIds);
        Assert.Equal([2, 100], repository.TelasAtivasIds);
        Assert.Equal([1, 4], repository.FuncionalidadesAtivasIds);
    }

    [Fact]
    public async Task DeveCriarUsuario_ResolvendoFuncionalidadesPelaTelaQuandoIdsDoPayloadForemRepetidos()
    {
        var repository = new UsuarioRepositoryFake
        {
            TelasAtivas =
            [
                new Tela { Id = 3, ModuloId = 1, Nome = "Lista de Amigos", Status = true },
                new Tela { Id = 4, ModuloId = 1, Nome = "Convites", Status = true }
            ],
            FuncionalidadesAtivas =
            [
                new Funcionalidade { Id = 10, TelaId = 3, Nome = "Visualizar", Status = true },
                new Funcionalidade { Id = 11, TelaId = 3, Nome = "Criar", Status = true },
                new Funcionalidade { Id = 12, TelaId = 3, Nome = "Editar", Status = true },
                new Funcionalidade { Id = 13, TelaId = 3, Nome = "Excluir", Status = true },
                new Funcionalidade { Id = 20, TelaId = 4, Nome = "Visualizar", Status = true },
                new Funcionalidade { Id = 21, TelaId = 4, Nome = "Criar", Status = true },
                new Funcionalidade { Id = 22, TelaId = 4, Nome = "Editar", Status = true },
                new Funcionalidade { Id = 23, TelaId = 4, Nome = "Excluir", Status = true }
            ]
        };

        var request = new SalvarUsuarioRequest(
            "Novo Usuario",
            "novo@empresa.com",
            "USER",
            true,
            [
                new SalvarModuloUsuarioRequest(
                    "1",
                    "Geral",
                    true,
                    [
                        new SalvarTelaUsuarioRequest(
                            "3",
                            "Lista de Amigos",
                            true,
                            [
                                new SalvarFuncionalidadeUsuarioRequest("1", "Visualizar", true),
                                new SalvarFuncionalidadeUsuarioRequest("2", "comum.acoes.criar", true),
                                new SalvarFuncionalidadeUsuarioRequest("3", "Editar", true),
                                new SalvarFuncionalidadeUsuarioRequest("4", "comum.acoes.excluir", true)
                            ]),
                        new SalvarTelaUsuarioRequest(
                            "4",
                            "Convites",
                            true,
                            [
                                new SalvarFuncionalidadeUsuarioRequest("1", "Visualizar", true),
                                new SalvarFuncionalidadeUsuarioRequest("2", "comum.acoes.criar", true),
                                new SalvarFuncionalidadeUsuarioRequest("3", "Editar", true),
                                new SalvarFuncionalidadeUsuarioRequest("4", "comum.acoes.excluir", true)
                            ])
                    ])
            ]);

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var response = await service.CriarAsync(request);

        Assert.True(response.Sucesso);
        Assert.Equal([3, 4], repository.TelasAtivasIds);
        Assert.Equal([10, 11, 12, 13, 20, 21, 22, 23], repository.FuncionalidadesAtivasIds);
    }

    [Fact]
    public async Task DeveImpedirCriacao_ComEmailDuplicado()
    {
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorEmail = new Usuario { Id = 20, Nome = "Existente", Email = "existente@empresa.com", PerfilId = 1, Ativo = true }
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "existente@empresa.com", "USER")));

        Assert.Equal("email_em_uso", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirCriacao_ComEmailDuplicadoMesmoQuandoUsuarioExistenteEstiverInativo()
    {
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorEmail = new Usuario { Id = 20, Nome = "Existente", Email = "existente@empresa.com", PerfilId = 1, Ativo = false }
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "existente@empresa.com", "USER")));

        Assert.Equal("email_em_uso", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirCriacao_ComNomeVazio()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("", "novo@empresa.com", "USER")));

        Assert.Equal("nome_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirCriacao_ComEmailInvalido()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "email-invalido", "USER")));

        Assert.Equal("email_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirCriacao_ComPerfilInvalido()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(new SalvarUsuarioRequest("Novo Usuario", "novo@empresa.com", "GESTOR")));

        Assert.Equal("perfil_invalido", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoUsuarioNaoForEncontradoNaAtualizacao()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AtualizarAsync(99, new SalvarUsuarioRequest("Nome", "usuario@empresa.com", "USER")));

        Assert.Equal("usuario_nao_encontrado", ex.Message);
    }

    [Fact]
    public async Task DeveObterUsuarioComArvoreCompletaDePermissoes()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Usuario",
            Email = "admin@core.com",
            PerfilId = 1,
            Ativo = true,
            Modulos =
            [
                new UsuarioModulo { UsuarioId = 1, ModuloId = 1, Status = true },
                new UsuarioModulo { UsuarioId = 1, ModuloId = 2, Status = false }
            ],
            Telas =
            [
                new UsuarioTela { UsuarioId = 1, TelaId = 1, Status = true },
                new UsuarioTela { UsuarioId = 1, TelaId = 30, Status = false }
            ],
            Funcionalidades =
            [
                new UsuarioFuncionalidade { UsuarioId = 1, FuncionalidadeId = 1, Status = true },
                new UsuarioFuncionalidade { UsuarioId = 1, FuncionalidadeId = 2, Status = false }
            ]
        };

        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario,
            ModulosAtivos = [
                new Modulo { Id = 1, Nome = "Geral", Status = true },
                new Modulo { Id = 2, Nome = "Administracao", Status = true }
            ],
            TelasAtivas = [
                new Tela { Id = 1, ModuloId = 1, Nome = "Dashboard", Status = true },
                new Tela { Id = 30, ModuloId = 2, Nome = "Administracao", Status = true }
            ],
            FuncionalidadesAtivas = [
                new Funcionalidade { Id = 1, TelaId = 1, Nome = "Visualizar", Status = true },
                new Funcionalidade { Id = 2, TelaId = 30, Nome = "Criar", Status = true }
            ]
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var response = await service.ObterAsync(1);
        var modulos = response.Dados.ModulosAtivos.ToArray();
        var telasModuloGeral = modulos[0].Telas.ToArray();
        var funcionalidadesTelaGeral = telasModuloGeral[0].Funcionalidades.ToArray();
        var telasModuloAdministracao = modulos[1].Telas.ToArray();
        var funcionalidadesTelaAdministracao = telasModuloAdministracao[0].Funcionalidades.ToArray();

        Assert.True(response.Sucesso);
        Assert.Equal("ADMIN", response.Dados.Perfil);
        Assert.True(response.Dados.Status);
        Assert.Equal(2, modulos.Length);
        Assert.Equal("1", modulos[0].Id);
        Assert.True(modulos[0].Status);
        Assert.Single(telasModuloGeral);
        Assert.True(telasModuloGeral[0].Status);
        Assert.Single(funcionalidadesTelaGeral);
        Assert.True(funcionalidadesTelaGeral[0].Status);
        Assert.False(modulos[1].Status);
        Assert.False(telasModuloAdministracao[0].Status);
        Assert.False(funcionalidadesTelaAdministracao[0].Status);
    }

    [Fact]
    public async Task DeveAtualizarStatusEPermissoesDoUsuario()
    {
        var usuario = new Usuario { Id = 2, Nome = "Teste", Email = "teste@empresa.com", PerfilId = 2, Ativo = true };
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario
        };

        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            false,
            [
                new SalvarModuloUsuarioRequest(
                    "1",
                    "Geral",
                    true,
                    [
                        new SalvarTelaUsuarioRequest(
                            "2",
                            "Painel do Usuario",
                            true,
                            [
                                new SalvarFuncionalidadeUsuarioRequest("1", "Visualizar", true)
                            ])
                    ])
            ]);

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var response = await service.AtualizarAsync(2, request);

        Assert.True(response.Sucesso);
        Assert.False(usuario.Ativo);
        Assert.Equal("William de Mata", usuario.Nome);
        Assert.Equal("william.xavante@gmail.com", usuario.Email);
        Assert.Equal(2, usuario.PerfilId);
        Assert.Equal([1], repository.ModulosAtivosIds);
        Assert.Equal([2], repository.TelasAtivasIds);
        Assert.Equal([1], repository.FuncionalidadesAtivasIds);
    }

    [Fact]
    public async Task DeveInativarUsuario_NaExclusao()
    {
        var usuario = new Usuario { Id = 30, Nome = "Teste", Email = "teste@empresa.com", PerfilId = 2, Ativo = true };
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));
        var response = await service.ExcluirAsync(30);

        Assert.True(response.Sucesso);
        Assert.False(usuario.Ativo);
        Assert.Equal("Usuario removido com sucesso", response.Mensagem);
    }

    [Fact]
    public async Task DeveImpedirExclusao_DoProprioUsuario()
    {
        var usuario = new Usuario { Id = 30, Nome = "Teste", Email = "teste@empresa.com", PerfilId = 2, Ativo = true };
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(30));
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.ExcluirAsync(30));

        Assert.Equal("usuario_admin_nao_pode_excluir_a_si_mesmo", ex.Message);
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoUsuarioNaoForEncontradoNaExclusao()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(1));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ExcluirAsync(55));

        Assert.Equal("usuario_nao_encontrado", ex.Message);
    }

    [Fact]
    public async Task DeveAlterarSenha_DoUsuarioAutenticado()
    {
        var usuario = new Usuario { Id = 12, Nome = "Usuario", Email = "usuario@core.com", PerfilId = 2, Ativo = true, SenhaHash = "hash" };
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario,
            SenhaValida = true
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));
        var mensagem = await service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "NovaSenha@123", "NovaSenha@123"));

        Assert.Equal("Senha alterada com sucesso.", mensagem);
        Assert.True(repository.AlterouSenha);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_QuandoUsuarioNaoEstiverAutenticado()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(null));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "NovaSenha@123", "NovaSenha@123")));

        Assert.Equal("usuario_nao_autenticado", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_ComSenhaAtualVazia()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("", "NovaSenha@123", "NovaSenha@123")));

        Assert.Equal("senha_atual_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_ComNovaSenhaVazia()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "", "")));

        Assert.Equal("nova_senha_obrigatoria", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_ComSenhaFraca()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "123", "123")));

        Assert.Equal("senha_fraca", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_ComConfirmacaoDiferente()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "NovaSenha@123", "OutraSenha@123")));

        Assert.Equal("confirmacao_senha_diferente", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_QuandoUsuarioNaoForEncontrado()
    {
        var repository = new UsuarioRepositoryFake();
        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "NovaSenha@123", "NovaSenha@123")));

        Assert.Equal("usuario_inativo_ou_nao_encontrado", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirAlteracaoDeSenha_QuandoSenhaAtualForInvalida()
    {
        var usuario = new Usuario { Id = 12, Nome = "Usuario", Email = "usuario@core.com", PerfilId = 2, Ativo = true, SenhaHash = "hash" };
        var repository = new UsuarioRepositoryFake
        {
            UsuarioPorId = usuario,
            SenhaValida = false
        };

        var service = new UsuarioService(repository, new UsuarioAutenticadoProviderFake(12));
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AlterarSenhaAsync(new AlterarSenhaRequest("Atual@12345", "NovaSenha@123", "NovaSenha@123")));

        Assert.Equal("senha_atual_incorreta", ex.Message);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Usuario? UsuarioPorId { get; set; }
        public Usuario? UsuarioPorEmail { get; set; }
        public Usuario? UsuarioCriado { get; private set; }
        public bool SenhaValida { get; set; }
        public bool AlterouSenha { get; private set; }
        public IReadOnlyCollection<Modulo> ModulosAtivos { get; set; } = [];
        public IReadOnlyCollection<Tela> TelasAtivas { get; set; } = [];
        public IReadOnlyCollection<Funcionalidade> FuncionalidadesAtivas { get; set; } = [];
        public IReadOnlyCollection<int> ModulosAtivosIds { get; private set; } = [];
        public IReadOnlyCollection<int> TelasAtivasIds { get; private set; } = [];
        public IReadOnlyCollection<int> FuncionalidadesAtivasIds { get; private set; } = [];

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(UsuarioPorId);

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(UsuarioPorEmail);

        public Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ModulosAtivos);

        public Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(TelasAtivas);

        public Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(FuncionalidadesAtivas);

        public Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default) =>
            Task.FromResult(SenhaValida);

        public Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            usuario.Id = 10;
            UsuarioCriado = usuario;
            return Task.FromResult(usuario);
        }

        public Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task SincronizarPermissoesAsync(int usuarioId, int usuarioCadastroId, IReadOnlyCollection<int> modulosAtivosIds, IReadOnlyCollection<int> telasAtivasIds, IReadOnlyCollection<int> funcionalidadesAtivasIds, CancellationToken cancellationToken = default)
        {
            ModulosAtivosIds = modulosAtivosIds.ToArray();
            TelasAtivasIds = telasAtivasIds.ToArray();
            FuncionalidadesAtivasIds = funcionalidadesAtivasIds.ToArray();
            return Task.CompletedTask;
        }

        public Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default)
        {
            AlterouSenha = true;
            return Task.CompletedTask;
        }
    }

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }
}
