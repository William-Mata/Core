namespace Core.Domain.Entities;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? RevogadoEmUtc { get; set; }
}
