namespace Core.Application.DTOs.Financeiro;

public sealed record DocumentoRequest(string NomeArquivo, string ConteudoBase64, string? ContentType = null);
public sealed record DocumentoDto(string NomeArquivo, string Caminho, string? ContentType, long TamanhoBytes);
