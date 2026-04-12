using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Despesa
{
    public long Id { get; set; }
    public long? DespesaOrigemId { get; set; }
    public long? DespesaRecorrenciaOrigemId { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public DateOnly DataLancamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataEfetivacao { get; set; }
    public TipoDespesa TipoDespesa { get; set; } = TipoDespesa.Alimentacao;
    public TipoPagamento TipoPagamento { get; set; } = TipoPagamento.Pix;
    public Recorrencia Recorrencia { get; set; } = Recorrencia.Unica;
    public bool RecorrenciaFixa { get; set; }
    public int? QuantidadeRecorrencia { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal? ValorTotalRateioAmigos { get; set; }
    public TipoRateioAmigos? TipoRateioAmigos { get; set; }
    public decimal ValorLiquido { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Imposto { get; set; }
    public decimal Juros { get; set; }
    public decimal? ValorEfetivacao { get; set; }
    public StatusDespesa Status { get; set; } = StatusDespesa.Pendente;
    public long? ContaBancariaId { get; set; }
    public long? ContaDestinoId { get; set; }
    public long? ReceitaTransferenciaId { get; set; }
    public long? CartaoId { get; set; }
    public List<Documento> Documentos { get; set; } = [];
    public List<DespesaAmigoRateio> AmigosRateio { get; set; } = [];
    public List<DespesaAreaRateio> AreasRateio { get; set; } = [];
    public List<DespesaLog> Logs { get; set; } = [];
}
