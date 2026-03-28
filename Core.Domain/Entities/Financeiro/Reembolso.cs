using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Reembolso
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Solicitante { get; set; } = string.Empty;
    public DateOnly DataSolicitacao { get; set; }
    public DateOnly? DataEfetivacao { get; set; }
    public decimal ValorTotal { get; set; }
    public StatusReembolso Status { get; set; } = StatusReembolso.Aguardando;
    public List<ReembolsoDespesa> Despesas { get; set; } = [];
}
