using Core.Domain.Enums.Financeiro;

namespace Core.Domain.Entities.Financeiro;

public sealed class Reembolso
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public string Competencia { get; set; } = string.Empty;
    public DateTime DataLancamento { get; set; }
    public DateOnly? DataVencimento { get; set; }
    public DateTime? DataEfetivacao { get; set; }
    public long? CartaoId { get; set; }
    public long? FaturaCartaoId { get; set; }
    public List<Documento> Documentos { get; set; } = [];
    public decimal ValorTotal { get; set; }
    public StatusReembolso Status { get; set; } = StatusReembolso.Aguardando;
    public List<ReembolsoDespesa> Despesas { get; set; } = [];
}
