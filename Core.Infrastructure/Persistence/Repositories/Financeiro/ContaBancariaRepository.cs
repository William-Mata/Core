using Core.Domain.Entities.Financeiro;
using Core.Domain.Interfaces.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories.Financeiro;

public sealed class ContaBancariaRepository(AppDbContext dbContext) : IContaBancariaRepository
{
    public Task<List<ContaBancaria>> ListarAsync(CancellationToken cancellationToken = default) =>
        ListarCoreAsync(null, cancellationToken);

    public Task<List<ContaBancaria>> ListarAsync(int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ListarCoreAsync(usuarioCadastroId, cancellationToken);

    private Task<List<ContaBancaria>> ListarCoreAsync(int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.ContasBancarias
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.Extrato)
            .Include(x => x.Logs)
            .OrderByDescending(x => x.DataAbertura)
            .ToListAsync(cancellationToken);

    public Task<ContaBancaria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, null, cancellationToken);

    public Task<ContaBancaria?> ObterPorIdAsync(long id, int usuarioCadastroId, CancellationToken cancellationToken = default) =>
        ObterCoreAsync(id, usuarioCadastroId, cancellationToken);

    private Task<ContaBancaria?> ObterCoreAsync(long id, int? usuarioCadastroId, CancellationToken cancellationToken) =>
        dbContext.ContasBancarias
            .Where(x => x.Id == id)
            .Where(x => !usuarioCadastroId.HasValue || x.UsuarioCadastroId == usuarioCadastroId.Value)
            .Include(x => x.Extrato)
            .Include(x => x.Logs)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ContaBancaria> CriarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
    {
        dbContext.ContasBancarias.Add(conta);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conta;
    }

    public async Task<ContaBancaria> AtualizarAsync(ContaBancaria conta, CancellationToken cancellationToken = default)
    {
        dbContext.ContasBancarias.Update(conta);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conta;
    }
}
