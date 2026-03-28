using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;

namespace Core.Application.Services;

public sealed class UsuarioService(IUsuarioRepository repository, IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<ListarUsuariosResponse> ListarAsync(ListarUsuariosRequest request, CancellationToken cancellationToken = default)
    {
        var usuarios = await repository.ListarAsync(request.Id, request.Descricao, request.DataInicio, request.DataFim, cancellationToken);
        var dados = usuarios.Select(Map).ToArray();
        return new ListarUsuariosResponse(true, dados, dados.Length);
    }

    public async Task<ObterUsuarioResponse> ObterAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("usuario_nao_encontrado");
        var modulos = await repository.ListarModulosAsync(cancellationToken);
        var telas = await repository.ListarTelasAsync(cancellationToken);
        var funcionalidades = await repository.ListarFuncionalidadesAsync(cancellationToken);

        return new ObterUsuarioResponse(true, MapDetalhe(usuario, modulos, telas, funcionalidades));
    }

    public async Task<CriarUsuarioResponse> CriarAsync(SalvarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var nome = ValidarNome(request.Nome);
        var email = ValidarEmail(request.Email);
        var perfilId = MapearPerfilId(request.Perfil);

        var usuarioExistente = await repository.ObterPorEmailAsync(email, cancellationToken);
        if (usuarioExistente is not null) throw new DomainException("email_em_uso");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            PerfilId = perfilId,
            PrimeiroAcesso = true,
            SenhaHash = string.Empty,
            Ativo = request.Status ?? true,
            UsuarioCadastroId = usuarioAutenticadoId
        };

        var criado = await repository.CriarAsync(usuario, cancellationToken);

        if (request.ModulosAtivos is not null)
        {
            var permissoes = await MapearPermissoesAtivasAsync(request.ModulosAtivos, cancellationToken);
            await repository.SincronizarPermissoesAsync(
                criado.Id,
                usuarioAutenticadoId,
                permissoes.ModulosAtivosIds,
                permissoes.TelasAtivasIds,
                permissoes.FuncionalidadesAtivasIds,
                cancellationToken);
        }

        return new CriarUsuarioResponse(true, "Usuario criado com sucesso", Map(criado));
    }

    public async Task<ResultadoUsuarioResponse> AtualizarAsync(int id, SalvarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("usuario_nao_encontrado");
        var nome = ValidarNome(request.Nome);
        var email = ValidarEmail(request.Email);
        var perfilId = MapearPerfilId(request.Perfil);

        var usuarioExistente = await repository.ObterPorEmailAsync(email, cancellationToken);
        if (usuarioExistente is not null && usuarioExistente.Id != id) throw new DomainException("email_em_uso");

        usuario.Nome = nome;
        usuario.Email = email;
        usuario.PerfilId = perfilId;
        usuario.Ativo = request.Status ?? usuario.Ativo;

        await repository.AtualizarAsync(usuario, cancellationToken);

        if (request.ModulosAtivos is not null)
        {
            var permissoes = await MapearPermissoesAtivasAsync(request.ModulosAtivos, cancellationToken);
            await repository.SincronizarPermissoesAsync(
                id,
                usuarioAutenticadoId,
                permissoes.ModulosAtivosIds,
                permissoes.TelasAtivasIds,
                permissoes.FuncionalidadesAtivasIds,
                cancellationToken);
        }

        return new ResultadoUsuarioResponse(true, "Usuario atualizado com sucesso");
    }

    public async Task<ResultadoUsuarioResponse> ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var usuario = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("usuario_nao_encontrado");

        if (usuario.Id == usuarioAutenticadoId) throw new DomainException("usuario_admin_nao_pode_excluir_a_si_mesmo");

        usuario.Ativo = false;
        await repository.AtualizarAsync(usuario, cancellationToken);
        return new ResultadoUsuarioResponse(true, "Usuario removido com sucesso");
    }

    public async Task<string> AlterarSenhaAsync(AlterarSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var senhaAtual = request.SenhaAtual ?? string.Empty;
        var novaSenha = request.NovaSenha ?? string.Empty;
        var confirmarSenha = request.ConfirmarSenha ?? string.Empty;

        if (string.IsNullOrWhiteSpace(senhaAtual)) throw new DomainException("senha_atual_obrigatoria");
        if (string.IsNullOrWhiteSpace(novaSenha)) throw new DomainException("nova_senha_obrigatoria");
        if (novaSenha.Length < 10) throw new DomainException("senha_fraca");
        if(novaSenha == senhaAtual) throw new DomainException("nova_senha_igual_senha_atual");
        if (!string.Equals(novaSenha, confirmarSenha, StringComparison.Ordinal)) throw new DomainException("confirmacao_senha_diferente");

        var usuarioId = ObterUsuarioAutenticadoId();
        var usuario = await repository.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("usuario_inativo_ou_nao_encontrado");

        var senhaAtualValida = await repository.ValidarSenhaAsync(usuario, senhaAtual, cancellationToken);
        if (!senhaAtualValida) throw new DomainException("senha_atual_incorreta");

        await repository.AlterarSenhaAsync(usuario, novaSenha, cancellationToken);
        return "Senha alterada com sucesso.";
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static string ValidarNome(string? nome)
    {
        var valor = nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor)) throw new DomainException("nome_obrigatorio");
        return valor;
    }

    private static string ValidarEmail(string? email)
    {
        var valor = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor)) throw new DomainException("email_obrigatorio");
        if (!Regex.IsMatch(valor, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")) throw new DomainException("email_invalido");
        return valor;
    }

    private static int? ConverterId(string? valor) =>
        int.TryParse(valor?.Trim(), out var id) ? id : null;

    private async Task<PermissoesAtivas> MapearPermissoesAtivasAsync(
        IReadOnlyCollection<SalvarModuloUsuarioRequest> modulosAtivos,
        CancellationToken cancellationToken)
    {
        var telasDisponiveis = await repository.ListarTelasAsync(cancellationToken);
        var funcionalidadesDisponiveis = await repository.ListarFuncionalidadesAsync(cancellationToken);

        var modulosAtivosIds = modulosAtivos
            .Where(x => x.Status)
            .Select(x => ConverterId(x.Id))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        var telasAtivasIds = modulosAtivos
            .Where(x => x.Status)
            .SelectMany(modulo => (modulo.Telas ?? [])
                .Where(tela => tela.Status)
                .Select(tela => ResolverTelaId(modulo, tela, telasDisponiveis)))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        var funcionalidadesAtivasIds = modulosAtivos
            .Where(x => x.Status)
            .SelectMany(modulo => (modulo.Telas ?? [])
                .Where(tela => tela.Status)
                .SelectMany(tela =>
                {
                    var telaId = ResolverTelaId(modulo, tela, telasDisponiveis);
                    if (!telaId.HasValue)
                    {
                        return Array.Empty<int>();
                    }

                    return (tela.Funcionalidades ?? [])
                        .Where(funcionalidade => funcionalidade.Status)
                        .Select(funcionalidade => ResolverFuncionalidadeId(telaId.Value, funcionalidade, funcionalidadesDisponiveis))
                        .Where(funcionalidadeId => funcionalidadeId.HasValue)
                        .Select(funcionalidadeId => funcionalidadeId!.Value);
                }))
            .Distinct()
            .ToArray();

        return new PermissoesAtivas(modulosAtivosIds, telasAtivasIds, funcionalidadesAtivasIds);
    }

    private static int? ResolverTelaId(
        SalvarModuloUsuarioRequest modulo,
        SalvarTelaUsuarioRequest tela,
        IReadOnlyCollection<Tela> telasDisponiveis)
    {
        var telaId = ConverterId(tela.Id);
        var moduloId = ConverterId(modulo.Id);

        if (telaId.HasValue)
        {
            var telaEncontrada = telasDisponiveis.FirstOrDefault(x => x.Id == telaId.Value);
            if (telaEncontrada is not null && (!moduloId.HasValue || telaEncontrada.ModuloId == moduloId.Value))
            {
                return telaEncontrada.Id;
            }

            if (!telasDisponiveis.Any())
            {
                return telaId.Value;
            }
        }

        return telasDisponiveis
            .FirstOrDefault(x =>
                (!moduloId.HasValue || x.ModuloId == moduloId.Value) &&
                NormalizarChavePermissao(x.Nome) == NormalizarChavePermissao(tela.Nome))
            ?.Id;
    }

    private static int? ResolverFuncionalidadeId(
        int telaId,
        SalvarFuncionalidadeUsuarioRequest funcionalidade,
        IReadOnlyCollection<Funcionalidade> funcionalidadesDisponiveis)
    {
        var funcionalidadeId = ConverterId(funcionalidade.Id);

        if (funcionalidadeId.HasValue)
        {
            var funcionalidadeEncontrada = funcionalidadesDisponiveis.FirstOrDefault(x =>
                x.Id == funcionalidadeId.Value &&
                x.TelaId == telaId);

            if (funcionalidadeEncontrada is not null)
            {
                return funcionalidadeEncontrada.Id;
            }

            if (!funcionalidadesDisponiveis.Any(x => x.TelaId == telaId))
            {
                return funcionalidadeId.Value;
            }
        }

        var chaveFuncionalidade = NormalizarChavePermissao(funcionalidade.Nome);

        return funcionalidadesDisponiveis
            .FirstOrDefault(x =>
                x.TelaId == telaId &&
                NormalizarChavePermissao(x.Nome) == chaveFuncionalidade)
            ?.Id;
    }

    private static string NormalizarChavePermissao(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var texto = valor.Trim();
        texto = texto.Replace("comum.acoes.", string.Empty, StringComparison.OrdinalIgnoreCase);
        texto = texto.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(texto.Length);
        foreach (var caractere in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caractere))
            {
                builder.Append(char.ToLowerInvariant(caractere));
            }
        }

        return builder.ToString();
    }

    private static int MapearPerfilId(string? perfil) =>
        (perfil?.Trim().ToUpperInvariant()) switch
        {
            "ADMIN" => 1,
            "USER" => 2,
            _ => throw new DomainException("perfil_invalido")
        };

    private static string MapearPerfil(int perfilId) =>
        perfilId switch
        {
            1 => "ADMIN",
            2 => "USER",
            _ => "USER"
        };

    private static UsuarioDto Map(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, MapearPerfil(usuario.PerfilId), usuario.DataHoraCadastro);

    private static UsuarioDetalheDto MapDetalhe(
        Usuario usuario,
        IReadOnlyCollection<Modulo> modulos,
        IReadOnlyCollection<Tela> telas,
        IReadOnlyCollection<Funcionalidade> funcionalidades)
    {
        var modulosUsuario = usuario.Modulos.ToDictionary(x => x.ModuloId, x => x.Status);
        var telasUsuario = usuario.Telas.ToDictionary(x => x.TelaId, x => x.Status);
        var funcionalidadesUsuario = usuario.Funcionalidades.ToDictionary(x => x.FuncionalidadeId, x => x.Status);

        var telasPorModulo = telas
            .GroupBy(x => x.ModuloId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Tela>)x.OrderBy(t => t.Id).ToArray());

        var funcionalidadesPorTela = funcionalidades
            .GroupBy(x => x.TelaId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Funcionalidade>)x.OrderBy(f => f.Id).ToArray());

        var modulosAtivos = modulos
            .OrderBy(x => x.Id)
            .Select(modulo => new ModuloUsuarioDto(
                modulo.Id.ToString(),
                modulo.Nome,
                modulo.Status && modulosUsuario.TryGetValue(modulo.Id, out var moduloAtivo) && moduloAtivo,
                telasPorModulo.TryGetValue(modulo.Id, out var telasModulo)
                    ? telasModulo
                        .Select(tela => new TelaUsuarioDto(
                            tela.Id.ToString(),
                            tela.Nome,
                            tela.Status && telasUsuario.TryGetValue(tela.Id, out var telaAtiva) && telaAtiva,
                            funcionalidadesPorTela.TryGetValue(tela.Id, out var funcionalidadesTela)
                                ? funcionalidadesTela
                                    .Select(funcionalidade => new FuncionalidadeUsuarioDto(
                                        funcionalidade.Id.ToString(),
                                        funcionalidade.Nome,
                                        funcionalidade.Status && funcionalidadesUsuario.TryGetValue(funcionalidade.Id, out var funcionalidadeAtiva) && funcionalidadeAtiva))
                                    .ToArray()
                                : []))
                        .ToArray()
                    : []))
            .ToArray();

        return new UsuarioDetalheDto(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            MapearPerfil(usuario.PerfilId),
            usuario.Ativo,
            usuario.DataHoraCadastro,
            modulosAtivos);
    }

    private sealed record PermissoesAtivas(
        IReadOnlyCollection<int> ModulosAtivosIds,
        IReadOnlyCollection<int> TelasAtivasIds,
        IReadOnlyCollection<int> FuncionalidadesAtivasIds);
}
