using System.Text.RegularExpressions;
using Core.Application.DTOs.Administracao;
using Core.Domain.Entities;
using Core.Domain.Entities.Administracao;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;

namespace Core.Application.Services.Administracao;

public sealed class AutenticacaoService(
    IAutenticacaoRepository autenticacaoRepository,
    ITentativaLoginRepository tentativaLoginRepository,
    ITokenService tokenService)
{
    private const int MaxTentativas = 5;
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    public async Task<AutenticacaoSuccessResponse> EntrarAsync(EntrarRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var senha = request.Senha ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("email_obrigatorio");
        if (string.IsNullOrWhiteSpace(senha)) throw new DomainException("senha_obrigatoria");
        if (!Regex.IsMatch(email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")) throw new DomainException("email_invalido");

        var usuarioPrimeiroAcesso = await autenticacaoRepository.ObterUsuarioAtivoPorEmailAsync(email, cancellationToken);
        if (usuarioPrimeiroAcesso is not null && usuarioPrimeiroAcesso.PrimeiroAcesso)
            throw new DomainException("primeiro_acesso_requer_criacao_senha");

        var tentativas = await tentativaLoginRepository.ObterAsync(email, cancellationToken);
        if (tentativas is not null && tentativas.TentativasInvalidas >= MaxTentativas) throw new DomainException("login_bloqueado");

        var usuario = await autenticacaoRepository.ObterUsuarioAtivoPorCredenciaisAsync(email, senha, cancellationToken);
        if (usuario is null)
        {
            var total = await tentativaLoginRepository.IncrementarAsync(email, cancellationToken);
            throw new DomainException(total >= MaxTentativas ? "login_bloqueado" : "credenciais_invalidas");
        }

        await tentativaLoginRepository.ZerarAsync(email, cancellationToken);
        return await GerarAutenticacaoAsync(usuario, cancellationToken);
    }

    public async Task<string> CriarPrimeiraSenhaAsync(CriarPrimeiraSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var senha = request.Senha ?? string.Empty;
        var confirmarSenha = request.ConfirmarSenha ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("email_obrigatorio");
        if (!Regex.IsMatch(email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")) throw new DomainException("email_invalido");
        if (string.IsNullOrWhiteSpace(senha)) throw new DomainException("senha_obrigatoria");
        if (senha.Length < 10) throw new DomainException("senha_fraca");
        if (!string.Equals(senha, confirmarSenha, StringComparison.Ordinal)) throw new DomainException("confirmacao_senha_diferente");

        var usuario = await autenticacaoRepository.ObterUsuarioAtivoPorEmailAsync(email, cancellationToken)
            ?? throw new DomainException("usuario_inativo_ou_nao_encontrado");

        if (!usuario.PrimeiroAcesso) throw new DomainException("primeira_senha_ja_definida");

        await autenticacaoRepository.DefinirPrimeiraSenhaAsync(usuario, senha, cancellationToken);
        return "Senha criada com sucesso.";
    }

    public async Task<AutenticacaoSuccessResponse> RenovarTokenAsync(RenovarTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = request.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("refresh_token_obrigatorio");

        var refreshToken = await autenticacaoRepository.ObterRefreshTokenValidoAsync(token, cancellationToken);
        if (refreshToken is null) throw new DomainException("refresh_token_invalido");

        var usuario = await autenticacaoRepository.ObterUsuarioAtivoPorIdAsync(refreshToken.UsuarioId, cancellationToken)
            ?? throw new DomainException("usuario_inativo_ou_nao_encontrado");

        refreshToken.RevogadoEmUtc = DateTime.UtcNow;
        await autenticacaoRepository.RevogarRefreshTokenAsync(refreshToken, cancellationToken);

        return await GerarAutenticacaoAsync(usuario, cancellationToken);
    }

    public async Task<string> EsqueciSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("email_obrigatorio");
        if (!Regex.IsMatch(email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")) throw new DomainException("email_invalido");

        await tentativaLoginRepository.ZerarAsync(email, cancellationToken);
        return "Se o email estiver cadastrado, as instrucoes de recuperacao serao enviadas.";
    }

    private async Task<AutenticacaoSuccessResponse> GerarAutenticacaoAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var expiracao = agora.AddHours(8);
        var accessToken = tokenService.GerarAccessToken(usuario, expiracao);
        var refreshTokenValor = $"refresh-{Guid.NewGuid():N}";
        var modulos = await autenticacaoRepository.ListarModulosAsync(cancellationToken);
        var telas = await autenticacaoRepository.ListarTelasAsync(cancellationToken);
        var funcionalidades = await autenticacaoRepository.ListarFuncionalidadesAsync(cancellationToken);

        await autenticacaoRepository.SalvarRefreshTokenAsync(new RefreshToken
        {
            UsuarioId = usuario.Id,
            UsuarioCadastroId = usuario.Id,
            Token = refreshTokenValor,
            ExpiraEmUtc = agora.Add(RefreshTokenTtl)
        }, cancellationToken);

        return new AutenticacaoSuccessResponse(
            accessToken,
            refreshTokenValor,
            expiracao,
            new UsuarioAutenticadoResponse(
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Ativo,
                new PerfilUsuarioResponse(usuario.PerfilId, ObterNomePerfil(usuario.PerfilId)),
                MapearModulos(usuario, modulos, telas, funcionalidades)));
    }

    private static string ObterNomePerfil(int perfilId) =>
        perfilId switch
        {
            1 => "Administrador",
            2 => "Usuario",
            _ => "Perfil"
        };

    private static IReadOnlyCollection<ModuloUsuarioResponse> MapearModulos(
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

        return modulos
            .OrderBy(x => x.Id)
            .Select(modulo => new ModuloUsuarioResponse(
                modulo.Id,
                modulo.Nome,
                modulo.Status && modulosUsuario.TryGetValue(modulo.Id, out var moduloAtivo) && moduloAtivo ? 1 : 0,
                telasPorModulo.TryGetValue(modulo.Id, out var telasModulo)
                    ? telasModulo
                        .Select(tela => new TelaUsuarioResponse(
                            tela.Id,
                            tela.Nome,
                            tela.Status && telasUsuario.TryGetValue(tela.Id, out var telaAtiva) && telaAtiva ? 1 : 0,
                            funcionalidadesPorTela.TryGetValue(tela.Id, out var funcionalidadesTela)
                                ? funcionalidadesTela
                                    .Select(funcionalidade => new FuncionalidadeUsuarioResponse(
                                        funcionalidade.Id,
                                        funcionalidade.Nome,
                                        funcionalidade.Status && funcionalidadesUsuario.TryGetValue(funcionalidade.Id, out var funcionalidadeAtiva) && funcionalidadeAtiva ? 1 : 0))
                                    .ToArray()
                                : []))
                        .ToArray()
                    : []))
            .ToArray();
    }
}
