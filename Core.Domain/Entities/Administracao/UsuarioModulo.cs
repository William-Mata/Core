namespace Core.Domain.Entities;

public sealed class UsuarioModulo
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioId { get; set; }
    public int ModuloId { get; set; }
    public bool Status { get; set; } = false;
    public Modulo? Modulo { get; set; }
}
