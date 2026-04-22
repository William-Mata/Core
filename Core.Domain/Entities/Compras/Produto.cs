using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class Produto
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoNormalizada { get; set; } = string.Empty;
    public UnidadeMedidaCompra UnidadePadrao { get; set; } = UnidadeMedidaCompra.Unidade;
    public string? ObservacaoPadrao { get; set; }
    public decimal? UltimoPrecoUnitario { get; set; }
    public DateTime? DataHoraUltimoPreco { get; set; }
    public List<ItemListaCompra> Itens { get; set; } = [];
    public List<DesejoCompra> Desejos { get; set; } = [];
    public List<HistoricoProduto> HistoricosPreco { get; set; } = [];
}


