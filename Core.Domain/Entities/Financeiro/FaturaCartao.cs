using Core.Domain.Enums.Financeiro;

namespace Core.Domain.Entities.Financeiro;

public sealed class FaturaCartao
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long CartaoId { get; set; }
    public string Competencia { get; set; } = string.Empty;
    public DateOnly? DataVencimento { get; set; }
    public DateOnly? DataFechamento { get; set; }
    public DateTime? DataEfetivacao { get; set; }
    public DateTime? DataEstorno { get; set; }
    public long? DespesaPagamentoId { get; set; }
    public decimal ValorTotal { get; set; }
    public StatusFaturaCartao Status { get; set; } = StatusFaturaCartao.Aberta;
}
