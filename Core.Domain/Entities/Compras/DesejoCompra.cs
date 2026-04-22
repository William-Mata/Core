using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class DesejoCompra
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoNormalizada { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public UnidadeMedidaCompra Unidade { get; set; } = UnidadeMedidaCompra.Unidade;
    public decimal Quantidade { get; set; } = 1m;
    public decimal? PrecoEstimado { get; set; }
    public bool Convertido { get; set; }
    public DateTime? DataHoraConversao { get; set; }
    public Produto? Produto { get; set; }
}


