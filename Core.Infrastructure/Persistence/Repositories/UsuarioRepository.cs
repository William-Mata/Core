using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Persistence.Repositories;

public sealed class UsuarioRepository(AppDbContext dbContext) : IUsuarioRepository
{
    public async Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Usuarios
            .AsNoTracking()
            .Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(filtroId) && int.TryParse(filtroId.Trim(), out var id))
        {
            query = query.Where(x => x.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(descricao))
        {
            var termo = descricao.Trim();
            query = query.Where(x => x.Nome.Contains(termo) || x.Email.Contains(termo));
        }

        if (dataInicio.HasValue)
        {
            var inicio = dataInicio.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.DataHoraCadastro >= inicio);
        }

        if (dataFim.HasValue)
        {
            var fim = dataFim.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(x => x.DataHoraCadastro <= fim);
        }

        return await query
            .OrderByDescending(x => x.DataHoraCadastro)
            .ThenByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Usuarios
            .Include(x => x.Modulos)
            .Include(x => x.Telas)
            .Include(x => x.Funcionalidades)
            .FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Usuarios.FirstOrDefaultAsync(x => x.Email == email && x.Ativo, cancellationToken);

    public async Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Modulos
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Telas
            .AsNoTracking()
            .OrderBy(x => x.ModuloId)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Funcionalidades
            .AsNoTracking()
            .OrderBy(x => x.TelaId)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        dbContext.Usuarios.Add(usuario);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ConcederPermissoesPadraoAsync(usuario.Id, usuario.UsuarioCadastroId, cancellationToken);
        return usuario;
    }

    public async Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        dbContext.Usuarios.Update(usuario);
        await dbContext.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task SincronizarPermissoesAsync(
        int usuarioId,
        int usuarioCadastroId,
        IReadOnlyCollection<int> modulosAtivosIds,
        IReadOnlyCollection<int> telasAtivasIds,
        IReadOnlyCollection<int> funcionalidadesAtivasIds,
        CancellationToken cancellationToken = default)
    {
        var usuarioModulos = await dbContext.UsuariosModulos
            .Where(x => x.UsuarioId == usuarioId)
            .ToArrayAsync(cancellationToken);

        foreach (var usuarioModulo in usuarioModulos)
        {
            usuarioModulo.Status = modulosAtivosIds.Contains(usuarioModulo.ModuloId);
        }

        var novosModulosIds = modulosAtivosIds
            .Except(usuarioModulos.Select(x => x.ModuloId))
            .ToArray();

        if (novosModulosIds.Length > 0)
        {
            dbContext.UsuariosModulos.AddRange(novosModulosIds.Select(moduloId => new UsuarioModulo
            {
                UsuarioId = usuarioId,
                UsuarioCadastroId = usuarioCadastroId,
                ModuloId = moduloId,
                Status = true
            }));
        }

        var usuarioTelas = await dbContext.UsuariosTelas
            .Where(x => x.UsuarioId == usuarioId)
            .ToArrayAsync(cancellationToken);

        foreach (var usuarioTela in usuarioTelas)
        {
            usuarioTela.Status = telasAtivasIds.Contains(usuarioTela.TelaId);
        }

        var novasTelasIds = telasAtivasIds
            .Except(usuarioTelas.Select(x => x.TelaId))
            .ToArray();

        if (novasTelasIds.Length > 0)
        {
            dbContext.UsuariosTelas.AddRange(novasTelasIds.Select(telaId => new UsuarioTela
            {
                UsuarioId = usuarioId,
                UsuarioCadastroId = usuarioCadastroId,
                TelaId = telaId,
                Status = true
            }));
        }

        var usuarioFuncionalidades = await dbContext.UsuariosFuncionalidades
            .Where(x => x.UsuarioId == usuarioId)
            .ToArrayAsync(cancellationToken);

        foreach (var usuarioFuncionalidade in usuarioFuncionalidades)
        {
            usuarioFuncionalidade.Status = funcionalidadesAtivasIds.Contains(usuarioFuncionalidade.FuncionalidadeId);
        }

        var novasFuncionalidadesIds = funcionalidadesAtivasIds
            .Except(usuarioFuncionalidades.Select(x => x.FuncionalidadeId))
            .ToArray();

        if (novasFuncionalidadesIds.Length > 0)
        {
            dbContext.UsuariosFuncionalidades.AddRange(novasFuncionalidadesIds.Select(funcionalidadeId => new UsuarioFuncionalidade
            {
                UsuarioId = usuarioId,
                UsuarioCadastroId = usuarioCadastroId,
                FuncionalidadeId = funcionalidadeId,
                Status = true
            }));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default) =>
        Task.FromResult(SenhaHasher.Verificar(senha, usuario.SenhaHash));

    public async Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default)
    {
        usuario.SenhaHash = SenhaHasher.Hash(novaSenha);
        dbContext.Usuarios.Update(usuario);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ConcederPermissoesPadraoAsync(int usuarioId, int usuarioCadastroId, CancellationToken cancellationToken)
    {
        var isAdministradorPadrao = usuarioId == 1;

        var modulosAtivosIds = isAdministradorPadrao
            ? await dbContext.Modulos
                .AsNoTracking()
                .Where(x => x.Status)
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken)
            : await dbContext.Modulos
                .AsNoTracking()
                .Where(x => x.Status && x.Nome == "Geral")
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);

        if (modulosAtivosIds.Length == 0)
        {
            return;
        }

        dbContext.UsuariosModulos.AddRange(modulosAtivosIds.Select(moduloId => new UsuarioModulo
        {
            UsuarioId = usuarioId,
            UsuarioCadastroId = usuarioCadastroId,
            ModuloId = moduloId,
            Status = true
        }));

        var telasAtivasIds = await dbContext.Telas
            .AsNoTracking()
            .Where(x => x.Status && modulosAtivosIds.Contains(x.ModuloId))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (telasAtivasIds.Length > 0)
        {
            dbContext.UsuariosTelas.AddRange(telasAtivasIds.Select(telaId => new UsuarioTela
            {
                UsuarioId = usuarioId,
                UsuarioCadastroId = usuarioCadastroId,
                TelaId = telaId,
                Status = true
            }));
        }

        var funcionalidadesAtivasIds = await dbContext.Funcionalidades
            .AsNoTracking()
            .Where(x => x.Status && telasAtivasIds.Contains(x.TelaId))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (funcionalidadesAtivasIds.Length > 0)
        {
            dbContext.UsuariosFuncionalidades.AddRange(funcionalidadesAtivasIds.Select(funcionalidadeId => new UsuarioFuncionalidade
            {
                UsuarioId = usuarioId,
                UsuarioCadastroId = usuarioCadastroId,
                FuncionalidadeId = funcionalidadeId,
                Status = true
            }));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
