using Core.Domain.Enums.Compras;

namespace Core.Domain.Entities.Compras;

public sealed class ParticipacaoListaCompra
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ListaCompraId { get; set; }
    public int UsuarioId { get; set; }
    public PapelParticipacaoListaCompra Papel { get; set; } = PapelParticipacaoListaCompra.Editor;
    public bool Status { get; set; } = true;
    public ListaCompra? ListaCompra { get; set; }
}
