using Core.Domain.Common;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests.Integration.Repositories;

public sealed class AppDbContextTimezoneTests
{
    [Fact]
    public async Task DeveConverterDataHoraParaHorarioBrasil_QuandoCampoNaoEhUtc()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);

        var dataHoraUtc = new DateTime(2026, 4, 10, 18, 30, 0, DateTimeKind.Utc);
        context.Usuarios.Add(new Usuario
        {
            Id = 9001,
            UsuarioCadastroId = 1,
            Nome = "Usuario Fuso",
            Email = "usuario-fuso@core.com",
            SenhaHash = "hash",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 2,
            DataHoraCadastro = dataHoraUtc
        });

        await context.SaveChangesAsync();

        var usuarioSalvo = await context.Usuarios.AsNoTracking().FirstAsync(x => x.Id == 9001);
        Assert.Equal(DataHoraBrasil.Converter(dataHoraUtc), usuarioSalvo.DataHoraCadastro);
    }

    [Fact]
    public async Task NaoDeveConverterCampoComSufixoUtc_QuandoPersistir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new AppDbContext(options);

        context.Usuarios.Add(new Usuario
        {
            Id = 9002,
            UsuarioCadastroId = 1,
            Nome = "Usuario Token",
            Email = "usuario-token@core.com",
            SenhaHash = "hash",
            Ativo = true,
            PrimeiroAcesso = false,
            PerfilId = 2
        });
        await context.SaveChangesAsync();

        var baseUtc = new DateTime(2026, 4, 10, 18, 30, 0, DateTimeKind.Utc);
        var refreshToken = new RefreshToken
        {
            UsuarioId = 9002,
            UsuarioCadastroId = 9002,
            Token = $"refresh-{Guid.NewGuid():N}",
            DataHoraCadastro = baseUtc,
            ExpiraEmUtc = baseUtc.AddDays(7),
            RevogadoEmUtc = baseUtc.AddHours(1)
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var refreshTokenSalvo = await context.RefreshTokens.AsNoTracking()
            .FirstAsync(x => x.Token == refreshToken.Token);

        Assert.Equal(DataHoraBrasil.Converter(baseUtc), refreshTokenSalvo.DataHoraCadastro);
        Assert.Equal(baseUtc.AddDays(7), refreshTokenSalvo.ExpiraEmUtc);
        Assert.Equal(baseUtc.AddHours(1), refreshTokenSalvo.RevogadoEmUtc);
    }
}
