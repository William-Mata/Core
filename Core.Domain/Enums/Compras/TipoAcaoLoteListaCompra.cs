namespace Core.Domain.Enums.Compras;

public enum TipoAcaoLoteListaCompra
{
    MarcarSelecionadosComoComprados = 1,
    DesmarcarSelecionados = 2,
    ExcluirSelecionados = 3,
    ExcluirComprados = 4,
    ExcluirNaoComprados = 5,
    ExcluirSemPreco = 6,
    LimparLista = 7,
    ResetarPrecos = 8,
    ResetarCores = 9,
    CriarNovaListaComComprados = 10,
    CriarNovaListaComNaoComprados = 11,
    DuplicarLista = 12,
    MesclarDuplicados = 13
}
