using Core.Domain.Entities.Compras;
using Core.Domain.Enums.Compras;
using Core.Domain.Interfaces.Compras;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Compras;

public sealed class ComprasRepository(AppDbContext dbContext) : IComprasRepository
{
    public Task<List<ListaCompra>> ListarListasAcessiveisAsync(int usuarioId, bool incluirArquivadas, CancellationToken cancellationToken = default)
    {
        return dbContext.ListasCompras
            .Where(x =>
                x.UsuarioProprietarioId == usuarioId ||
                x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))
            .Where(x => incluirArquivadas || x.Status == StatusListaCompra.Ativa)
            .Include(x => x.Itens)
            .Include(x => x.Participantes)
            .OrderByDescending(x => x.DataHoraAtualizacao)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<ListaCompra?> ObterListaAcessivelPorIdAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.ListasCompras
            .Where(x => x.Id == listaId)
            .Where(x =>
                x.UsuarioProprietarioId == usuarioId ||
                x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))
            .Include(x => x.Itens)
                .ThenInclude(x => x.Produto)
            .Include(x => x.Participantes)
            .Include(x => x.Logs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ListaCompra?> ObterListaDoProprietarioAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.ListasCompras
            .Where(x => x.Id == listaId && x.UsuarioProprietarioId == usuarioId)
            .Include(x => x.Itens)
                .ThenInclude(x => x.Produto)
            .Include(x => x.Participantes)
            .Include(x => x.Logs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddListaAsync(ListaCompra lista, CancellationToken cancellationToken = default)
    {
        dbContext.ListasCompras.Add(lista);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverListaAsync(ListaCompra lista, CancellationToken cancellationToken = default)
    {
        dbContext.ListasCompras.Remove(lista);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Produto>> BuscarSugestoesProdutosAsync(int usuarioId, string descricao, int limite, CancellationToken cancellationToken = default)
    {
        return dbContext.Produtos
            .AsNoTracking()
            .Where(x => x.DescricaoNormalizada.Contains(descricao))
            .Where(x =>
                x.UsuarioCadastroId == usuarioId ||
                x.Desejos.Any(d => d.UsuarioCadastroId == usuarioId) ||
                x.Itens.Any(i =>
                    i.ListaCompra != null &&
                    (i.ListaCompra.UsuarioProprietarioId == usuarioId ||
                     i.ListaCompra.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))))
            .OrderByDescending(x => x.DataHoraUltimoPreco)
            .ThenBy(x => x.Descricao)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public Task<Produto?> ObterProdutoPorDescricaoEUnidadeAsync(string descricaoNormalizada, UnidadeMedidaCompra unidade, CancellationToken cancellationToken = default)
    {
        return dbContext.Produtos
            .FirstOrDefaultAsync(x => x.DescricaoNormalizada == descricaoNormalizada && x.UnidadePadrao == unidade, cancellationToken);
    }

    public async Task AddProdutoAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        dbContext.Produtos.Add(produto);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<DesejoCompra>> ListarDesejosAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.DesejosCompra
            .Where(x => x.UsuarioCadastroId == usuarioId)
            .OrderByDescending(x => x.DataHoraCadastro)
            .ToListAsync(cancellationToken);
    }

    public Task<DesejoCompra?> ObterDesejoAsync(long desejoId, int usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.DesejosCompra
            .FirstOrDefaultAsync(x => x.Id == desejoId && x.UsuarioCadastroId == usuarioId, cancellationToken);
    }

    public Task<List<DesejoCompra>> ObterDesejosAsync(IReadOnlyCollection<long> desejosIds, int usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.DesejosCompra
            .Where(x => desejosIds.Contains(x.Id) && x.UsuarioCadastroId == usuarioId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default)
    {
        dbContext.DesejosCompra.Add(desejo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default)
    {
        dbContext.DesejosCompra.Remove(desejo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<HistoricoProduto>> ListarHistoricoPrecosAsync(
        int usuarioId,
        string? descricao,
        UnidadeMedidaCompra? unidade,
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HistoricosProdutos
            .AsNoTracking()
            .Include(x => x.Produto)
            .Include(x => x.ItemListaCompra!)
                .ThenInclude(x => x.ListaCompra!)
                    .ThenInclude(x => x.Participantes)
            .Where(x =>
                x.UsuarioCadastroId == usuarioId ||
                (x.ItemListaCompra != null &&
                 x.ItemListaCompra.ListaCompra != null &&
                 (x.ItemListaCompra.ListaCompra.UsuarioProprietarioId == usuarioId ||
                  x.ItemListaCompra.ListaCompra.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))));

        if (!string.IsNullOrWhiteSpace(descricao))
            query = query.Where(x => x.Produto != null && x.Produto.DescricaoNormalizada.Contains(descricao));

        if (unidade.HasValue)
            query = query.Where(x => x.Unidade == unidade.Value);

        if (dataInicio.HasValue)
            query = query.Where(x => x.DataHoraCadastro >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(x => x.DataHoraCadastro <= dataFim.Value);

        return query
            .OrderByDescending(x => x.DataHoraCadastro)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}


