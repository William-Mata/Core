namespace Core.Domain.Entities;

public sealed class Modulo
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Status { get; set; } = false;
}
