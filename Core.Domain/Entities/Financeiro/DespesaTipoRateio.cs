namespace Core.Domain.Entities.Financeiro;

public sealed class DespesaTipoRateio
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long DespesaId { get; set; }
    public string TipoRateio { get; set; } = string.Empty;
}