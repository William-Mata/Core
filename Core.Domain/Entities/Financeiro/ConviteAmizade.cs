using Core.Domain.Enums;

namespace Core.Domain.Entities.Financeiro;

public sealed class ConviteAmizade
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public int UsuarioOrigemId { get; set; }
    public int UsuarioDestinoId { get; set; }
    public string? Mensagem { get; set; }
    public StatusConviteAmizade Status { get; set; } = StatusConviteAmizade.Pendente;
    public DateTime? DataHoraResposta { get; set; }
}
