namespace Core.Domain.Entities.Financeiro;

public sealed class SubArea
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long AreaId { get; set; }
    public Area Area { get; set; } = default!;
    public string Nome { get; set; } = string.Empty;
}
