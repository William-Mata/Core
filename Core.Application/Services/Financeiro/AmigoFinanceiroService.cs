using Core.Application.DTOs.Financeiro;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;

namespace Core.Application.Services.Financeiro;

public sealed class AmigoFinanceiroService(
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<AmigoListaDto>> ListarAmigosAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var usuarios = await usuarioRepository.ListarAtivosAsync(cancellationToken);

        return usuarios
            .Where(x => x.Id != usuarioId)
            .Select(x => new AmigoListaDto(x.Id, x.Nome, x.Email))
            .ToArray();
    }
}
