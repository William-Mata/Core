using System.Text;
using System.Text.Json;
using Core.Application.Contracts.Financeiro;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Core.Infrastructure.Messaging;

public sealed class RabbitMqRecorrenciaBackgroundPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqRecorrenciaBackgroundPublisher> logger) : IRecorrenciaBackgroundPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublicarDespesaAsync(DespesaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
    {
        await PublicarAsync(options.Value.QueueRecorrenciaDespesa, message, cancellationToken);
    }

    public async Task PublicarReceitaAsync(ReceitaRecorrenciaBackgroundMessage message, CancellationToken cancellationToken = default)
    {
        await PublicarAsync(options.Value.QueueRecorrenciaReceita, message, cancellationToken);
    }

    private async Task PublicarAsync<T>(string queueName, T message, CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = options.Value.HostName,
                Port = options.Value.Port,
                UserName = options.Value.UserName,
                Password = options.Value.Password,
                VirtualHost = options.Value.VirtualHost
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, SerializerOptions));
            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RabbitMQ indisponivel. Mensagem de recorrencia nao publicada na fila {QueueName}.", queueName);
        }
    }
}
