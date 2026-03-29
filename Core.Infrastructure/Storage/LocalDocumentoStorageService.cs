using Core.Application.Contracts.Financeiro;
using Core.Application.DTOs.Financeiro;
using Microsoft.Extensions.Hosting;

namespace Core.Infrastructure.Storage;

public sealed class LocalDocumentoStorageService(IHostEnvironment hostEnvironment) : IDocumentoStorageService
{
    private static readonly string BasePathLocal = @"C:\temp";

    public async Task<IReadOnlyCollection<DocumentoDto>> SalvarAsync(IReadOnlyCollection<DocumentoRequest> documentos, CancellationToken cancellationToken = default)
    {
        if (documentos.Count == 0) return [];

        var destino = ResolverDiretorioDestino();
        Directory.CreateDirectory(destino);

        var arquivos = new List<DocumentoDto>(documentos.Count);
        foreach (var documento in documentos)
        {
            if (string.IsNullOrWhiteSpace(documento.NomeArquivo) || string.IsNullOrWhiteSpace(documento.ConteudoBase64))
                continue;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(documento.ConteudoBase64);
            }
            catch (FormatException)
            {
                continue;
            }

            if (bytes.Length == 0)
                continue;

            var nomeSeguro = SanitizarNomeArquivo(documento.NomeArquivo);
            var nomeFinal = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{nomeSeguro}";
            var caminhoCompleto = Path.Combine(destino, nomeFinal);

            await File.WriteAllBytesAsync(caminhoCompleto, bytes, cancellationToken);
            arquivos.Add(new DocumentoDto(documento.NomeArquivo, caminhoCompleto, documento.ContentType, bytes.LongLength));
        }

        return arquivos;
    }

    private string ResolverDiretorioDestino()
    {
        if (hostEnvironment.IsDevelopment())
            return BasePathLocal;

        return Path.Combine(Path.GetTempPath(), "Core", "documentos");
    }

    private static string SanitizarNomeArquivo(string nomeArquivo)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var resultado = new string(nomeArquivo.Select(ch => invalidos.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(resultado) ? "arquivo.bin" : resultado;
    }
}
