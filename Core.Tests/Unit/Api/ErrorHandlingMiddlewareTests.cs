using System.Text.Json;
using Core.Api.Middlewares;
using Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Core.Tests.Unit.Api;

public sealed class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task DeveRetornarProblemDetails_ParaDomainException()
    {
        var context = CriarHttpContext("/api/usuarios");
        var middleware = new ErrorHandlingMiddleware(_ => throw new DomainException("usuario_nao_autenticado"), Microsoft.Extensions.Logging.Abstractions.NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        var payload = await LerPayloadAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("https://httpstatuses.com/400", payload.GetProperty("type").GetString());
        Assert.Equal("Requisicao invalida", payload.GetProperty("title").GetString());
        Assert.Equal("Usuario nao autenticado.", payload.GetProperty("detail").GetString());
        Assert.Equal("/api/usuarios", payload.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task DeveRetornarProblemDetails_ParaNotFoundException()
    {
        var context = CriarHttpContext("/api/usuarios/99");
        var middleware = new ErrorHandlingMiddleware(_ => throw new NotFoundException("usuario_nao_encontrado"), Microsoft.Extensions.Logging.Abstractions.NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        var payload = await LerPayloadAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("https://httpstatuses.com/404", payload.GetProperty("type").GetString());
        Assert.Equal("Recurso nao encontrado", payload.GetProperty("title").GetString());
        Assert.Equal("Usuario nao encontrado.", payload.GetProperty("detail").GetString());
        Assert.Equal("/api/usuarios/99", payload.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task DeveRetornarProblemDetails_ParaErroInterno()
    {
        var context = CriarHttpContext("/api/financeiro/cartoes");
        var middleware = new ErrorHandlingMiddleware(_ => throw new InvalidOperationException("falha"), Microsoft.Extensions.Logging.Abstractions.NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        var payload = await LerPayloadAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("https://httpstatuses.com/500", payload.GetProperty("type").GetString());
        Assert.Equal("Erro interno do servidor", payload.GetProperty("title").GetString());
        Assert.Equal("Erro interno do servidor.", payload.GetProperty("detail").GetString());
        Assert.Equal("/api/financeiro/cartoes", payload.GetProperty("instance").GetString());
    }

    private static DefaultHttpContext CriarHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> LerPayloadAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }
}

