using Core.Domain.Entities.Compras;
using Core.Domain.Enums.Compras;

namespace Core.Domain.Interfaces.Compras;

public interface IComprasRepository
{
    Task<List<ListaCompra>> ListarListasAcessiveisAsync(int usuarioId, bool incluirArquivadas, CancellationToken cancellationToken = default);
    Task<ListaCompra?> ObterListaAcessivelPorIdAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default);
    Task<ListaCompra?> ObterListaDoProprietarioAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default);
    Task AddListaAsync(ListaCompra lista, CancellationToken cancellationToken = default);
    Task RemoverListaAsync(ListaCompra lista, CancellationToken cancellationToken = default);
    Task<List<ItemListaCompra>> BuscarSugestoesItensAsync(int usuarioId, string descricao, int limite, CancellationToken cancellationToken = default);
    Task<Produto?> ObterProdutoPorDescricaoEUnidadeAsync(string descricaoNormalizada, UnidadeMedidaCompra unidade, CancellationToken cancellationToken = default);
    Task AddProdutoAsync(Produto produto, CancellationToken cancellationToken = default);
    Task<List<DesejoCompra>> ListarDesejosAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<DesejoCompra?> ObterDesejoAsync(long desejoId, int usuarioId, CancellationToken cancellationToken = default);
    Task<List<DesejoCompra>> ObterDesejosAsync(IReadOnlyCollection<long> desejosIds, int usuarioId, CancellationToken cancellationToken = default);
    Task AddDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default);
    Task RemoverDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default);
    Task<List<HistoricoProduto>> ListarHistoricoPrecosAsync(int usuarioId, string? descricao, UnidadeMedidaCompra? unidade, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}


