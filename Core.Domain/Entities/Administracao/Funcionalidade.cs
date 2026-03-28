namespace Core.Domain.Entities.Administracao;

public sealed class Funcionalidade
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int TelaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Status { get; set; } = false;
    public Tela? Tela { get; set; }
}
