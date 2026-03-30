using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class AmigoFinanceiroService(
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<AmigoListaDto>> ListarAmigosAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var usuarios = await amizadeRepository.ListarAmigosAceitosAsync(usuarioId, cancellationToken);

        return usuarios
            .Select(x => new AmigoListaDto(x.Id, x.Nome, x.Email))
            .ToArray();
    }

    public async Task<ConviteAmizadeDto> EnviarConviteAsync(EnviarConviteAmizadeRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var usuarioDestinoId = request.UsuarioDestinoId;

        if (usuarioDestinoId <= 0 || usuarioDestinoId == usuarioId)
        {
            throw new DomainException("usuario_destino_invalido");
        }

        var usuarioOrigem = await usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken) ?? throw new DomainException("usuario_nao_encontrado");
        var usuarioDestino = await usuarioRepository.ObterPorIdAsync(usuarioDestinoId, cancellationToken);
        if (usuarioDestino is null || !usuarioDestino.Ativo)
        {
            throw new DomainException("usuario_destino_invalido");
        }

        if (await amizadeRepository.ExisteAmizadeAsync(usuarioId, usuarioDestinoId, cancellationToken))
        {
            throw new DomainException("amizade_ja_existente");
        }

        if (await amizadeRepository.ObterConvitePendenteAsync(usuarioId, usuarioDestinoId, cancellationToken) is not null)
        {
            throw new DomainException("convite_ja_enviado");
        }

        if (await amizadeRepository.ObterConvitePendenteAsync(usuarioDestinoId, usuarioId, cancellationToken) is not null)
        {
            throw new DomainException("convite_pendente_existente");
        }

        var convite = new ConviteAmizade
        {
            UsuarioCadastroId = usuarioId,
            UsuarioOrigemId = usuarioId,
            UsuarioDestinoId = usuarioDestinoId,
            Status = StatusConviteAmizade.Pendente
        };

        var criado = await amizadeRepository.CriarConviteAsync(convite, cancellationToken);

        return new ConviteAmizadeDto(
            criado.Id,
            criado.UsuarioOrigemId,
            usuarioOrigem.Nome,
            criado.UsuarioDestinoId,
            usuarioDestino.Nome,
            criado.Status.ToString().ToLowerInvariant(),
            "enviado",
            criado.DataHoraCadastro,
            criado.DataHoraResposta);
    }

    public async Task<IReadOnlyCollection<ConviteAmizadeDto>> ListarConvitesAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var convites = await amizadeRepository.ListarConvitesPendentesAsync(usuarioId, cancellationToken);

        if (convites.Count == 0)
        {
            return [];
        }

        var usuarios = await usuarioRepository.ListarAtivosAsync(cancellationToken);
        var usuariosPorId = usuarios.ToDictionary(x => x.Id);

        return convites
            .Select(convite =>
            {
                var origemNome = usuariosPorId.TryGetValue(convite.UsuarioOrigemId, out var origem) ? origem.Nome : string.Empty;
                var destinoNome = usuariosPorId.TryGetValue(convite.UsuarioDestinoId, out var destino) ? destino.Nome : string.Empty;
                var direcao = convite.UsuarioOrigemId == usuarioId ? "enviado" : "recebido";

                return new ConviteAmizadeDto(
                    convite.Id,
                    convite.UsuarioOrigemId,
                    origemNome,
                    convite.UsuarioDestinoId,
                    destinoNome,
                    convite.Status.ToString().ToLowerInvariant(),
                    direcao,
                    convite.DataHoraCadastro,
                    convite.DataHoraResposta);
            })
            .ToArray();
    }

    public Task<ConviteAmizadeDto> AceitarConviteAsync(long conviteId, CancellationToken cancellationToken = default) =>
        ResponderConviteAsync(conviteId, StatusConviteAmizade.Aceito, cancellationToken);

    public Task<ConviteAmizadeDto> RejeitarConviteAsync(long conviteId, CancellationToken cancellationToken = default) =>
        ResponderConviteAsync(conviteId, StatusConviteAmizade.Rejeitado, cancellationToken);

    public async Task RemoverAmizadeAsync(int amigoId, CancellationToken cancellationToken = default)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        if (amigoId <= 0 || amigoId == usuarioId)
        {
            throw new DomainException("amizade_nao_encontrada");
        }

        var amizade = await amizadeRepository.ObterAmizadeAsync(usuarioId, amigoId, cancellationToken) ?? throw new NotFoundException("amizade_nao_encontrada");
        await amizadeRepository.ExcluirAmizadeAsync(amizade, cancellationToken);
    }

    private async Task<ConviteAmizadeDto> ResponderConviteAsync(long conviteId, StatusConviteAmizade status, CancellationToken cancellationToken)
    {
        var usuarioId = usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
        var convite = await amizadeRepository.ObterConvitePorIdAsync(conviteId, cancellationToken) ?? throw new NotFoundException("convite_nao_encontrado");

        if (convite.UsuarioDestinoId != usuarioId)
        {
            throw new DomainException("convite_nao_permitido");
        }

        if (convite.Status != StatusConviteAmizade.Pendente)
        {
            throw new DomainException("status_convite_invalido");
        }

        convite.Status = status;
        convite.UsuarioCadastroId = usuarioId;
        convite.DataHoraResposta = DateTime.UtcNow;
        await amizadeRepository.AtualizarConviteAsync(convite, cancellationToken);

        if (status == StatusConviteAmizade.Aceito && !await amizadeRepository.ExisteAmizadeAsync(convite.UsuarioOrigemId, convite.UsuarioDestinoId, cancellationToken))
        {
            var (usuarioAId, usuarioBId) = OrdenarPar(convite.UsuarioOrigemId, convite.UsuarioDestinoId);
            await amizadeRepository.CriarAmizadeAsync(new Amizade
            {
                UsuarioCadastroId = usuarioId,
                UsuarioAId = usuarioAId,
                UsuarioBId = usuarioBId
            }, cancellationToken);
        }

        var usuarioOrigem = await usuarioRepository.ObterPorIdAsync(convite.UsuarioOrigemId, cancellationToken) ?? throw new DomainException("usuario_nao_encontrado");
        var usuarioDestino = await usuarioRepository.ObterPorIdAsync(convite.UsuarioDestinoId, cancellationToken) ?? throw new DomainException("usuario_nao_encontrado");

        return new ConviteAmizadeDto(
            convite.Id,
            convite.UsuarioOrigemId,
            usuarioOrigem.Nome,
            convite.UsuarioDestinoId,
            usuarioDestino.Nome,
            convite.Status.ToString().ToLowerInvariant(),
            "recebido",
            convite.DataHoraCadastro,
            convite.DataHoraResposta);
    }

    private static (int UsuarioAId, int UsuarioBId) OrdenarPar(int usuarioId, int amigoId) =>
        usuarioId < amigoId ? (usuarioId, amigoId) : (amigoId, usuarioId);
}
