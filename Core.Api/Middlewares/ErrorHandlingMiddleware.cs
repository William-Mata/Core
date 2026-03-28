using Core.Api.Extensions;
using Core.Domain.Exceptions;

namespace Core.Api.Middlewares;

public sealed class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(ex.Message.ParaProblemDetails(StatusCodes.Status404NotFound, context));
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ex.Message.ParaProblemDetails(StatusCodes.Status400BadRequest, context));
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync("erro_interno".ParaProblemDetails(StatusCodes.Status500InternalServerError, context));
        }
    }
}
