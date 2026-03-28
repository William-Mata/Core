namespace Core.Application.DTOs;

public sealed record EntrarRequest(string Email, string Senha);
public sealed record EsqueciSenhaRequest(string Email);
public sealed record RenovarTokenRequest(string RefreshToken);
public sealed record CriarPrimeiraSenhaRequest(string Email, string Senha, string ConfirmarSenha);

public sealed record FuncionalidadeUsuarioResponse(int Id, string Nome, int Status);
public sealed record TelaUsuarioResponse(int Id, string Nome, int Status, IReadOnlyCollection<FuncionalidadeUsuarioResponse> Funcionalidades);
public sealed record ModuloUsuarioResponse(int Id, string Nome, int Status, IReadOnlyCollection<TelaUsuarioResponse> Telas);
public sealed record PerfilUsuarioResponse(int Id, string Nome);
public sealed record UsuarioAutenticadoResponse(int Id, string Nome, string Email, bool Status, PerfilUsuarioResponse Perfil, IReadOnlyCollection<ModuloUsuarioResponse> ModulosAtivos);
public sealed record AutenticacaoSuccessResponse(string AccessToken, string RefreshToken, DateTime Expiracao, UsuarioAutenticadoResponse Usuario);
