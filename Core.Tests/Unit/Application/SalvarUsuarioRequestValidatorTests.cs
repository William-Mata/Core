using Core.Application.DTOs.Administracao;
using Core.Application.Validators.Administracao;
using Core.Domain.Common;

namespace Core.Tests.Unit.Application;

public sealed class SalvarUsuarioRequestValidatorTests
{
    private readonly SalvarUsuarioRequestValidator _validator = new();

    [Fact]
    public void DeveValidarComSucesso_QuandoPayloadForValido()
    {
        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            new DateOnly(2000, 1, 1),
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
                    ])
            ]);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DeveInvalidar_QuandoModuloNaoTiverId()
    {
        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            null,
            true,
            [
                new SalvarModuloUsuarioRequest(
                    "",
                    "Geral",
                    true,
                    [])
            ]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "Os modulos informados sao invalidos.");
    }

    [Fact]
    public void DeveInvalidar_QuandoTelaNaoTiverId()
    {
        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            null,
            true,
            [
                new SalvarModuloUsuarioRequest(
                    "1",
                    "Geral",
                    true,
                    [
                        new SalvarTelaUsuarioRequest(
                            "",
                            "Painel do Usuario",
                            true,
                            [])
                    ])
            ]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "Os modulos informados sao invalidos.");
    }

    [Fact]
    public void DeveInvalidar_QuandoFuncionalidadeNaoTiverId()
    {
        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            null,
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
                                new SalvarFuncionalidadeUsuarioRequest("", "Visualizar", true)
                            ])
                    ])
            ]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "Os modulos informados sao invalidos.");
    }

    [Fact]
    public void DeveInvalidar_QuandoDataNascimentoForNoFuturo()
    {
        var amanhaUtc = DataHoraBrasil.Hoje().AddDays(1);
        var request = new SalvarUsuarioRequest(
            "William de Mata",
            "william.xavante@gmail.com",
            "USER",
            amanhaUtc);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "A data de nascimento informada e invalida.");
    }
}

