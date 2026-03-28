using System.Text.Json;
using Core.Application.Contracts.Financeiro;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Core.Infrastructure.Messaging;

public sealed class RabbitMqRecorrenciaBackgroundConsumerService(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqRecorrenciaBackgroundConsumerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Value.HostName,
            Port = options.Value.Port,
            UserName = options.Value.UserName,
            Password = options.Value.Password,
            VirtualHost = options.Value.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(options.Value.QueueRecorrenciaDespesa, true, false, false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(options.Value.QueueRecorrenciaReceita, true, false, false, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var despesaConsumer = new AsyncEventingBasicConsumer(_channel);
        despesaConsumer.ReceivedAsync += OnDespesaReceivedAsync;
        await _channel.BasicConsumeAsync(options.Value.QueueRecorrenciaDespesa, false, despesaConsumer, stoppingToken);

        var receitaConsumer = new AsyncEventingBasicConsumer(_channel);
        receitaConsumer.ReceivedAsync += OnReceitaReceivedAsync;
        await _channel.BasicConsumeAsync(options.Value.QueueRecorrenciaReceita, false, receitaConsumer, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task OnDespesaReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null) return;

        try
        {
            var payload = JsonSerializer.Deserialize<DespesaRecorrenciaBackgroundMessage>(eventArgs.Body.Span, SerializerOptions);
            if (payload is null) throw new InvalidOperationException("Mensagem de recorrencia de despesa invalida.");

            await ProcessarDespesaAsync(payload, CancellationToken.None);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem de recorrencia de despesa.");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    private async Task OnReceitaReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null) return;

        try
        {
            var payload = JsonSerializer.Deserialize<ReceitaRecorrenciaBackgroundMessage>(eventArgs.Body.Span, SerializerOptions);
            if (payload is null) throw new InvalidOperationException("Mensagem de recorrencia de receita invalida.");

            await ProcessarReceitaAsync(payload, CancellationToken.None);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem de recorrencia de receita.");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    private async Task ProcessarDespesaAsync(DespesaRecorrenciaBackgroundMessage payload, CancellationToken cancellationToken)
    {
        var alvo = payload.Recorrencia == Recorrencia.Fixa ? 100 : payload.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo <= 1) return;

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var liquido = payload.ValorTotal - payload.Desconto + payload.Acrescimo + payload.Imposto + payload.Juros;

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = AvancarData(payload.DataLancamento, payload.Recorrencia, numero - 1);
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);

            var jaExiste = await dbContext.Despesas.AnyAsync(
                x => x.UsuarioCadastroId == payload.UsuarioId &&
                     x.Descricao == payload.Descricao &&
                     x.DataLancamento == dataLancamento &&
                     x.TipoDespesa == payload.TipoDespesa &&
                     x.TipoPagamento == payload.TipoPagamento,
                cancellationToken);

            if (jaExiste) continue;

            dbContext.Despesas.Add(new Despesa
            {
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                DataLancamento = dataLancamento,
                DataVencimento = dataVencimento,
                TipoDespesa = payload.TipoDespesa,
                TipoPagamento = payload.TipoPagamento,
                Recorrencia = payload.Recorrencia,
                QuantidadeRecorrencia = payload.QuantidadeRecorrencia,
                ValorTotal = payload.ValorTotal,
                ValorLiquido = liquido,
                Desconto = payload.Desconto,
                Acrescimo = payload.Acrescimo,
                Imposto = payload.Imposto,
                Juros = payload.Juros,
                Status = StatusDespesa.Pendente,
                AnexoDocumento = payload.AnexoDocumento,
                AmigosRateio = payload.AmigosRateio.Select(x => new DespesaAmigoRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AmigoNome = x.Nome,
                    Valor = x.Valor
                }).ToList(),
                AreasRateio = payload.AreasRateio.Select(x => new DespesaAreaRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AreaId = x.AreaId,
                    SubAreaId = x.SubAreaId,
                    Valor = x.Valor
                }).ToList(),
                TiposRateio = payload.TiposRateio.Select(x => new DespesaTipoRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    TipoRateio = x
                }).ToList(),
                Logs =
                [
                    new DespesaLog
                    {
                        UsuarioCadastroId = payload.UsuarioId,
                        Acao = AcaoLogs.Cadastro,
                        Descricao = "Despesa recorrente gerada em background."
                    }
                ]
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessarReceitaAsync(ReceitaRecorrenciaBackgroundMessage payload, CancellationToken cancellationToken)
    {
        var alvo = payload.Recorrencia == Recorrencia.Fixa ? 100 : payload.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo <= 1) return;

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var liquido = payload.ValorTotal - payload.Desconto + payload.Acrescimo + payload.Imposto + payload.Juros;
        long? contaBancariaId = null;

        if (!string.IsNullOrWhiteSpace(payload.ContaBancaria) && long.TryParse(payload.ContaBancaria, out var contaId))
        {
            contaBancariaId = contaId;
        }

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = AvancarData(payload.DataLancamento, payload.Recorrencia, numero - 1);
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);

            var jaExiste = await dbContext.Receitas.AnyAsync(
                x => x.UsuarioCadastroId == payload.UsuarioId &&
                     x.Descricao == payload.Descricao &&
                     x.DataLancamento == dataLancamento &&
                     x.TipoReceita == payload.TipoReceita &&
                     x.TipoRecebimento == payload.TipoRecebimento,
                cancellationToken);

            if (jaExiste) continue;

            dbContext.Receitas.Add(new Receita
            {
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                DataLancamento = dataLancamento,
                DataVencimento = dataVencimento,
                TipoReceita = payload.TipoReceita,
                TipoRecebimento = payload.TipoRecebimento,
                Recorrencia = payload.Recorrencia,
                QuantidadeRecorrencia = payload.QuantidadeRecorrencia,
                ValorTotal = payload.ValorTotal,
                ValorLiquido = liquido,
                Desconto = payload.Desconto,
                Acrescimo = payload.Acrescimo,
                Imposto = payload.Imposto,
                Juros = payload.Juros,
                Status = StatusReceita.Pendente,
                ContaBancariaId = contaBancariaId,
                AnexoDocumento = payload.AnexoDocumento,
                AmigosRateio = payload.AmigosRateio.Select(x => new ReceitaAmigoRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AmigoNome = x.Nome,
                    Valor = x.Valor
                }).ToList(),
                AreasRateio = payload.AreasRateio.Select(x => new ReceitaAreaRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AreaId = x.AreaId,
                    SubAreaId = x.SubAreaId,
                    Valor = x.Valor
                }).ToList(),
                Logs =
                [
                    new ReceitaLog
                    {
                        UsuarioCadastroId = payload.UsuarioId,
                        Acao = AcaoLogs.Cadastro,
                        Descricao = "Receita recorrente gerada em background."
                    }
                ]
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly AvancarData(DateOnly data, Recorrencia recorrencia, int repeticoes)
    {
        return recorrencia switch
        {
            Recorrencia.Diaria => data.AddDays(repeticoes),
            Recorrencia.Semanal => data.AddDays(7 * repeticoes),
            Recorrencia.Quinzenal => data.AddDays(15 * repeticoes),
            Recorrencia.Mensal or Recorrencia.Fixa => data.AddMonths(repeticoes),
            Recorrencia.Trimestral => data.AddMonths(3 * repeticoes),
            Recorrencia.Semestral => data.AddMonths(6 * repeticoes),
            Recorrencia.Anual => data.AddYears(repeticoes),
            _ => data
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
