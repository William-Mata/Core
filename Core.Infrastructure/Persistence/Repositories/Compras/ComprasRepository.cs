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

    public Task<List<ItemListaCompra>> BuscarSugestoesItensAsync(int usuarioId, string descricao, int limite, CancellationToken cancellationToken = default)
    {
        return dbContext.ItensListasCompras
            .AsNoTracking()
            .Where(x => x.DescricaoNormalizada.Contains(descricao))
            .Where(x =>
                x.ListaCompra != null &&
                (x.ListaCompra.UsuarioProprietarioId == usuarioId ||
                 x.ListaCompra.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status)))
            .OrderBy(x => x.Descricao)
            .ThenByDescending(x => x.DataHoraCadastro)
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

    public async Task<ComprasDashboardKpisReadModel> ObterDashboardKpisAsync(int usuarioId, DateTime inicioMes, DateTime fimMesExclusivo, CancellationToken cancellationToken = default)
    {
        var itensCompradosMesQuery = ItensCompradosAcessiveisQuery(usuarioId)
            .Where(x => x.DataHoraCompra >= inicioMes && x.DataHoraCompra < fimMesExclusivo);

        var totalGastoMes = await itensCompradosMesQuery
            .SumAsync(x => (decimal?)x.ValorTotal, cancellationToken) ?? 0m;

        var itensCompradosMes = await itensCompradosMesQuery.CountAsync(cancellationToken);

        var planejamentosAtivos = await ListasAcessiveisQuery(usuarioId)
            .CountAsync(x => x.Status == StatusListaCompra.Ativa, cancellationToken);

        var desejosPendentes = await dbContext.DesejosCompra
            .AsNoTracking()
            .CountAsync(x => x.UsuarioCadastroId == usuarioId && !x.Convertido, cancellationToken);

        var historicos = HistoricosPrecosAcessiveisQuery(usuarioId)
            .Where(x => x.PrecoUnitario > 0);

        var estatisticasPrecosQuery = historicos
            .GroupBy(x => new { x.ProdutoId, x.Unidade })
            .Select(g => new
            {
                g.Key.ProdutoId,
                g.Key.Unidade,
                MenorPreco = g.Min(x => x.PrecoUnitario),
                UltimaData = g.Max(x => x.DataHoraCadastro),
                TotalOcorrencias = g.Count()
            })
            .Where(x => x.TotalOcorrencias >= 2 && x.UltimaData >= inicioMes && x.UltimaData < fimMesExclusivo);

        var estatisticasPrecos = await estatisticasPrecosQuery.ToListAsync(cancellationToken);
        var ultimosPrecos = await (
                from historico in historicos
                join estatistica in estatisticasPrecosQuery
                    on new { historico.ProdutoId, historico.Unidade, historico.DataHoraCadastro }
                    equals new { estatistica.ProdutoId, estatistica.Unidade, DataHoraCadastro = estatistica.UltimaData }
                select new
                {
                    historico.ProdutoId,
                    historico.Unidade,
                    historico.Id,
                    historico.PrecoUnitario
                })
            .ToListAsync(cancellationToken);

        var ultimosPrecosPorProduto = ultimosPrecos
            .GroupBy(x => new { x.ProdutoId, x.Unidade })
            .ToDictionary(
                x => (x.Key.ProdutoId, x.Key.Unidade),
                x => x.OrderByDescending(item => item.Id).First().PrecoUnitario);

        var economiaPotencialMes = estatisticasPrecos.Sum(x =>
        {
            var ultimoPreco = ultimosPrecosPorProduto.TryGetValue((x.ProdutoId, x.Unidade), out var preco)
                ? preco
                : 0m;
            return Math.Max(0m, ultimoPreco - x.MenorPreco);
        });

        return new ComprasDashboardKpisReadModel(
            totalGastoMes,
            planejamentosAtivos,
            itensCompradosMes,
            desejosPendentes,
            decimal.Round(economiaPotencialMes, 2));
    }

    public async Task<List<ComprasDashboardEvolucaoMensalReadModel>> ListarDashboardEvolucaoMensalAsync(int usuarioId, DateTime inicio, DateTime fimExclusivo, CancellationToken cancellationToken = default)
    {
        var agregados = await ItensCompradosAcessiveisQuery(usuarioId)
            .Where(x => x.DataHoraCompra >= inicio && x.DataHoraCompra < fimExclusivo)
            .Select(x => new
            {
                Ano = x.DataHoraCompra!.Value.Year,
                Mes = x.DataHoraCompra.Value.Month,
                x.ValorTotal,
                x.ListaCompraId,
                ListaStatus = x.ListaCompra!.Status
            })
            .GroupBy(x => new { x.Ano, x.Mes })
            .Select(g => new
            {
                Year = g.Key.Ano,
                Month = g.Key.Mes,
                ValorTotal = g.Sum(x => x.ValorTotal),
                QuantidadeItens = g.Count(),
                ListasFinalizadas = g.Where(x => x.ListaStatus == StatusListaCompra.Arquivada)
                    .Select(x => x.ListaCompraId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync(cancellationToken);

        return agregados
            .Select(x => new ComprasDashboardEvolucaoMensalReadModel(x.Year, x.Month, x.ValorTotal, x.QuantidadeItens, x.ListasFinalizadas))
            .ToList();
    }

    public async Task<List<ComprasDashboardTipoCompraReadModel>> ListarDashboardTiposCompraAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var agregados = await ItensCompradosAcessiveisQuery(usuarioId)
            .Where(x => x.ListaCompra != null)
            .Select(x => new
            {
                Categoria = x.ListaCompra!.Categoria,
                x.ValorTotal
            })
            .GroupBy(x => x.Categoria)
            .Select(g => new
            {
                Categoria = g.Key,
                ValorTotal = g.Sum(x => x.ValorTotal),
                QuantidadeItens = g.Count()
            })
            .OrderByDescending(x => x.ValorTotal)
            .ToListAsync(cancellationToken);

        return agregados
            .Select(x => new ComprasDashboardTipoCompraReadModel(x.Categoria, x.ValorTotal, x.QuantidadeItens))
            .ToList();
    }

    public async Task<List<ComprasDashboardProdutoMaisCompradoReadModel>> ListarDashboardProdutosMaisCompradosAsync(int usuarioId, int limite, CancellationToken cancellationToken = default)
    {
        var agregados = await ItensCompradosAcessiveisQuery(usuarioId)
            .GroupBy(x => x.Descricao)
            .Select(g => new
            {
                Descricao = g.Key,
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Quantidade)
            .ThenBy(x => x.Descricao)
            .Take(limite)
            .ToListAsync(cancellationToken);

        return agregados
            .Select(x => new ComprasDashboardProdutoMaisCompradoReadModel(x.Descricao, x.Quantidade))
            .ToList();
    }

    public async Task<List<ComprasDashboardUltimaCompraReadModel>> ListarDashboardUltimasComprasAsync(int usuarioId, int limite, CancellationToken cancellationToken = default)
    {
        var compras = await ItensCompradosAcessiveisQuery(usuarioId)
            .Where(x => x.DataHoraCompra.HasValue && x.ListaCompra != null)
            .Select(x => new
            {
                x.Id,
                x.Descricao,
                Valor = x.ValorTotal,
                Data = x.DataHoraCompra!.Value,
                Planejamento = x.ListaCompra!.Nome,
                CorMarcador = x.EtiquetaCor
            })
            .OrderByDescending(x => x.Data)
            .ThenByDescending(x => x.Id)
            .Take(limite)
            .ToListAsync(cancellationToken);

        return compras
            .Select(x => new ComprasDashboardUltimaCompraReadModel(x.Id, x.Descricao, x.Valor, x.Data, x.Planejamento, x.CorMarcador))
            .ToList();
    }

    public async Task<List<ComprasDashboardUltimoDesejoReadModel>> ListarDashboardUltimosDesejosAsync(int usuarioId, int limite, CancellationToken cancellationToken = default)
    {
        var desejos = await dbContext.DesejosCompra
            .AsNoTracking()
            .Where(x => x.UsuarioCadastroId == usuarioId)
            .OrderByDescending(x => x.DataHoraCadastro)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Descricao,
                ValorEstimado = x.PrecoEstimado,
                Data = x.DataHoraCadastro,
                x.Convertido
            })
            .Take(limite)
            .ToListAsync(cancellationToken);

        return desejos
            .Select(x => new ComprasDashboardUltimoDesejoReadModel(x.Id, x.Descricao, x.ValorEstimado, x.Data, x.Convertido))
            .ToList();
    }

    public async Task<List<ComprasDashboardVariacaoPrecoReadModel>> ListarDashboardVariacoesPrecosAsync(int usuarioId, int limite, CancellationToken cancellationToken = default)
    {
        var historicos = HistoricosPrecosAcessiveisQuery(usuarioId)
            .Where(x => x.PrecoUnitario > 0 && x.Produto != null);

        var estatisticasPrecosQuery = historicos
            .GroupBy(x => new { x.ProdutoId, x.Unidade })
            .Select(g => new
            {
                g.Key.ProdutoId,
                g.Key.Unidade,
                MenorPreco = g.Min(x => x.PrecoUnitario),
                MaiorPreco = g.Max(x => x.PrecoUnitario),
                MediaPreco = g.Average(x => x.PrecoUnitario),
                UltimaData = g.Max(x => x.DataHoraCadastro),
                TotalOcorrencias = g.Count()
            })
            .Where(x => x.TotalOcorrencias >= 2);

        var estatisticasPrecos = await estatisticasPrecosQuery.ToListAsync(cancellationToken);
        var ultimosHistoricos = await (
                from historico in historicos
                join estatistica in estatisticasPrecosQuery
                    on new { historico.ProdutoId, historico.Unidade, historico.DataHoraCadastro }
                    equals new { estatistica.ProdutoId, estatistica.Unidade, DataHoraCadastro = estatistica.UltimaData }
                select new
                {
                    historico.ProdutoId,
                    historico.Unidade,
                    historico.Id,
                    historico.PrecoUnitario,
                    Produto = historico.Produto!.Descricao
                })
            .ToListAsync(cancellationToken);

        var ultimosPorProduto = ultimosHistoricos
            .GroupBy(x => new { x.ProdutoId, x.Unidade })
            .ToDictionary(
                x => (x.Key.ProdutoId, x.Key.Unidade),
                x => x.OrderByDescending(item => item.Id).First());

        return estatisticasPrecos
            .Select(x =>
            {
                var ultimo = ultimosPorProduto.TryGetValue((x.ProdutoId, x.Unidade), out var historico)
                    ? historico
                    : null;

                return new
                {
                    x.ProdutoId,
                    x.Unidade,
                    Produto = ultimo?.Produto ?? string.Empty,
                    UltimoPreco = ultimo?.PrecoUnitario ?? 0m,
                    x.MenorPreco,
                    x.MaiorPreco,
                    x.MediaPreco,
                    x.TotalOcorrencias
                };
            })
            .Where(x => x.UltimoPreco > x.MenorPreco)
            .OrderByDescending(x => x.UltimoPreco - x.MenorPreco)
            .ThenBy(x => x.Produto)
            .Take(limite)
            .Select(x => new ComprasDashboardVariacaoPrecoReadModel(
                x.ProdutoId,
                x.Unidade,
                x.Produto,
                x.UltimoPreco,
                x.MenorPreco,
                x.MaiorPreco,
                x.MediaPreco,
                x.TotalOcorrencias))
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<ListaCompra> ListasAcessiveisQuery(int usuarioId) =>
        dbContext.ListasCompras
            .AsNoTracking()
            .Where(x =>
                x.UsuarioProprietarioId == usuarioId ||
                x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status));

    private IQueryable<ItemListaCompra> ItensCompradosAcessiveisQuery(int usuarioId) =>
        dbContext.ItensListasCompras
            .AsNoTracking()
            .Where(x => x.Comprado && x.DataHoraCompra.HasValue)
            .Where(x =>
                x.ListaCompra != null &&
                (x.ListaCompra.UsuarioProprietarioId == usuarioId ||
                 x.ListaCompra.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status)));

    private IQueryable<HistoricoProduto> HistoricosPrecosAcessiveisQuery(int usuarioId) =>
        dbContext.HistoricosProdutos
            .AsNoTracking()
            .Where(x =>
                x.UsuarioCadastroId == usuarioId ||
                (x.ItemListaCompra != null &&
                 x.ItemListaCompra.ListaCompra != null &&
                 (x.ItemListaCompra.ListaCompra.UsuarioProprietarioId == usuarioId ||
                  x.ItemListaCompra.ListaCompra.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))));
}


