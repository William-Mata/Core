namespace Core.Domain.Entities.Financeiro;

public sealed class ReembolsoDespesa
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ReembolsoId { get; set; }
    public long DespesaId { get; set; }
    public Despesa? Despesa { get; set; }
}
