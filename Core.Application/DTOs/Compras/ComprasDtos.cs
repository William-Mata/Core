using Core.Domain.Enums;
using Core.Domain.Enums.Compras;

namespace Core.Application.DTOs.Compras;

public sealed record CriarListaCompraRequest(string Nome, string Categoria, string? Observacao = null);
public sealed record AtualizarListaCompraRequest(string Nome, string Categoria, string? Observacao = null);
public sealed record CompartilharListaCompraRequest(int AmigoId, PapelParticipacaoListaCompra Papel = PapelParticipacaoListaCompra.CoProprietario);
public sealed record CriarItemListaCompraRequest(
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoUnitario,
    string? EtiquetaCor);
public sealed record AtualizarItemListaCompraRequest(
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoUnitario,
    string? EtiquetaCor,
    bool Comprado);
public sealed record EdicaoRapidaItemListaCompraRequest(decimal Quantidade, decimal? PrecoUnitario);
public sealed record MarcarCompradoItemListaCompraRequest(bool Comprado);
public sealed record AcaoLoteListaCompraRequest(
    TipoAcaoLoteListaCompra Acao,
    IReadOnlyCollection<long>? ItensIds = null,
    string? NomeNovaLista = null,
    string? CategoriaNovaLista = null);
public sealed record CriarDesejoCompraRequest(
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoEstimado);
public sealed record AtualizarDesejoCompraRequest(
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoEstimado);
public sealed record ConverterDesejosCompraRequest(
    IReadOnlyCollection<long> DesejosIds,
    long? ListaDestinoId,
    string? NomeNovaLista,
    string? CategoriaNovaLista,
    AcaoPosConversaoDesejoCompra AcaoPosConversao);

public sealed record ParticipanteListaCompraDto(int UsuarioId, string Nome, string Email, string Papel);
public sealed record ListaCompraResumoDto(
    long Id,
    string Nome,
    string Categoria,
    string? Observacao,
    string Status,
    string PapelUsuario,
    decimal ValorTotal,
    decimal ValorComprado,
    decimal PercentualComprado,
    int QuantidadeItens,
    int QuantidadeItensComprados,
    int QuantidadeParticipantes,
    DateTime DataHoraAtualizacao);
public sealed record ItemListaCompraDto(
    long Id,
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoUnitario,
    decimal ValorTotal,
    string? EtiquetaCor,
    bool Comprado,
    DateTime? DataHoraCompra);
public sealed record ListaCompraLogDto(
    long Id,
    DateTime DataHoraCadastro,
    int UsuarioCadastroId,
    long? ItemListaCompraId,
    AcaoLogs Acao,
    string Descricao,
    string? ValorAnterior,
    string? ValorNovo);
public sealed record ListaCompraDetalheDto(
    long Id,
    string Nome,
    string Categoria,
    string? Observacao,
    string Status,
    decimal ValorTotal,
    decimal ValorComprado,
    decimal PercentualComprado,
    int QuantidadeItens,
    int QuantidadeItensComprados,
    IReadOnlyCollection<ItemListaCompraDto> Itens,
    IReadOnlyCollection<ParticipanteListaCompraDto> Participantes,
    IReadOnlyCollection<ListaCompraLogDto> Logs,
    DateTime DataHoraAtualizacao);
public sealed record SugestaoProdutoCompraDto(
    long ProdutoId,
    string Descricao,
    UnidadeMedidaCompra Unidade,
    string? ObservacaoPadrao,
    decimal? UltimoPrecoUnitario);
public sealed record AcaoLoteListaCompraResultadoDto(
    string Acao,
    int ItensAfetados,
    long? NovaListaId = null);
public sealed record DesejoCompraDto(
    long Id,
    string Descricao,
    string? Observacao,
    UnidadeMedidaCompra Unidade,
    decimal Quantidade,
    decimal? PrecoEstimado,
    bool Convertido,
    DateTime? DataHoraConversao);
public sealed record ResultadoConversaoDesejosDto(long ListaId, int ItensCriados, int DesejosProcessados);
public sealed record HistoricoProdutoDto(
    long ProdutoId,
    string Descricao,
    UnidadeMedidaCompra Unidade,
    decimal UltimoPreco,
    decimal MenorPreco,
    decimal MaiorPreco,
    decimal MediaPreco,
    DateTime DataUltimoPreco,
    int TotalOcorrencias);


