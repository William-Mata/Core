using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Financeiro;

public sealed class HistoricoTransacaoFinanceiraService(IHistoricoTransacaoFinanceiraRepository repository)
{
    public Task RegistrarEfetivacaoAsync(
        TipoTransacaoFinanceira tipoTransacao,
        long transacaoId,
        int usuarioOperacaoId,
        DateOnly dataTransacao,
        decimal valorAntesTransacao,
        decimal valorTransacao,
        decimal valorDepoisTransacao,
        string descricao,
        TipoPagamento? tipoPagamento = null,
        long? contaBancariaId = null,
        long? contaDestinoId = null,
        long? cartaoId = null,
        TipoRecebimento? tipoRecebimento = null,
        CancellationToken cancellationToken = default,
        string? observacao = null,
        bool ocultarDoHistorico = false) =>
        RegistrarAsync(
            tipoTransacao,
            transacaoId,
            usuarioOperacaoId,
            dataTransacao,
            valorAntesTransacao,
            valorTransacao,
            valorDepoisTransacao,
            descricao,
            TipoOperacaoTransacaoFinanceira.Efetivacao,
            tipoPagamento,
            tipoRecebimento,
            contaBancariaId,
            contaDestinoId,
            cartaoId,
            observacao,
            ocultarDoHistorico,
            cancellationToken);

    public async Task RegistrarEstornoAsync(
        TipoTransacaoFinanceira tipoTransacao,
        long transacaoId,
        int usuarioOperacaoId,
        DateOnly dataTransacao,
        decimal valorAntesTransacao,
        decimal valorTransacao,
        decimal valorDepoisTransacao,
        string descricao,
        TipoPagamento? tipoPagamento = null,
        long? contaBancariaId = null,
        long? contaDestinoId = null,
        long? cartaoId = null,
        TipoRecebimento? tipoRecebimento = null,
        CancellationToken cancellationToken = default,
        string? observacao = null,
        bool ocultarDoHistorico = false)
    {
        if (!contaBancariaId.HasValue && !cartaoId.HasValue)
        {
            var ultimoHistorico = await repository.ObterUltimoPorTransacaoAsync(tipoTransacao, transacaoId, cancellationToken);
            if (ultimoHistorico is not null)
            {
                contaBancariaId = ultimoHistorico.ContaBancariaId;
                contaDestinoId ??= ultimoHistorico.ContaDestinoId;
                cartaoId = ultimoHistorico.CartaoId;
                tipoPagamento ??= ultimoHistorico.TipoPagamento;
                tipoRecebimento ??= ultimoHistorico.TipoRecebimento;
            }
        }

        if (ocultarDoHistorico)
            await repository.MarcarOcultoPorTransacaoAsync(tipoTransacao, transacaoId, cancellationToken);

        await RegistrarAsync(
            tipoTransacao,
            transacaoId,
            usuarioOperacaoId,
            dataTransacao,
            valorAntesTransacao,
            valorTransacao,
            valorDepoisTransacao,
            descricao,
            TipoOperacaoTransacaoFinanceira.Estorno,
            tipoPagamento,
            tipoRecebimento,
            contaBancariaId,
            contaDestinoId,
            cartaoId,
            observacao,
            ocultarDoHistorico,
            cancellationToken);
    }

    private Task RegistrarAsync(
        TipoTransacaoFinanceira tipoTransacao,
        long transacaoId,
        int usuarioOperacaoId,
        DateOnly dataTransacao,
        decimal valorAntesTransacao,
        decimal valorTransacao,
        decimal valorDepoisTransacao,
        string descricao,
        TipoOperacaoTransacaoFinanceira tipoOperacao,
        TipoPagamento? tipoPagamento,
        TipoRecebimento? tipoRecebimento,
        long? contaBancariaId,
        long? contaDestinoId,
        long? cartaoId,
        string? observacao,
        bool ocultarDoHistorico,
        CancellationToken cancellationToken)
    {
        var historicoOrigem = CriarHistorico(
            usuarioOperacaoId,
            tipoTransacao,
            transacaoId,
            tipoOperacao,
            contaBancariaId,
            contaDestinoId,
            cartaoId,
            dataTransacao,
            descricao,
            observacao,
            ocultarDoHistorico,
            tipoPagamento,
            tipoRecebimento,
            valorAntesTransacao,
            valorTransacao,
            valorDepoisTransacao);

        return RegistrarComEspelhoSeTransferenciaAsync(
            historicoOrigem,
            contaBancariaId,
            contaDestinoId,
            cartaoId,
            cancellationToken);
    }

    private async Task RegistrarComEspelhoSeTransferenciaAsync(
        HistoricoTransacaoFinanceira historicoOrigem,
        long? contaBancariaIdOrigem,
        long? contaDestinoId,
        long? cartaoId,
        CancellationToken cancellationToken)
    {
        await repository.CriarAsync(historicoOrigem, cancellationToken);

        var historicoEspelho = CriarHistoricoEspelhoSeTransferencia(historicoOrigem, contaBancariaIdOrigem, contaDestinoId, cartaoId);
        if (historicoEspelho is null)
            return;

        await repository.CriarAsync(historicoEspelho, cancellationToken);
    }

