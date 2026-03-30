namespace Core.Domain.Entities.Financeiro;

public sealed class ReceitaAmigoRateio
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ReceitaId { get; set; }
    public int? AmigoId { get; set; }
    public string AmigoNome { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
}
