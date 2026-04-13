using Core.Domain.Entities.Financeiro;

namespace Core.Domain.Interfaces.Financeiro;

public interface IFaturaCartaoRepository
{
    Task<List<FaturaCartao>> ListarPorUsuarioAsync(int usuarioCadastroId, long? cartaoId, string? competencia, CancellationToken cancellationToken = default);
    Task<FaturaCartao?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default);
    Task<FaturaCartao?> ObterPorCartaoCompetenciaAsync(long cartaoId, int usuarioCadastroId, string competencia, CancellationToken cancellationToken = default);
    Task<FaturaCartao> CriarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default);
    Task<FaturaCartao> AtualizarAsync(FaturaCartao fatura, CancellationToken cancellationToken = default);
}
