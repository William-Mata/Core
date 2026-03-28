using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Integration.Repositories;

public sealed class UsuarioRepositoryTests
{
    [Fact]
    public async Task DeveEncontrarUsuarioPorEmailMesmoQuandoEstiverInativo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);

        context.Usuarios.Add(new Usuario
        {
            Id = 10,
            UsuarioCadastroId = 1,
            Nome = "Usuario Inativo",
            Email = "inativo@empresa.com",
            SenhaHash = "hash",
            Ativo = false,
            PrimeiroAcesso = false,
            PerfilId = 2
        });

        await context.SaveChangesAsync();

        var repository = new UsuarioRepository(context);
        var usuario = await repository.ObterPorEmailAsync("inativo@empresa.com");

        Assert.NotNull(usuario);
        Assert.Equal(10, usuario.Id);
        Assert.False(usuario.Ativo);
    }

    [Fact]
    public async Task DeveCriarUsuarioESalvarPermissoesSemInserirEntidadesRelacionadasVazias()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);

        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            UsuarioCadastroId = 1,
            Nome = "Administrador",
            Email = "admin@core.com",
            SenhaHash = "hash",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 1
        });

        context.Modulos.Add(new Modulo
        {
            Id = 1,
            UsuarioCadastroId = 1,
            Nome = "Geral",
            Status = true
        });

        context.Telas.Add(new Tela
        {
            Id = 2,
            UsuarioCadastroId = 1,
            ModuloId = 1,
            Nome = "Painel do Usuario",
            Status = true
        });

        context.Funcionalidades.Add(new Funcionalidade
        {
            Id = 1,
            UsuarioCadastroId = 1,
            TelaId = 2,
            Nome = "Visualizar",
            Status = true
        });

        await context.SaveChangesAsync();

        var repository = new UsuarioRepository(context);

        await repository.CriarAsync(new Usuario
        {
            UsuarioCadastroId = 1,
            Nome = "Novo Usuario",
            Email = "novo@empresa.com",
            SenhaHash = string.Empty,
            Ativo = true,
            PrimeiroAcesso = true,
            PerfilId = 2
        });

        Assert.Equal(1, await context.Modulos.CountAsync());
        Assert.Equal(1, await context.Telas.CountAsync());
        Assert.Equal(1, await context.Funcionalidades.CountAsync());
        Assert.Equal(1, await context.UsuariosModulos.CountAsync(x => x.UsuarioId == 2 && x.ModuloId == 1 && x.Status));
        Assert.Equal(1, await context.UsuariosTelas.CountAsync(x => x.UsuarioId == 2 && x.TelaId == 2 && x.Status));
        Assert.Equal(1, await context.UsuariosFuncionalidades.CountAsync(x => x.UsuarioId == 2 && x.FuncionalidadeId == 1 && x.Status));
    }
}
