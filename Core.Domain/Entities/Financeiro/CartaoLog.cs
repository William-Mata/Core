using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class CartaoLog
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long CartaoId { get; set; }
    public AcaoLogs Acao { get; set; }
    public string Descricao { get; set; } = string.Empty;
}