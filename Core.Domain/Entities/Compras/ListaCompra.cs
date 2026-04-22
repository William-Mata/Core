using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class ListaCompra
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioProprietarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public StatusListaCompra Status { get; set; } = StatusListaCompra.Ativa;
    public DateTime DataHoraAtualizacao { get; set; } = DateTime.UtcNow;
    public List<ItemListaCompra> Itens { get; set; } = [];
    public List<ParticipacaoListaCompra> Participantes { get; set; } = [];
    public List<ListaCompraLog> Logs { get; set; } = [];
}
