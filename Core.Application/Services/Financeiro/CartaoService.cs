using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class CartaoService(ICartaoRepository repository, IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<CartaoDto>> ListarAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(ObterUsuarioAutenticadoId(), cancellationToken)).Select(Map).ToArray();

    public async Task<CartaoDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("cartao_nao_encontrado"));

    public async Task<CartaoDto> CriarAsync(CriarCartaoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        Validar(request.Descricao, request.Bandeira, request.Tipo, request.Limite, request.SaldoDisponivel, request.DiaVencimento, request.DataVencimentoCartao);
        var cartao = new Cartao
        {
            Descricao = request.Descricao.Trim(),
            Bandeira = request.Bandeira.Trim(),
            Tipo = request.Tipo,
            Limite = request.Tipo == TipoCartao.Credito ? request.Limite : 0m,
            SaldoDisponivel = request.SaldoDisponivel,
            DiaVencimento = request.Tipo == TipoCartao.Credito ? request.DiaVencimento : null,
            DataVencimentoCartao = request.Tipo == TipoCartao.Credito ? request.DataVencimentoCartao : null,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusCartao.Ativo,
            Logs = [new CartaoLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Cartao criado com status ativo." }]
        };

        return Map(await repository.CriarAsync(cartao, cancellationToken));
    }

    public async Task<CartaoDto> AtualizarAsync(long id, AtualizarCartaoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("cartao_nao_encontrado");
        Validar(request.Descricao, request.Bandeira, request.Tipo, request.Limite, existente.SaldoDisponivel, request.DiaVencimento, request.DataVencimentoCartao);

        existente.Descricao = request.Descricao.Trim();
        existente.Bandeira = request.Bandeira.Trim();
        existente.Tipo = request.Tipo;
        existente.Limite = request.Tipo == TipoCartao.Credito ? request.Limite : 0m;
        existente.DiaVencimento = request.Tipo == TipoCartao.Credito ? request.DiaVencimento : null;
        existente.DataVencimentoCartao = request.Tipo == TipoCartao.Credito ? request.DataVencimentoCartao : null;
        existente.Logs.Add(new CartaoLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Cartao atualizado." });

        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    public async Task<CartaoDto> InativarAsync(long id, AlternarStatusCartaoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("cartao_nao_encontrado");
        if (existente.Status != StatusCartao.Ativo) throw new DomainException("status_invalido");
        if (request.QuantidadePendencias > 0) throw new DomainException("cartao_com_pendencias");

        existente.Status = StatusCartao.Inativo;
        existente.Logs.Add(new CartaoLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Cartao inativado." });
        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    public async Task<CartaoDto> AtivarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("cartao_nao_encontrado");
        if (existente.Status != StatusCartao.Inativo) throw new DomainException("status_invalido");

        existente.Status = StatusCartao.Ativo;
        existente.Logs.Add(new CartaoLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Cartao ativado." });
        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static void Validar(string descricao, string bandeira, TipoCartao tipo, decimal? limite, decimal saldoDisponivel, DateOnly? diaVencimento, DateOnly? dataVencimentoCartao)
    {
        if (string.IsNullOrWhiteSpace(descricao) || string.IsNullOrWhiteSpace(bandeira)) throw new DomainException("campo_obrigatorio");
        if (!Enum.IsDefined(tipo)) throw new DomainException("tipo_invalido");
        if (saldoDisponivel < 0) throw new DomainException("saldo_invalido");
        if (tipo == TipoCartao.Credito && (!limite.HasValue || limite <= 0 || !diaVencimento.HasValue || !dataVencimentoCartao.HasValue)) throw new DomainException("dados_credito_obrigatorios");
    }

    private static CartaoDto Map(Cartao c) =>
        new(c.Id, c.Descricao, c.Bandeira, c.Tipo, c.Limite, c.SaldoDisponivel, c.DiaVencimento, c.DataVencimentoCartao, c.Status.ToString().ToLowerInvariant(),
            c.Lancamentos.Select(x => new CartaoLancamentoDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Descricao, x.Valor)).ToArray(),
            c.Logs.Select(x => new CartaoLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());
}
