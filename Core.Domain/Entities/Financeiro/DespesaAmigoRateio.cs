namespace Core.Domain.Entities.Financeiro;

public sealed class DespesaAmigoRateio
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long DespesaId { get; set; }
    public string AmigoNome { get; set; } = string.Empty;
}