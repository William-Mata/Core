namespace Core.Domain.Entities;

public sealed class Tela
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int ModuloId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Status { get; set; } = false;
    public Modulo? Modulo { get; set; }
}
