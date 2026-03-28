namespace Core.Domain.Entities;

public sealed class TentativaLoginInvalida
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Email { get; set; } = string.Empty;
    public int TentativasInvalidas { get; set; }
    public DateTime AtualizadoEmUtc { get; set; } = DateTime.UtcNow;
}