namespace Core.Application.DTOs.Financeiro;

public sealed record EnviarConviteAmizadeRequest(int UsuarioDestinoId);

public sealed record ConviteAmizadeDto(
    long Id,
    int UsuarioOrigemId,
    string UsuarioOrigemNome,
    int UsuarioDestinoId,
    string UsuarioDestinoNome,
    string Status,
    string Direcao,
    DateTime DataHoraCadastro,
    DateTime? DataHoraResposta);
