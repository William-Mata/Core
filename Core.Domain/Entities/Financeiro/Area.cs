namespace Core.Domain.Entities.Financeiro;

using Core.Domain.Enums.Financeiro;

public sealed class Area
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoAreaFinanceira Tipo { get; set; }
    public List<SubArea> SubAreas { get; set; } = [];
}
