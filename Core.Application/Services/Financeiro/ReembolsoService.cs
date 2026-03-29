using System.Text.Json;
using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class ReembolsoService(
    IReembolsoRepository repository,
    IDespesaRepository despesaRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    HistoricoTransacaoFinanceiraService historicoTransacaoFinanceiraService)
{
    public async Task<IReadOnlyCollection<ReembolsoDto>> ListarAsync(ListarReembolsosRequest request, CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(request.Id, request.Descricao, request.DataInicio, request.DataFim, cancellationToken))
            .Select(Map)
            .ToArray();

    public async Task<ReembolsoDto> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        Map(await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("reembolso_nao_encontrado"));

    public async Task<ReembolsoDto> CriarAsync(SalvarReembolsoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        ValidarContaOuCartao(request.ContaBancariaId, request.CartaoId);
        var descricao = ValidarDescricao(request.Descricao);
        var solicitante = ValidarSolicitante(request.Solicitante);
        var despesasIds = ExtrairDespesasIds(request.DespesasVinculadas);
        var despesas = await ObterDespesasValidasAsync(despesasIds, cancellationToken);
        var status = NormalizarStatus(request.Status);
        ValidarPeriodoEfetivacao(request.DataLancamento, request.DataEfetivacao, status);

        await ValidarDespesasVinculadasAsync(despesasIds, null, cancellationToken);

        var reembolso = new Reembolso
        {
            Descricao = descricao,
            Solicitante = solicitante,
            DataLancamento = request.DataLancamento,
            DataEfetivacao = request.DataEfetivacao,
            ValorTotal = despesas.Sum(x => x.ValorTotal),
            Status = status,
            UsuarioCadastroId = usuarioAutenticadoId,
            Despesas = despesasIds
                .Select(id => new ReembolsoDespesa
                {
                    UsuarioCadastroId = usuarioAutenticadoId,
                    DespesaId = id
                })
                .ToList()
        };

        return Map(await repository.CriarAsync(reembolso, cancellationToken));
    }

    public async Task<ReembolsoDto> AtualizarAsync(long id, SalvarReembolsoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        ValidarContaOuCartao(request.ContaBancariaId, request.CartaoId);
        var reembolso = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("reembolso_nao_encontrado");
        var statusAnterior = reembolso.Status;
        var descricao = ValidarDescricao(request.Descricao);
        var solicitante = ValidarSolicitante(request.Solicitante);
        var despesasIds = ExtrairDespesasIds(request.DespesasVinculadas);
        var despesas = await ObterDespesasValidasAsync(despesasIds, cancellationToken);
        var status = NormalizarStatus(request.Status);
        ValidarPeriodoEfetivacao(request.DataLancamento, request.DataEfetivacao, status);

        await ValidarDespesasVinculadasAsync(despesasIds, id, cancellationToken);

        reembolso.Descricao = descricao;
        reembolso.Solicitante = solicitante;
        reembolso.DataLancamento = request.DataLancamento;
        reembolso.DataEfetivacao = request.DataEfetivacao;
        reembolso.ValorTotal = despesas.Sum(x => x.ValorTotal);
        reembolso.Status = status;
        reembolso.Despesas = despesasIds
            .Select(despesaId => new ReembolsoDespesa
            {
                ReembolsoId = reembolso.Id,
                UsuarioCadastroId = usuarioAutenticadoId,
                DespesaId = despesaId
            })
            .ToList();

        if (statusAnterior != StatusReembolso.Pago && reembolso.Status == StatusReembolso.Pago)
        {
            ValidarDestinoParaPagamento(request.ContaBancariaId, request.CartaoId);
        }

        var reembolsoAtualizado = await repository.AtualizarAsync(reembolso, cancellationToken);

        if (statusAnterior != StatusReembolso.Pago && reembolsoAtualizado.Status == StatusReembolso.Pago)
        {
            await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
                TipoTransacaoFinanceira.Reembolso,
                reembolsoAtualizado.Id,
                usuarioAutenticadoId,
                reembolsoAtualizado.DataEfetivacao ?? DateOnly.FromDateTime(DateTime.UtcNow),
                0m,
                reembolsoAtualizado.ValorTotal,
                reembolsoAtualizado.ValorTotal,
                "Efetivacao de reembolso",
                "reembolso",
                request.ContaBancariaId,
                request.CartaoId,
                cancellationToken);
        }
        else if (statusAnterior == StatusReembolso.Pago && reembolsoAtualizado.Status != StatusReembolso.Pago)
        {
            await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
                TipoTransacaoFinanceira.Reembolso,
                reembolsoAtualizado.Id,
                usuarioAutenticadoId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                reembolsoAtualizado.ValorTotal,
                reembolsoAtualizado.ValorTotal,
                0m,
                "Estorno de reembolso",
                "reembolso",
                request.ContaBancariaId,
                request.CartaoId,
                cancellationToken);
        }

        return Map(reembolsoAtualizado);
    }

    public async Task ExcluirAsync(long id, CancellationToken cancellationToken = default)
    {
        var reembolso = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("reembolso_nao_encontrado");
        await repository.ExcluirAsync(reembolso, cancellationToken);
    }

    public async Task<ReembolsoDto> EfetivarAsync(long id, EfetivarReembolsoRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        ValidarContaOuCartao(request.ContaBancariaId, request.CartaoId);
        ValidarDestinoParaPagamento(request.ContaBancariaId, request.CartaoId);

        var reembolso = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("reembolso_nao_encontrado");
        if (reembolso.Status == StatusReembolso.Pago) throw new DomainException("status_invalido");
        if (request.DataEfetivacao < reembolso.DataLancamento) throw new DomainException("periodo_invalido");

        reembolso.Status = StatusReembolso.Pago;
        reembolso.DataEfetivacao = request.DataEfetivacao;

        var reembolsoAtualizado = await repository.AtualizarAsync(reembolso, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEfetivacaoAsync(
            TipoTransacaoFinanceira.Reembolso,
            reembolsoAtualizado.Id,
            usuarioAutenticadoId,
            request.DataEfetivacao,
            0m,
            reembolsoAtualizado.ValorTotal,
            reembolsoAtualizado.ValorTotal,
            "Efetivacao de reembolso",
            "reembolso",
            request.ContaBancariaId,
            request.CartaoId,
            cancellationToken);

        return Map(reembolsoAtualizado);
    }

    public async Task<ReembolsoDto> EstornarAsync(long id, CancellationToken cancellationToken = default)
    {
        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var reembolso = await repository.ObterPorIdAsync(id, cancellationToken) ?? throw new NotFoundException("reembolso_nao_encontrado");
        if (reembolso.Status != StatusReembolso.Pago) throw new DomainException("status_invalido");

        var valorAntesTransacao = reembolso.ValorTotal;
        reembolso.Status = StatusReembolso.Aguardando;
        reembolso.DataEfetivacao = null;

        var reembolsoAtualizado = await repository.AtualizarAsync(reembolso, cancellationToken);
        await historicoTransacaoFinanceiraService.RegistrarEstornoAsync(
            TipoTransacaoFinanceira.Reembolso,
            reembolsoAtualizado.Id,
            usuarioAutenticadoId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            valorAntesTransacao,
            valorAntesTransacao,
            0m,
            "Estorno de reembolso",
            "reembolso",
            cancellationToken: cancellationToken);

        return Map(reembolsoAtualizado);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private static string ValidarDescricao(string? descricao)
    {
        var valor = descricao?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("descricao_obrigatoria");
        }

        return valor;
    }

    private static string ValidarSolicitante(string? solicitante)
    {
        var valor = solicitante?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("solicitante_obrigatorio");
        }

        return valor;
    }

    private static IReadOnlyCollection<long> ExtrairDespesasIds(IReadOnlyCollection<JsonElement>? despesasVinculadas)
    {
        if (despesasVinculadas is null || despesasVinculadas.Count == 0)
        {
            throw new DomainException("despesas_vinculadas_obrigatorias");
        }

        var ids = new List<long>();

        foreach (var item in despesasVinculadas)
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var idNumerico))
            {
                ids.Add(idNumerico);
                continue;
            }

            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("id", out var propriedadeId) &&
                propriedadeId.TryGetInt64(out var idObjeto))
            {
                ids.Add(idObjeto);
                continue;
            }

            throw new DomainException("despesa_vinculada_invalida");
        }

        var idsValidos = ids.Where(x => x > 0).Distinct().ToArray();
        if (idsValidos.Length == 0)
        {
            throw new DomainException("despesas_vinculadas_obrigatorias");
        }

        return idsValidos;
    }

    private async Task<IReadOnlyCollection<Despesa>> ObterDespesasValidasAsync(IReadOnlyCollection<long> despesasIds, CancellationToken cancellationToken)
    {
        var despesas = await despesaRepository.ObterPorIdsAsync(despesasIds, cancellationToken);
        if (despesas.Count != despesasIds.Count)
        {
            throw new DomainException("despesa_nao_encontrada");
        }

        return despesas;
    }

    private async Task ValidarDespesasVinculadasAsync(IReadOnlyCollection<long> despesasIds, long? reembolsoIgnoradoId, CancellationToken cancellationToken)
    {
        var existeConflito = await repository.ExisteDespesaVinculadaEmOutroReembolsoAsync(despesasIds, reembolsoIgnoradoId, cancellationToken);
        if (existeConflito)
        {
            throw new DomainException("despesa_vinculada_outro_reembolso");
        }
    }

    private static StatusReembolso NormalizarStatus(string? status) =>
        (status?.Trim().ToUpperInvariant()) switch
        {
            null or "" => StatusReembolso.Aguardando,
            "AGUARDANDO" => StatusReembolso.Aguardando,
            "APROVADO" => StatusReembolso.Aprovado,
            "PAGO" => StatusReembolso.Pago,
            "CANCELADO" => StatusReembolso.Cancelado,
            "REJEITADO" => StatusReembolso.Rejeitado,
            _ => throw new DomainException("status_reembolso_invalido")
        };

    private static ReembolsoDto Map(Reembolso reembolso) =>
        new(
            reembolso.Id,
            reembolso.Descricao,
            reembolso.Solicitante,
            reembolso.DataLancamento,
            reembolso.DataEfetivacao,
            reembolso.Despesas.Select(x => x.DespesaId).ToArray(),
            reembolso.ValorTotal,
            reembolso.Status.ToString().ToUpperInvariant());

    private static void ValidarContaOuCartao(long? contaBancariaId, long? cartaoId)
    {
        if (contaBancariaId.HasValue && cartaoId.HasValue)
        {
            throw new DomainException("forma_pagamento_invalida");
        }
    }

    private static void ValidarDestinoParaPagamento(long? contaBancariaId, long? cartaoId)
    {
        if (!contaBancariaId.HasValue && !cartaoId.HasValue)
        {
            throw new DomainException("conta_ou_cartao_obrigatorio");
        }
    }

    private static void ValidarPeriodoEfetivacao(DateOnly dataLancamento, DateOnly? dataEfetivacao, StatusReembolso status)
    {
        if (!dataEfetivacao.HasValue)
        {
            if (status == StatusReembolso.Pago)
            {
                throw new DomainException("data_efetivacao_obrigatoria");
            }

            return;
        }

        if (dataEfetivacao.Value < dataLancamento)
        {
            throw new DomainException("periodo_invalido");
        }
    }
}
