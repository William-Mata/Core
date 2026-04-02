using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class ContaBancariaService(
    IContaBancariaRepository repository,
    IHistoricoTransacaoFinanceiraRepository historicoRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<ContaBancariaDto>> ListarAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(ObterUsuarioAutenticadoId(), cancellationToken)).Select(Map).ToArray();

    public async Task<ContaBancariaDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, ObterUsuarioAutenticadoId(), cancellationToken) ?? throw new NotFoundException("conta_bancaria_nao_encontrada"));

    public async Task<IReadOnlyCollection<LancamentoVinculadoDto>> ListarLancamentosAsync(long id, string? competencia, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var conta = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("conta_bancaria_nao_encontrada");

        return (await historicoRepository.ListarPorContaBancariaCompetenciaAsync(conta.Id, usuarioAutenticadoId, competencia, cancellationToken))
            .Select(MapLancamento)
            .ToArray();
    }

    public async Task<ContaBancariaDto> CriarAsync(CriarContaBancariaRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        if (string.IsNullOrWhiteSpace(request.Descricao) || string.IsNullOrWhiteSpace(request.Banco) || string.IsNullOrWhiteSpace(request.Agencia) || string.IsNullOrWhiteSpace(request.Numero))
            throw new DomainException("campo_obrigatorio");
        if (request.SaldoInicial <= 0) throw new DomainException("saldo_inicial_invalido");

        var conta = new ContaBancaria
        {
            Descricao = request.Descricao.Trim(),
            Banco = request.Banco.Trim(),
            Agencia = request.Agencia.Trim(),
            Numero = request.Numero.Trim(),
            SaldoInicial = request.SaldoInicial,
            SaldoAtual = request.SaldoInicial,
            DataAbertura = request.DataAbertura,
            UsuarioCadastroId = usuarioAutenticadoId,
            Status = StatusContaBancaria.Ativa,
            Logs = [new ContaBancariaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Cadastro, Descricao = "Conta bancaria criada com status ativa." }]
        };

        return Map(await repository.CriarAsync(conta, cancellationToken));
    }

    public async Task<ContaBancariaDto> AtualizarAsync(long id, AtualizarContaBancariaRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("conta_bancaria_nao_encontrada");
        if (string.IsNullOrWhiteSpace(request.Descricao) || string.IsNullOrWhiteSpace(request.Banco) || string.IsNullOrWhiteSpace(request.Agencia) || string.IsNullOrWhiteSpace(request.Numero))
            throw new DomainException("campo_obrigatorio");

        existente.Descricao = request.Descricao.Trim();
        existente.Banco = request.Banco.Trim();
        existente.Agencia = request.Agencia.Trim();
        existente.Numero = request.Numero.Trim();
        existente.DataAbertura = request.DataAbertura;
        existente.Logs.Add(new ContaBancariaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Conta bancaria atualizada." });

        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    public async Task<ContaBancariaDto> InativarAsync(long id, AlternarStatusContaBancariaRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("conta_bancaria_nao_encontrada");
        if (existente.Status != StatusContaBancaria.Ativa) throw new DomainException("status_invalido");
        if (request.QuantidadePendencias > 0) throw new DomainException("conta_com_pendencias");

        existente.Status = StatusContaBancaria.Inativa;
        existente.Logs.Add(new ContaBancariaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Exclusao, Descricao = "Conta bancaria inativada." });
        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    public async Task<ContaBancariaDto> AtivarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var existente = await repository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken) ?? throw new NotFoundException("conta_bancaria_nao_encontrada");
        if (existente.Status != StatusContaBancaria.Inativa) throw new DomainException("status_invalido");

        existente.Status = StatusContaBancaria.Ativa;
        existente.Logs.Add(new ContaBancariaLog { UsuarioCadastroId = usuarioAutenticadoId, Acao = AcaoLogs.Atualizacao, Descricao = "Conta bancaria ativada." });
        return Map(await repository.AtualizarAsync(existente, cancellationToken));
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static ContaBancariaDto Map(ContaBancaria c) =>
        new(c.Id, c.Descricao, c.Banco, c.Agencia, c.Numero, c.SaldoInicial, c.SaldoAtual, c.DataAbertura, c.Status.ToString().ToLowerInvariant(),
            c.Extrato.Select(x => new ContaBancariaExtratoDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Descricao, x.Tipo, x.Valor)).ToArray(),
            c.Logs.Select(x => new ContaBancariaLogDto(x.Id, DateOnly.FromDateTime(x.DataHoraCadastro), x.Acao, x.Descricao)).ToArray());

    private static LancamentoVinculadoDto MapLancamento(HistoricoTransacaoFinanceira historico) =>
        new(
            historico.Id,
            historico.TransacaoId,
            historico.DataTransacao,
            historico.TipoTransacao.ToString().ToLowerInvariant(),
            historico.TipoOperacao.ToString().ToLowerInvariant(),
            historico.Descricao,
            historico.TipoPagamento,
            historico.ValorAntesTransacao,
            historico.ValorTransacao,
            historico.ValorDepoisTransacao);
}
