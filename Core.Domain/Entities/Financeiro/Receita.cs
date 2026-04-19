using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Receita
{
    public long Id { get; set; }
    public long? ReceitaOrigemId { get; set; }
    public long? ReceitaRecorrenciaOrigemId { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public string Competencia { get; set; } = string.Empty;
    public DateTime DataLancamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateTime? DataEfetivacao { get; set; }
    public TipoReceita TipoReceita { get; set; } = TipoReceita.Salario;
    public TipoRecebimento TipoRecebimento { get; set; } = TipoRecebimento.Dinheiro;
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
    public StatusReceita Status { get; set; } = StatusReceita.Pendente;
    public long? ContaBancariaId { get; set; }
    public long? ContaDestinoId { get; set; }
    public long? DespesaTransferenciaId { get; set; }
    public long? CartaoId { get; set; }
    public long? FaturaCartaoId { get; set; }
    public List<Documento> Documentos { get; set; } = [];
    public List<ReceitaAmigoRateio> AmigosRateio { get; set; } = [];
    public List<ReceitaAreaRateio> AreasRateio { get; set; } = [];
    public List<ReceitaLog> Logs { get; set; } = [];
}
