using System.Security.Claims;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Core.Api.Security;

public sealed class UsuarioAutenticadoProvider(IHttpContextAccessor httpContextAccessor) : IUsuarioAutenticadoProvider
{
    public int? ObterUsuarioId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return null;

        var claimId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("usuario_id");

        if (int.TryParse(claimId, out var usuarioId)) return usuarioId;

        return null;
    }
}
