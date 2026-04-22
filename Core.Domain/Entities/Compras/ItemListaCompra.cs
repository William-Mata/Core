using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class ItemListaCompra
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ListaCompraId { get; set; }
    public long? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoNormalizada { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public UnidadeMedidaCompra Unidade { get; set; } = UnidadeMedidaCompra.Unidade;
    public decimal Quantidade { get; set; } = 1m;
    public decimal? PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public string? EtiquetaCor { get; set; }
    public bool Comprado { get; set; }
    public DateTime? DataHoraCompra { get; set; }
    public ListaCompra? ListaCompra { get; set; }
    public Produto? Produto { get; set; }
}


