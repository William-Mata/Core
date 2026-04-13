using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class Cartao
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Bandeira { get; set; } = string.Empty;
    public TipoCartao Tipo { get; set; } = TipoCartao.Credito;
    public decimal? Limite { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public DateOnly? DiaVencimento { get; set; }
    public DateOnly? DataVencimentoCartao { get; set; }
    public StatusCartao Status { get; set; } = StatusCartao.Ativo;
    public List<CartaoLog> Logs { get; set; } = [];
}