    private static HistoricoTransacaoFinanceira? CriarHistoricoEspelhoSeTransferencia(
        HistoricoTransacaoFinanceira historicoOrigem,
        long? contaBancariaIdOrigem,
        long? contaDestinoId,
        long? cartaoId)
    {
        if (!contaDestinoId.HasValue || cartaoId.HasValue)
            return null;

        var ehMovimentacaoEntreContas = historicoOrigem.TipoPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix
            || historicoOrigem.TipoRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix;
        if (!ehMovimentacaoEntreContas)
            return null;

        var tipoTransacaoEspelho = historicoOrigem.TipoTransacao switch
        {
            TipoTransacaoFinanceira.Despesa => TipoTransacaoFinanceira.Receita,
            TipoTransacaoFinanceira.Receita => TipoTransacaoFinanceira.Despesa,
            _ => (TipoTransacaoFinanceira?)null
        };

        if (!tipoTransacaoEspelho.HasValue)
            return null;

        var tipoPagamentoEspelho = tipoTransacaoEspelho == TipoTransacaoFinanceira.Despesa
            ? ConverterParaTipoPagamentoEspelho(historicoOrigem.TipoRecebimento, historicoOrigem.TipoPagamento)
            : (TipoPagamento?)null;
        var tipoRecebimentoEspelho = tipoTransacaoEspelho == TipoTransacaoFinanceira.Receita
            ? ConverterParaTipoRecebimentoEspelho(historicoOrigem.TipoPagamento, historicoOrigem.TipoRecebimento)
            : (TipoRecebimento?)null;

        return CriarHistorico(
            historicoOrigem.UsuarioOperacaoId,
            tipoTransacaoEspelho.Value,
            historicoOrigem.TransacaoId,
            historicoOrigem.TipoOperacao,
            contaDestinoId,
            contaBancariaIdOrigem,
            null,
            historicoOrigem.DataTransacao,
            historicoOrigem.Descricao,
            historicoOrigem.Observacao,
            historicoOrigem.OcultarDoHistorico,
            tipoPagamentoEspelho,
            tipoRecebimentoEspelho,
            historicoOrigem.ValorAntesTransacao,
            historicoOrigem.ValorTransacao,
            historicoOrigem.ValorDepoisTransacao);
    }

    private static TipoPagamento? ConverterParaTipoPagamentoEspelho(TipoRecebimento? origemRecebimento, TipoPagamento? origemPagamento)
    {
        if (origemRecebimento.HasValue)
        {
            return origemRecebimento.Value switch
            {
                TipoRecebimento.Transferencia => TipoPagamento.Transferencia,
                TipoRecebimento.Pix => TipoPagamento.Pix,
                _ => null
            };
        }

        return origemPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix
            ? origemPagamento
            : null;
    }

    private static TipoRecebimento? ConverterParaTipoRecebimentoEspelho(TipoPagamento? origemPagamento, TipoRecebimento? origemRecebimento)
    {
        if (origemPagamento.HasValue)
        {
            return origemPagamento.Value switch
            {
                TipoPagamento.Transferencia => TipoRecebimento.Transferencia,
                TipoPagamento.Pix => TipoRecebimento.Pix,
                _ => null
            };
        }

        return origemRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix
            ? origemRecebimento
            : null;
    }

    private static HistoricoTransacaoFinanceira CriarHistorico(
        int usuarioOperacaoId,
        TipoTransacaoFinanceira tipoTransacao,
        long transacaoId,
        TipoOperacaoTransacaoFinanceira tipoOperacao,
        long? contaBancariaId,
        long? contaDestinoId,
        long? cartaoId,
        DateOnly dataTransacao,
        string descricao,
        string? observacao,
        bool ocultarDoHistorico,
        TipoPagamento? tipoPagamento,
        TipoRecebimento? tipoRecebimento,
        decimal valorAntesTransacao,
        decimal valorTransacao,
        decimal valorDepoisTransacao) =>
        new()
        {
            UsuarioOperacaoId = usuarioOperacaoId,
            TipoTransacao = tipoTransacao,
            TransacaoId = transacaoId,
            TipoOperacao = tipoOperacao,
            TipoConta = ResolverTipoConta(contaBancariaId, cartaoId, tipoPagamento, tipoRecebimento),
            ContaBancariaId = contaBancariaId,
            ContaDestinoId = contaDestinoId,
            CartaoId = cartaoId,
            DataTransacao = dataTransacao,
            Descricao = descricao,
            Observacao = observacao,
            OcultarDoHistorico = ocultarDoHistorico,
            TipoPagamento = tipoPagamento,
            TipoRecebimento = tipoRecebimento,
            ValorAntesTransacao = valorAntesTransacao,
            ValorTransacao = valorTransacao,
            ValorDepoisTransacao = valorDepoisTransacao
        };

    private static TipoContaTransacaoFinanceira ResolverTipoConta(long? contaBancariaId, long? cartaoId, TipoPagamento? tipoPagamento, TipoRecebimento? tipoRecebimento)
    {
        if (contaBancariaId.HasValue)
        {
            return TipoContaTransacaoFinanceira.ContaBancaria;
        }

        if (cartaoId.HasValue)
        {
            return TipoContaTransacaoFinanceira.Cartao;
        }

        if (tipoPagamento is TipoPagamento.CartaoCredito or TipoPagamento.CartaoDebito ||
            tipoRecebimento is TipoRecebimento.CartaoCredito or TipoRecebimento.CartaoDebito)
        {
            return TipoContaTransacaoFinanceira.Cartao;
        }

        return TipoContaTransacaoFinanceira.NaoInformado;
    }
}
