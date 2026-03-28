using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public sealed class AutenticacaoController(AutenticacaoService service) : ControllerBase
{
    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar([FromBody] EntrarRequest request, CancellationToken cancellationToken)
        => Ok(await service.EntrarAsync(request, cancellationToken));

    [HttpPost("criar-primeira-senha")]
    public async Task<IActionResult> CriarPrimeiraSenha([FromBody] CriarPrimeiraSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.CriarPrimeiraSenhaAsync(request, cancellationToken) });

    [HttpPost("renovar-token")]
    public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenRequest request, CancellationToken cancellationToken)
        => Ok(await service.RenovarTokenAsync(request, cancellationToken));

    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.EsqueciSenhaAsync(request, cancellationToken) });
}
