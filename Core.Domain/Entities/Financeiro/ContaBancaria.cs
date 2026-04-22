using Core.Domain.Enums.Financeiro;

namespace Core.Domain.Entities.Financeiro;

public sealed class ContaBancaria
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
    public string Agencia { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; }
    public decimal SaldoAtual { get; set; }
    public DateOnly DataAbertura { get; set; }
    public StatusContaBancaria Status { get; set; } = StatusContaBancaria.Ativa;
    public List<ContaBancariaExtrato> Extrato { get; set; } = [];
    public List<ContaBancariaLog> Logs { get; set; } = [];
}