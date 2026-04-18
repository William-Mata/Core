using Core.Application.DTOs.Financeiro;
using Core.Domain.Common;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class AmigoService(
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
        var usuarioLogado = await usuarioRepository.ObterPorIdAsync(usuarioId);
        var usuarioDestinoEmail = request.Email;

        if (string.IsNullOrEmpty(usuarioDestinoEmail) || usuarioLogado?.Email == usuarioDestinoEmail)
        {
            throw new DomainException("usuario_destino_invalido");
        }

        var usuarioOrigem = await usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken) ?? throw new DomainException("usuario_nao_encontrado");
        var usuarioDestino = await usuarioRepository.ObterPorEmailAsync(usuarioDestinoEmail, cancellationToken);
        if (usuarioDestino is null || !usuarioDestino.Ativo)
        {
            throw new DomainException("usuario_destino_invalido");
        }

        if (await amizadeRepository.ExisteAmizadeAsync(usuarioId, usuarioDestino.Id, cancellationToken))
        {
            throw new DomainException("amizade_ja_existente");
        }

        if (await amizadeRepository.ObterConvitePendenteAsync(usuarioId, usuarioDestino.Id, cancellationToken) is not null)
        {
            throw new DomainException("convite_ja_enviado");
        }

        if (await amizadeRepository.ObterConvitePendenteAsync(usuarioDestino.Id, usuarioId, cancellationToken) is not null)
        {
            throw new DomainException("convite_pendente_existente");
        }

        var convite = new ConviteAmizade
        {
            UsuarioCadastroId = usuarioId,
            UsuarioOrigemId = usuarioId,
            UsuarioDestinoId = usuarioDestino.Id,
            Mensagem = request.Mensagem,
            Status = StatusConviteAmizade.Pendente
        };

        var criado = await amizadeRepository.CriarConviteAsync(convite, cancellationToken);

        return new ConviteAmizadeDto(
            criado.Id,
            usuarioOrigem.Nome,
            usuarioOrigem.Email,
            criado.Status.ToString().ToLowerInvariant(),
            request.Mensagem,
            criado.DataHoraCadastro);
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
                var origem = usuariosPorId.TryGetValue(convite.UsuarioOrigemId, out var usuarioOrigem)
                    ? usuarioOrigem
                    : null;

                return new ConviteAmizadeDto(
                    convite.Id,
                    origem?.Nome ?? string.Empty,
                    origem?.Email ?? string.Empty,
                    convite.Status.ToString().ToLowerInvariant(),
                    convite.Mensagem,
                    convite.DataHoraCadastro);
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
        convite.DataHoraResposta = DataHoraBrasil.Agora();
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
        return new ConviteAmizadeDto(
            convite.Id,
            usuarioOrigem.Nome,
            usuarioOrigem.Email,
            convite.Status.ToString().ToLowerInvariant(),
            convite.Mensagem,
            convite.DataHoraCadastro);
    }

    private static (int UsuarioAId, int UsuarioBId) OrdenarPar(int usuarioId, int amigoId) =>
        usuarioId < amigoId ? (usuarioId, amigoId) : (amigoId, usuarioId);
}
