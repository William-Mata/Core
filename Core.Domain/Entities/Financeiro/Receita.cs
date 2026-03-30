using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Receita
{
    public long Id { get; set; }
    public long? ReceitaOrigemId { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public DateOnly DataLancamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataEfetivacao { get; set; }
    public string TipoReceita { get; set; } = string.Empty;
    public string TipoRecebimento { get; set; } = string.Empty;
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
    public StatusReceita Status { get; set; } = StatusReceita.Pendente;
    public long? ContaBancariaId { get; set; }
    public List<Documento> Documentos { get; set; } = [];
    public List<ReceitaAmigoRateio> AmigosRateio { get; set; } = [];
    public List<ReceitaAreaRateio> AreasRateio { get; set; } = [];
    public List<ReceitaLog> Logs { get; set; } = [];
}
