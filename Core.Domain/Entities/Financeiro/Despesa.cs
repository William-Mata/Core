using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Despesa
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public DateOnly DataLancamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataEfetivacao { get; set; }
    public string TipoDespesa { get; set; } = string.Empty;
    public string TipoPagamento { get; set; } = string.Empty;
    public Recorrencia Recorrencia { get; set; } = Recorrencia.Unica;
    public bool RecorrenciaFixa { get; set; }
    public int? QuantidadeRecorrencia { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorLiquido { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Imposto { get; set; }
    public decimal Juros { get; set; }
    public decimal? ValorEfetivacao { get; set; }
    public StatusDespesa Status { get; set; } = StatusDespesa.Pendente;
    public string? AnexoDocumento { get; set; }
    public List<DespesaAmigoRateio> AmigosRateio { get; set; } = [];
    public List<DespesaAreaRateio> AreasRateio { get; set; } = [];
    public List<DespesaLog> Logs { get; set; } = [];
}
