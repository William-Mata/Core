namespace Core.Domain.Entities.Financeiro;

public sealed class Amizade
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioAId { get; set; }
    public int UsuarioBId { get; set; }
}
