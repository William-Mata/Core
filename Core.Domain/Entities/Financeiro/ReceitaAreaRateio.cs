namespace Core.Domain.Entities.Financeiro;

public sealed class ReceitaAreaRateio
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ReceitaId { get; set; }
    public long AreaId { get; set; }
    public Area Area { get; set; } = default!;
    public long SubAreaId { get; set; }
    public SubArea SubArea { get; set; } = default!;
    public decimal? Valor { get; set; }
}