namespace Core.Application.DTOs;

public sealed record ListarUsuariosRequest(string? Id, string? Descricao, DateOnly? DataInicio, DateOnly? DataFim);
public sealed record SalvarFuncionalidadeUsuarioRequest(string Id, string Nome, bool Status);
public sealed record SalvarTelaUsuarioRequest(string Id, string Nome, bool Status, IReadOnlyCollection<SalvarFuncionalidadeUsuarioRequest>? Funcionalidades);
public sealed record SalvarModuloUsuarioRequest(string Id, string Nome, bool Status, IReadOnlyCollection<SalvarTelaUsuarioRequest>? Telas);
public sealed record SalvarUsuarioRequest(string Nome, string Email, string Perfil, bool? Status = null, IReadOnlyCollection<SalvarModuloUsuarioRequest>? ModulosAtivos = null);
public sealed record AlterarSenhaRequest(string SenhaAtual, string NovaSenha, string ConfirmarSenha);
public sealed record UsuarioDto(int Id, string Nome, string Email, string Perfil, DateTime DataCriacao);
public sealed record FuncionalidadeUsuarioDto(string Id, string Nome, bool Status);
public sealed record TelaUsuarioDto(string Id, string Nome, bool Status, IReadOnlyCollection<FuncionalidadeUsuarioDto> Funcionalidades);
public sealed record ModuloUsuarioDto(string Id, string Nome, bool Status, IReadOnlyCollection<TelaUsuarioDto> Telas);
public sealed record UsuarioDetalheDto(int Id, string Nome, string Email, string Perfil, bool Status, DateTime DataCriacao, IReadOnlyCollection<ModuloUsuarioDto> ModulosAtivos);
public sealed record ListarUsuariosResponse(bool Sucesso, IReadOnlyCollection<UsuarioDto> Dados, int Quantidade);
public sealed record ObterUsuarioResponse(bool Sucesso, UsuarioDetalheDto Dados);
public sealed record ResultadoUsuarioResponse(bool Sucesso, string Mensagem);
public sealed record CriarUsuarioResponse(bool Sucesso, string Mensagem, UsuarioDto Dados);
