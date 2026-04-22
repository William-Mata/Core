using Core.Application.DTOs.Administracao;
using Core.Application.Services.Administracao;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers;

/// <summary>
/// Endpoints de autenticacao e gerenciamento de token.
/// </summary>
[ApiController]
[Route("api/autenticacao")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public sealed class AutenticacaoController(AutenticacaoService service) : ControllerBase
{
    /// <summary>
    /// Realiza o login e retorna token de acesso e refresh token.
    /// </summary>
    [HttpPost("entrar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Entrar([FromBody] EntrarRequest request, CancellationToken cancellationToken)
        => Ok(await service.EntrarAsync(request, cancellationToken));

    /// <summary>
    /// Define a primeira senha de um usuario previamente cadastrado.
    /// </summary>
    [HttpPost("criar-primeira-senha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CriarPrimeiraSenha([FromBody] CriarPrimeiraSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.CriarPrimeiraSenhaAsync(request, cancellationToken) });

    /// <summary>
    /// Renova o token de acesso utilizando refresh token valido.
    /// </summary>
    [HttpPost("renovar-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RenovarToken([FromBody] RenovarTokenRequest request, CancellationToken cancellationToken)
        => Ok(await service.RenovarTokenAsync(request, cancellationToken));

    /// <summary>
    /// Dispara o fluxo de recuperacao de senha.
    /// </summary>
    [HttpPost("esqueci-senha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.EsqueciSenhaAsync(request, cancellationToken) });
}
