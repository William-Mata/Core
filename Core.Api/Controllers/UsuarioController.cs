using Core.Application.DTOs.Administracao;
using Core.Application.Services.Administracao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuarioController(UsuarioService service) : ControllerBase
{
    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request, CancellationToken cancellationToken)
        => Ok(new { mensagem = await service.AlterarSenhaAsync(request, cancellationToken) });

    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? id, [FromQuery] string? descricao, [FromQuery] DateOnly? dataInicio, [FromQuery] DateOnly? dataFim, CancellationToken cancellationToken)
        => Ok(await service.ListarAsync(new ListarUsuariosRequest(id, descricao, dataInicio, dataFim), cancellationToken));

    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await service.ObterAsync(id, cancellationToken));

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await service.CriarAsync(request, cancellationToken));

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar([FromRoute] int id, [FromBody] SalvarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await service.AtualizarAsync(id, request, cancellationToken));

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await service.ExcluirAsync(id, cancellationToken));
}
