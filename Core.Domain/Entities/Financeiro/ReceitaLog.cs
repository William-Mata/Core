using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class ReceitaLog
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public long ReceitaId { get; set; }
    public AcaoLogs Acao { get; set; }
    public string Descricao { get; set; } = string.Empty;
}