using Core.Application.Contracts.Financeiro;
using Core.Domain.Enums;
using Core.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Core.Tests.Unit.Infrastructure;

public sealed class RabbitMqRecorrenciaBackgroundPublisherTests
{
    [Fact]
    public async Task NaoDevePropagarExcecao_QuandoCanceladaPublicacaoDeDespesa()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var publisher = CriarPublisher();
        var mensagem = CriarMensagemDespesa();

        var exception = await Record.ExceptionAsync(() => publisher.PublicarDespesaAsync(mensagem, cts.Token));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NaoDevePropagarExcecao_QuandoCanceladaPublicacaoDeReceita()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var publisher = CriarPublisher();
        var mensagem = CriarMensagemReceita();

        var exception = await Record.ExceptionAsync(() => publisher.PublicarReceitaAsync(mensagem, cts.Token));

        Assert.Null(exception);
    }

    private static RabbitMqRecorrenciaBackgroundPublisher CriarPublisher()
    {
        var options = Options.Create(new RabbitMqOptions());
        return new RabbitMqRecorrenciaBackgroundPublisher(options, NullLogger<RabbitMqRecorrenciaBackgroundPublisher>.Instance);
    }

    private static DespesaRecorrenciaBackgroundMessage CriarMensagemDespesa() =>
        new(
            UsuarioId: 1,
            DespesaRecorrenciaOrigemId: 1,
            Descricao: "Teste",
            Observacao: null,
            DataHoraCadastroOrigem: DateTime.UtcNow,
            DataLancamento: DateOnly.FromDateTime(DateTime.UtcNow),
            DataVencimento: DateOnly.FromDateTime(DateTime.UtcNow),
            TipoDespesa: TipoDespesa.Servicos,
            TipoPagamento: TipoPagamento.Transferencia,
            Recorrencia: Recorrencia.Unica,
            RecorrenciaFixa: false,
            QuantidadeRecorrencia: 1,
            ValorTotal: 100m,
            Desconto: 0m,
            Acrescimo: 0m,
            Imposto: 0m,
            Juros: 0m,
            ContaBancariaId: null,
            CartaoId: null,
            ValorTotalRateioAmigos: null,
            TipoRateioAmigos: null,
            Documentos: [],
            AmigosRateio: [],
            AreasSubAreasRateio: []);

    private static ReceitaRecorrenciaBackgroundMessage CriarMensagemReceita() =>
        new(
            UsuarioId: 1,
            Descricao: "Teste",
            Observacao: null,
            DataHoraCadastroOrigem: DateTime.UtcNow,
            DataLancamento: DateOnly.FromDateTime(DateTime.UtcNow),
            DataVencimento: DateOnly.FromDateTime(DateTime.UtcNow),
            TipoReceita: TipoReceita.Salario,
            TipoRecebimento: TipoRecebimento.Transferencia,
            Recorrencia: Recorrencia.Unica,
            RecorrenciaFixa: false,
            QuantidadeRecorrencia: 1,
            ValorTotal: 100m,
            Desconto: 0m,
            Acrescimo: 0m,
            Imposto: 0m,
            Juros: 0m,
            ContaBancariaId: null,
            CartaoId: null,
            ValorTotalRateioAmigos: null,
            TipoRateioAmigos: null,
            Documentos: [],
            AmigosRateio: [],
            AreasSubAreasRateio: []);
}
