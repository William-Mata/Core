namespace Core.Application.DTOs.Financeiro;

public sealed record EnviarConviteAmizadeRequest(string Email, string? Mensagem);

public sealed record ConviteAmizadeDto(
    long Id,
    string UsuarioOrigemNome,
    string UsuarioOrigemEmail,
    string Status,
    string? Mensagem,
    DateTime DataHoraCadastro);
