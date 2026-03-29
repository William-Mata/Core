using Core.Application.DTOs.Financeiro;

namespace Core.Application.Contracts.Financeiro;

public interface IDocumentoStorageService
{
    Task<IReadOnlyCollection<DocumentoDto>> SalvarAsync(IReadOnlyCollection<DocumentoRequest> documentos, CancellationToken cancellationToken = default);
}
