using Core.Api.Extensions;
using Core.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Api.Middlewares;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Requisicao cancelada pelo cliente. Path: {Path}", context.Request.Path);
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogWarning(ex, "Requisicao HTTP invalida. Path: {Path}", context.Request.Path);
            await EscreverProblemDetailsAsync(context, "dados_invalidos", StatusCodes.Status400BadRequest);
        }
        catch (ValidationException ex)
        {
            var detalhes = ex.Errors
                .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Dados da requisicao invalidos." : x.ErrorMessage)
                .Distinct()
                .ToArray();

            logger.LogWarning(ex, "Falha de validacao na requisicao. Path: {Path}", context.Request.Path);
            await EscreverProblemDetailsAsync(context, "dados_invalidos", StatusCodes.Status400BadRequest, detalhes);
        }
        catch (NotFoundException ex)
        {
            logger.LogInformation(ex, "Recurso nao encontrado. Path: {Path} Codigo: {Codigo}", context.Request.Path, ex.Message);
            await EscreverProblemDetailsAsync(context, ex.Message, StatusCodes.Status404NotFound);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Erro de dominio. Path: {Path} Codigo: {Codigo}", context.Request.Path, ex.Message);
            await EscreverProblemDetailsAsync(context, ex.Message, StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro nao tratado. Path: {Path}", context.Request.Path);
            await EscreverProblemDetailsAsync(context, "erro_interno", StatusCodes.Status500InternalServerError);
        }
    }

    private static Task EscreverProblemDetailsAsync(
        HttpContext context,
        string codigo,
        int statusCode,
        IReadOnlyCollection<string>? detalhes = null)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = codigo.ParaProblemDetails(statusCode, context, detalhes);
        return context.Response.WriteAsJsonAsync(payload);
    }
}
