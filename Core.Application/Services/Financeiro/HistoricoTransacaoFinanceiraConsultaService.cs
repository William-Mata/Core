using Core.Application.DTOs.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class HistoricoTransacaoFinanceiraConsultaService(
    IHistoricoTransacaoFinanceiraRepository repository,
    IDespesaRepository despesaRepository,
    IReceitaRepository receitaRepository,
    IContaBancariaRepository contaBancariaRepository,
    ICartaoRepository cartaoRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider)
{
    public async Task<IReadOnlyCollection<HistoricoTransacaoFinanceiraListaDto>> ListarAsync(
        ListarHistoricoTransacaoFinanceiraRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantidadeRegistros <= 0)
            throw new DomainException("quantidade_registros_invalida");

        if (!Enum.IsDefined(request.OrdemRegistros))
            throw new DomainException("ordem_registros_invalida");

        var usuarioAutenticadoId = ObterUsuarioAutenticadoId();
        var historicos = await repository.ListarPorUsuarioAsync(
            usuarioAutenticadoId,
            request.QuantidadeRegistros,
            request.OrdemRegistros,
            cancellationToken);

        if (historicos.Count == 0)
            return [];

        var contasById = await ObterContasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var cartoesById = await ObterCartoesPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var despesasById = await ObterDespesasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);
        var receitasById = await ObterReceitasPorIdAsync(historicos, usuarioAutenticadoId, cancellationToken);

        return historicos
            .Select(historico => Map(
                historico,
                contasById.GetValueOrDefault(historico.ContaBancariaId ?? 0),
                cartoesById.GetValueOrDefault(historico.CartaoId ?? 0),
                despesasById.GetValueOrDefault(historico.TransacaoId),
                receitasById.GetValueOrDefault(historico.TransacaoId)))
            .ToArray();
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");

    private async Task<Dictionary<long, string>> ObterContasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var contaIds = historicos
            .Where(x => x.ContaBancariaId.HasValue)
            .Select(x => x.ContaBancariaId!.Value)
            .Distinct()
            .ToArray();

        var contas = await Task.WhenAll(contaIds.Select(id => contaBancariaRepository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken)));

        return contas
            .Where(x => x is not null)
            .ToDictionary(x => x!.Id, x => x!.Descricao);
    }

    private async Task<Dictionary<long, string>> ObterCartoesPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var cartaoIds = historicos
            .Where(x => x.CartaoId.HasValue)
            .Select(x => x.CartaoId!.Value)
            .Distinct()
            .ToArray();

        var cartoes = await Task.WhenAll(cartaoIds.Select(id => cartaoRepository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken)));

        return cartoes
            .Where(x => x is not null)
            .ToDictionary(x => x!.Id, x => x!.Descricao);
    }

    private async Task<Dictionary<long, Despesa>> ObterDespesasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var despesaIds = historicos
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Despesa)
            .Select(x => x.TransacaoId)
            .Distinct()
            .ToArray();

        if (despesaIds.Length == 0)
            return [];

        var despesas = await despesaRepository.ObterPorIdsAsync(despesaIds, usuarioAutenticadoId, cancellationToken);
        return despesas.ToDictionary(x => x.Id, x => x);
    }

    private async Task<Dictionary<long, Receita>> ObterReceitasPorIdAsync(
        IReadOnlyCollection<HistoricoTransacaoFinanceira> historicos,
        int usuarioAutenticadoId,
        CancellationToken cancellationToken)
    {
        var receitaIds = historicos
            .Where(x => x.TipoTransacao == TipoTransacaoFinanceira.Receita)
            .Select(x => x.TransacaoId)
            .Distinct()
            .ToArray();

        var receitas = await Task.WhenAll(receitaIds.Select(id => receitaRepository.ObterPorIdAsync(id, usuarioAutenticadoId, cancellationToken)));

        return receitas
            .Where(x => x is not null)
            .ToDictionary(x => x!.Id, x => x!);
    }

    private static HistoricoTransacaoFinanceiraListaDto Map(
        HistoricoTransacaoFinanceira historico,
        string? contaBancaria,
        string? cartao,
        Despesa? despesa,
        Receita? receita)
    {
        var idOrigem = historico.TipoTransacao.ToString().ToLowerInvariant();
        var tipoTransacao = historico.TipoOperacao == TipoOperacaoTransacaoFinanceira.Estorno
            ? "estorno"
            : idOrigem;

        return new HistoricoTransacaoFinanceiraListaDto(
            idOrigem,
            tipoTransacao,
            historico.ValorTransacao,
            historico.Descricao,
            historico.DataTransacao,
            historico.TipoPagamento?.ToString() ?? historico.TipoRecebimento?.ToString(),
            contaBancaria,
            cartao,
            despesa?.TipoDespesa.ToString(),
            receita?.TipoReceita.ToString());
    }
}
