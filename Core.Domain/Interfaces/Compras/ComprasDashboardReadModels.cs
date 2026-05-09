using Core.Domain.Enums.Compras;

namespace Core.Domain.Interfaces.Compras;

public sealed record ComprasDashboardKpisReadModel(
    decimal TotalGastoMes,
    int PlanejamentosAtivos,
    int ItensCompradosMes,
    int DesejosPendentes,
    decimal EconomiaPotencialMes);

public sealed record ComprasDashboardEvolucaoMensalReadModel(
    int Ano,
    int Mes,
    decimal ValorTotal,
    int QuantidadeItens,
    int ListasFinalizadas);

public sealed record ComprasDashboardTipoCompraReadModel(
    string Categoria,
    decimal ValorTotal,
    int QuantidadeItens);

public sealed record ComprasDashboardProdutoMaisCompradoReadModel(
    string Descricao,
    int Quantidade);

public sealed record ComprasDashboardUltimaCompraReadModel(
    long Id,
    string Descricao,
    decimal Valor,
    DateTime Data,
    string Planejamento,
    string? CorMarcador);

public sealed record ComprasDashboardUltimoDesejoReadModel(
    long Id,
    string Descricao,
    decimal? ValorEstimado,
    DateTime Data,
    bool Convertido);

public sealed record ComprasDashboardVariacaoPrecoReadModel(
    long ProdutoId,
    UnidadeMedidaCompra Unidade,
    string Produto,
    decimal UltimoPreco,
    decimal MenorPreco,
    decimal MaiorPreco,
    decimal MediaPreco,
    int TotalOcorrencias);
