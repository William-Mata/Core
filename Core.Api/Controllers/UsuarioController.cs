using Core.Application.DTOs.Administracao;
using Core.Application.Services.Administracao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers;

/// <summary>
/// Endpoints de gerenciamento de usuarios.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class UsuarioController(UsuarioService service) : ControllerBase
{
    /// <summary>
    /// Altera a senha do usuario autenticado.
    /// </summary>
    [HttpPost("alterar-senha")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.AlterarSenhaAsync(request, cancellationToken) });

    /// <summary>
    /// Lista usuarios com filtros opcionais.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar([FromQuery] string? id, [FromQuery] string? descricao, [FromQuery] DateOnly? dataInicio, [FromQuery] DateOnly? dataFim, CancellationToken cancellationToken)
        => Ok(await service.ListarAsync(new ListarUsuariosRequest(id, descricao, dataInicio, dataFim), cancellationToken));

    /// <summary>
    /// Obtem um usuario por identificador.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await service.ObterAsync(id, cancellationToken));

    /// <summary>
    /// Cria um novo usuario.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] SalvarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await service.CriarAsync(request, cancellationToken));

    /// <summary>
    /// Atualiza os dados de um usuario existente.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] SalvarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await service.AtualizarAsync(id, request, cancellationToken));

    /// <summary>
    /// Exclui um usuario por identificador.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await service.ExcluirAsync(id, cancellationToken));
}
