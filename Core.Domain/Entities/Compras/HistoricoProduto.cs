using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class HistoricoProduto
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ProdutoId { get; set; }
    public long? ItemListaCompraId { get; set; }
    public UnidadeMedidaCompra Unidade { get; set; } = UnidadeMedidaCompra.Unidade;
    public decimal PrecoUnitario { get; set; }
    public OrigemPrecoHistoricoCompra Origem { get; set; } = OrigemPrecoHistoricoCompra.Estimado;
    public Produto? Produto { get; set; }
    public ItemListaCompra? ItemListaCompra { get; set; }
}


