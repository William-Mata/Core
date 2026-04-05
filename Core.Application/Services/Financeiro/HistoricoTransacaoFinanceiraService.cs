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
        long? cartaoId = null,
        TipoRecebimento? tipoRecebimento = null,
        CancellationToken cancellationToken = default) =>
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
            cartaoId,
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
        long? cartaoId = null,
        TipoRecebimento? tipoRecebimento = null,
        CancellationToken cancellationToken = default)
    {
        if (!contaBancariaId.HasValue && !cartaoId.HasValue)
        {
            var ultimoHistorico = await repository.ObterUltimoPorTransacaoAsync(tipoTransacao, transacaoId, cancellationToken);
            if (ultimoHistorico is not null)
            {
                contaBancariaId = ultimoHistorico.ContaBancariaId;
                cartaoId = ultimoHistorico.CartaoId;
                tipoPagamento ??= ultimoHistorico.TipoPagamento;
                tipoRecebimento ??= ultimoHistorico.TipoRecebimento;
            }
        }

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
            cartaoId,
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
        long? cartaoId,
        CancellationToken cancellationToken)
    {
        var historico = new HistoricoTransacaoFinanceira
        {
            UsuarioOperacaoId = usuarioOperacaoId,
            TipoTransacao = tipoTransacao,
            TransacaoId = transacaoId,
            TipoOperacao = tipoOperacao,
            TipoConta = ResolverTipoConta(contaBancariaId, cartaoId, tipoPagamento, tipoRecebimento),
            ContaBancariaId = contaBancariaId,
            CartaoId = cartaoId,
            DataTransacao = dataTransacao,
            Descricao = descricao,
            TipoPagamento = tipoPagamento,
            TipoRecebimento = tipoRecebimento,
            ValorAntesTransacao = valorAntesTransacao,
            ValorTransacao = valorTransacao,
            ValorDepoisTransacao = valorDepoisTransacao
        };

        return repository.CriarAsync(historico, cancellationToken);
    }

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
