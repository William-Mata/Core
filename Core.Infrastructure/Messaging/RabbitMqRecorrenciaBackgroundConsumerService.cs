using System.Text.Json;
using System.Text.Json.Serialization;
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
private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarEIniciarConsumoAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested && _connection?.IsOpen == true && _channel?.IsOpen == true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponivel para consumidor de recorrencia. Nova tentativa em {DelaySeconds}s.", RetryDelay.TotalSeconds);
            }

            await FecharConexaoAsync();

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    private async Task ConectarEIniciarConsumoAsync(CancellationToken stoppingToken)
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
        var alvo = payload.RecorrenciaFixa
            ? Math.Max(100, payload.QuantidadeRecorrencia.GetValueOrDefault(100))
            : payload.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo <= 1) return;

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var liquido = payload.ValorTotal - payload.Desconto + payload.Acrescimo + payload.Imposto + payload.Juros;
        var origensCriadas = new List<Despesa>();

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = AvancarData(payload.DataLancamento, payload.Recorrencia, numero - 1);
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);
            var origemJaExiste = await dbContext.Despesas
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.DespesaOrigemId == null &&
                        x.UsuarioCadastroId == payload.UsuarioId &&
                        (
                            x.DespesaRecorrenciaOrigemId == payload.DespesaRecorrenciaOrigemId ||
                            (x.DespesaRecorrenciaOrigemId == null &&
                             x.DataHoraCadastro == payload.DataHoraCadastroOrigem &&
                             x.Descricao == payload.Descricao &&
                             x.TipoDespesa == payload.TipoDespesa &&
                             x.TipoPagamento == payload.TipoPagamento)
                        ) &&
                        x.DataLancamento == dataLancamento &&
                        x.DataVencimento == dataVencimento,
                    cancellationToken);

            if (origemJaExiste)
                continue;

            var origem = new Despesa
            {
                DataHoraCadastro = payload.DataHoraCadastroOrigem,
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                DataLancamento = dataLancamento,
                DataVencimento = dataVencimento,
                TipoDespesa = payload.TipoDespesa,
                TipoPagamento = payload.TipoPagamento,
                Recorrencia = payload.Recorrencia,
                RecorrenciaFixa = payload.RecorrenciaFixa,
                QuantidadeRecorrencia = payload.QuantidadeRecorrencia,
                DespesaRecorrenciaOrigemId = payload.DespesaRecorrenciaOrigemId,
                ValorTotal = payload.ValorTotal,
                ValorTotalRateioAmigos = payload.ValorTotalRateioAmigos,
                TipoRateioAmigos = payload.TipoRateioAmigos,
                ValorLiquido = liquido,
                Desconto = payload.Desconto,
                Acrescimo = payload.Acrescimo,
                Imposto = payload.Imposto,
                Juros = payload.Juros,
                ContaBancariaId = payload.ContaBancariaId,
                CartaoId = payload.CartaoId,
                Status = StatusDespesa.Pendente,
                Documentos = (payload.Documentos ?? []).Select(x => new Documento
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    NomeArquivo = x.NomeArquivo,
                    CaminhoArquivo = x.CaminhoArquivo,
                    ContentType = x.ContentType,
                    TamanhoBytes = x.TamanhoBytes
                }).ToList(),
                AmigosRateio = payload.AmigosRateio.Select(x => new DespesaAmigoRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AmigoId = x.AmigoId,
                    AmigoNome = x.Nome,
                    Valor = x.Valor
                }).ToList(),
                AreasRateio = payload.AreasSubAreasRateio.Select(x => new DespesaAreaRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AreaId = x.AreaId,
                    SubAreaId = x.SubAreaId,
                    Valor = x.Valor
                }).ToList(),
                Logs =
                [
                    new DespesaLog
                    {
                        UsuarioCadastroId = payload.UsuarioId,
                        Acao = AcaoLogs.Cadastro,
                        Descricao = "Despesa criada com status pendente."
                    }
                ]
            };

            dbContext.Despesas.Add(origem);
            origensCriadas.Add(origem);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (origensCriadas.Count == 0 || payload.AmigosRateio.Count == 0)
            return;

        var amigosAceitos = await dbContext.Amizades
            .AsNoTracking()
            .Where(x => x.UsuarioAId == payload.UsuarioId || x.UsuarioBId == payload.UsuarioId)
            .Select(x => x.UsuarioAId == payload.UsuarioId ? x.UsuarioBId : x.UsuarioAId)
            .ToHashSetAsync(cancellationToken);

        var amigosRateioValidos = payload.AmigosRateio
            .Where(x => x.AmigoId > 0 && x.Valor is > 0m && amigosAceitos.Contains(x.AmigoId))
            .Select(x => new { x.AmigoId, Valor = x.Valor!.Value })
            .ToArray();

        if (amigosRateioValidos.Length == 0)
            return;

        foreach (var origem in origensCriadas)
        {
            foreach (var amigo in amigosRateioValidos)
            {
                dbContext.Despesas.Add(new Despesa
                {
                    DataHoraCadastro = payload.DataHoraCadastroOrigem,
                    DespesaOrigemId = origem.Id,
                    UsuarioCadastroId = amigo.AmigoId,
                    Descricao = origem.Descricao,
                    Observacao = origem.Observacao,
                    DataLancamento = origem.DataLancamento,
                    DataVencimento = origem.DataVencimento,
                    TipoDespesa = origem.TipoDespesa,
                    TipoPagamento = origem.TipoPagamento,
                    Recorrencia = origem.Recorrencia,
                    RecorrenciaFixa = origem.RecorrenciaFixa,
                    QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
                    ValorTotal = amigo.Valor,
                    ValorTotalRateioAmigos = null,
                    TipoRateioAmigos = null,
                    ValorLiquido = amigo.Valor,
                    Desconto = 0m,
                    Acrescimo = 0m,
                    Imposto = 0m,
                    Juros = 0m,
                    ContaBancariaId = origem.ContaBancariaId,
                    CartaoId = origem.CartaoId,
                    Status = StatusDespesa.PendenteAprovacao,
                    AreasRateio = DistribuirAreasDespesa(payload.AreasSubAreasRateio, payload.ValorTotal, amigo.Valor, amigo.AmigoId),
                    Logs =
                    [
                        new DespesaLog
                        {
                            UsuarioCadastroId = payload.UsuarioId,
                            Acao = AcaoLogs.Cadastro,
                            Descricao = "Despesa compartilhada aguardando aprovacao."
                        }
                    ]
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessarReceitaAsync(ReceitaRecorrenciaBackgroundMessage payload, CancellationToken cancellationToken)
    {
        var alvo = payload.RecorrenciaFixa
            ? Math.Max(100, payload.QuantidadeRecorrencia.GetValueOrDefault(100))
            : payload.QuantidadeRecorrencia.GetValueOrDefault(1);
        if (alvo <= 1) return;

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var liquido = payload.ValorTotal - payload.Desconto + payload.Acrescimo + payload.Imposto + payload.Juros;
        var origensCriadas = new List<Receita>();

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = AvancarData(payload.DataLancamento, payload.Recorrencia, numero - 1);
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);

            var origem = new Receita
            {
                DataHoraCadastro = payload.DataHoraCadastroOrigem,
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                DataLancamento = dataLancamento,
                DataVencimento = dataVencimento,
                TipoReceita = payload.TipoReceita,
                TipoRecebimento = payload.TipoRecebimento,
                Recorrencia = payload.Recorrencia,
                RecorrenciaFixa = payload.RecorrenciaFixa,
                QuantidadeRecorrencia = payload.QuantidadeRecorrencia,
                ValorTotal = payload.ValorTotal,
                ValorTotalRateioAmigos = payload.ValorTotalRateioAmigos,
                TipoRateioAmigos = payload.TipoRateioAmigos,
                ValorLiquido = liquido,
                Desconto = payload.Desconto,
                Acrescimo = payload.Acrescimo,
                Imposto = payload.Imposto,
                Juros = payload.Juros,
                Status = StatusReceita.Pendente,
                ContaBancariaId = payload.ContaBancariaId,
                CartaoId = payload.CartaoId,
                Documentos = (payload.Documentos ?? []).Select(x => new Documento
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    NomeArquivo = x.NomeArquivo,
                    CaminhoArquivo = x.CaminhoArquivo,
                    ContentType = x.ContentType,
                    TamanhoBytes = x.TamanhoBytes
                }).ToList(),
                AmigosRateio = payload.AmigosRateio.Select(x => new ReceitaAmigoRateio
                {
                    UsuarioCadastroId = payload.UsuarioId,
                    AmigoId = x.AmigoId,
                    AmigoNome = x.Nome,
                    Valor = x.Valor
                }).ToList(),
                AreasRateio = payload.AreasSubAreasRateio.Select(x => new ReceitaAreaRateio
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
                        Descricao = "Receita criada com status pendente."
                    }
                ]
            };

            dbContext.Receitas.Add(origem);
            origensCriadas.Add(origem);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (origensCriadas.Count == 0 || payload.AmigosRateio.Count == 0)
            return;

        var amigosAceitos = await dbContext.Amizades
            .AsNoTracking()
            .Where(x => x.UsuarioAId == payload.UsuarioId || x.UsuarioBId == payload.UsuarioId)
            .Select(x => x.UsuarioAId == payload.UsuarioId ? x.UsuarioBId : x.UsuarioAId)
            .ToHashSetAsync(cancellationToken);

        var amigosRateioValidos = payload.AmigosRateio
            .Where(x => x.AmigoId > 0 && x.Valor is > 0m && amigosAceitos.Contains(x.AmigoId))
            .Select(x => new { x.AmigoId, Valor = x.Valor!.Value })
            .ToArray();

        if (amigosRateioValidos.Length == 0)
            return;

        foreach (var origem in origensCriadas)
        {
            foreach (var amigo in amigosRateioValidos)
            {
                dbContext.Receitas.Add(new Receita
                {
                    DataHoraCadastro = payload.DataHoraCadastroOrigem,
                    ReceitaOrigemId = origem.Id,
                    UsuarioCadastroId = amigo.AmigoId,
                    Descricao = origem.Descricao,
                    Observacao = origem.Observacao,
                    DataLancamento = origem.DataLancamento,
                    DataVencimento = origem.DataVencimento,
                    TipoReceita = origem.TipoReceita,
                    TipoRecebimento = origem.TipoRecebimento,
                    Recorrencia = origem.Recorrencia,
                    RecorrenciaFixa = origem.RecorrenciaFixa,
                    QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
                    ValorTotal = amigo.Valor,
                    ValorTotalRateioAmigos = null,
                    TipoRateioAmigos = null,
                    ValorLiquido = amigo.Valor,
                    Desconto = 0m,
                    Acrescimo = 0m,
                    Imposto = 0m,
                    Juros = 0m,
                    Status = StatusReceita.PendenteAprovacao,
                    ContaBancariaId = origem.ContaBancariaId,
                    CartaoId = origem.CartaoId,
                    AreasRateio = DistribuirAreasReceita(payload.AreasSubAreasRateio, payload.ValorTotal, amigo.Valor, amigo.AmigoId),
                    Logs =
                    [
                        new ReceitaLog
                        {
                            UsuarioCadastroId = payload.UsuarioId,
                            Acao = AcaoLogs.Cadastro,
                            Descricao = "Receita compartilhada aguardando aprovacao."
                        }
                    ]
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<DespesaAreaRateio> DistribuirAreasDespesa(
        IReadOnlyCollection<RateioAreaBackgroundMessage> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem.Where(x => x.Valor.HasValue && x.Valor > 0).ToArray();
        if (areas.Length == 0)
            return [];

        var resultado = new List<DespesaAreaRateio>(areas.Length);
        var acumulado = 0m;
        var fator = valorEspelho / valorTotalOrigem;

        for (var i = 0; i < areas.Length; i++)
        {
            var item = areas[i];
            var valor = i == areas.Length - 1
                ? Math.Round(valorEspelho - acumulado, 2, MidpointRounding.AwayFromZero)
                : Math.Round(item.Valor!.Value * fator, 2, MidpointRounding.AwayFromZero);

            if (valor < 0)
                valor = 0;

            acumulado += valor;
            resultado.Add(new DespesaAreaRateio
            {
                UsuarioCadastroId = usuarioCadastroId,
                AreaId = item.AreaId,
                SubAreaId = item.SubAreaId,
                Valor = valor
            });
        }

        return resultado;
    }

    private static List<ReceitaAreaRateio> DistribuirAreasReceita(
        IReadOnlyCollection<RateioAreaBackgroundMessage> areasRateioOrigem,
        decimal valorTotalOrigem,
        decimal valorEspelho,
        int usuarioCadastroId)
    {
        if (areasRateioOrigem.Count == 0 || valorTotalOrigem <= 0 || valorEspelho <= 0)
            return [];

        var areas = areasRateioOrigem.Where(x => x.Valor.HasValue && x.Valor > 0).ToArray();
        if (areas.Length == 0)
            return [];

        var resultado = new List<ReceitaAreaRateio>(areas.Length);
        var acumulado = 0m;
        var fator = valorEspelho / valorTotalOrigem;

        for (var i = 0; i < areas.Length; i++)
        {
            var item = areas[i];
            var valor = i == areas.Length - 1
                ? Math.Round(valorEspelho - acumulado, 2, MidpointRounding.AwayFromZero)
                : Math.Round(item.Valor!.Value * fator, 2, MidpointRounding.AwayFromZero);

            if (valor < 0)
                valor = 0;

            acumulado += valor;
            resultado.Add(new ReceitaAreaRateio
            {
                UsuarioCadastroId = usuarioCadastroId,
                AreaId = item.AreaId,
                SubAreaId = item.SubAreaId,
                Valor = valor
            });
        }

        return resultado;
    }

    private static DateOnly AvancarData(DateOnly data, Recorrencia recorrencia, int repeticoes)
    {
        return recorrencia switch
        {
            Recorrencia.Diaria => data.AddDays(repeticoes),
            Recorrencia.Semanal => data.AddDays(7 * repeticoes),
            Recorrencia.Quinzenal => data.AddDays(15 * repeticoes),
            Recorrencia.Mensal => data.AddMonths(repeticoes),
            Recorrencia.Trimestral => data.AddMonths(3 * repeticoes),
            Recorrencia.Semestral => data.AddMonths(6 * repeticoes),
            Recorrencia.Anual => data.AddYears(repeticoes),
            _ => data
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await FecharConexaoAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FecharConexaoAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
