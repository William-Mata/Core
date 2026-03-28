namespace Core.Domain.Entities.Administracao;

public sealed class Usuario
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public bool PrimeiroAcesso { get; set; } = true;
    public int PerfilId { get; set; } = 1;
    public List<UsuarioModulo> Modulos { get; set; } = [];
    public List<UsuarioTela> Telas { get; set; } = [];
    public List<UsuarioFuncionalidade> Funcionalidades { get; set; } = [];
}
