namespace Core.Domain.Entities;

public sealed class UsuarioFuncionalidade
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioId { get; set; }
    public int FuncionalidadeId { get; set; }
    public bool Status { get; set; } = false;
    public Funcionalidade? Funcionalidade { get; set; }
}
