using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class HistoricoTransacaoFinanceira
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioOperacaoId { get; set; }
    public TipoTransacaoFinanceira TipoTransacao { get; set; }
    public long TransacaoId { get; set; }
    public TipoOperacaoTransacaoFinanceira TipoOperacao { get; set; }
    public TipoContaTransacaoFinanceira TipoConta { get; set; } = TipoContaTransacaoFinanceira.NaoInformado;
    public long? ContaBancariaId { get; set; }
    public long? ContaDestinoId { get; set; }
    public long? CartaoId { get; set; }
    public DateOnly DataTransacao { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public bool OcultarDoHistorico { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }
    public TipoRecebimento? TipoRecebimento { get; set; }
    public decimal ValorAntesTransacao { get; set; }
    public decimal ValorTransacao { get; set; }
    public decimal ValorDepoisTransacao { get; set; }
}
