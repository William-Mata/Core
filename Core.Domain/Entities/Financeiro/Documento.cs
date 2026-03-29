namespace Core.Domain.Entities.Financeiro;

public sealed class Documento
{
    public long Id { get; set; }
    public DateTime DataHoraCadastro { get; set; } = DateTime.UtcNow;
    public int UsuarioCadastroId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long TamanhoBytes { get; set; }
    public long? DespesaId { get; set; }
    public long? ReceitaId { get; set; }
    public long? ReembolsoId { get; set; }
}
