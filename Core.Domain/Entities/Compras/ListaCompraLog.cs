using Core.Domain.Enums;

namespace Core.Domain.Entities.Compras;

public sealed class ListaCompraLog
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ListaCompraId { get; set; }
    public long? ItemListaCompraId { get; set; }
    public AcaoLogs Acao { get; set; } = AcaoLogs.Atualizacao;
    public string Descricao { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public ListaCompra? ListaCompra { get; set; }
    public ItemListaCompra? ItemListaCompra { get; set; }
}
