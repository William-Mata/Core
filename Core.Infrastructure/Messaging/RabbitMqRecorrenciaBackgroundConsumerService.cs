using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Application.Contracts.Financeiro;
using Core.Application.Services.Financeiro;
using Core.Domain.Common;
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
        await _channel.QueueDeclareAsync(options.Value.QueueFaturaCartaoGarantiaSaneamento, true, false, false, cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var despesaConsumer = new AsyncEventingBasicConsumer(_channel);
        despesaConsumer.ReceivedAsync += OnDespesaReceivedAsync;
        await _channel.BasicConsumeAsync(options.Value.QueueRecorrenciaDespesa, false, despesaConsumer, stoppingToken);

        var receitaConsumer = new AsyncEventingBasicConsumer(_channel);
        receitaConsumer.ReceivedAsync += OnReceitaReceivedAsync;
        await _channel.BasicConsumeAsync(options.Value.QueueRecorrenciaReceita, false, receitaConsumer, stoppingToken);

        var faturaGarantiaSaneamentoConsumer = new AsyncEventingBasicConsumer(_channel);
        faturaGarantiaSaneamentoConsumer.ReceivedAsync += OnFaturaGarantiaSaneamentoReceivedAsync;
        await _channel.BasicConsumeAsync(options.Value.QueueFaturaCartaoGarantiaSaneamento, false, faturaGarantiaSaneamentoConsumer, stoppingToken);
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

    private async Task OnFaturaGarantiaSaneamentoReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null) return;

        try
        {
            var payload = JsonSerializer.Deserialize<FaturaCartaoGarantiaSaneamentoBackgroundMessage>(eventArgs.Body.Span, SerializerOptions);
            if (payload is null) throw new InvalidOperationException("Mensagem de garantia/saneamento de fatura invalida.");

            await ProcessarFaturaGarantiaSaneamentoAsync(payload, CancellationToken.None);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem de garantia/saneamento de fatura.");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    private async Task ProcessarFaturaGarantiaSaneamentoAsync(FaturaCartaoGarantiaSaneamentoBackgroundMessage payload, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var faturaCartaoService = scope.ServiceProvider.GetRequiredService<FaturaCartaoService>();
        await faturaCartaoService.ProcessarGarantiaESaneamentoAsync(payload.UsuarioId, payload.Competencia, cancellationToken);
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
        var dataHoraCadastroOrigem = DataHoraBrasil.Converter(payload.DataHoraCadastroOrigem);

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = payload.DataLancamento;
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);
            var competencia = new DateTime(dataVencimento.Year, dataVencimento.Month, 1).ToString("yyyy-MM");
            var dataLancamentoInicio = dataLancamento.Date;
            var dataLancamentoFim = dataLancamentoInicio.AddDays(1);
            var origemJaExiste = await dbContext.Despesas
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.DespesaOrigemId == null &&
                        x.UsuarioCadastroId == payload.UsuarioId &&
                        (
                            x.DespesaRecorrenciaOrigemId == payload.DespesaRecorrenciaOrigemId ||
                            (x.DespesaRecorrenciaOrigemId == null &&
                             x.DataHoraCadastro == dataHoraCadastroOrigem &&
                             x.Descricao == payload.Descricao &&
                             x.TipoDespesa == payload.TipoDespesa &&
                             x.TipoPagamento == payload.TipoPagamento)
                        ) &&
                        x.DataLancamento >= dataLancamentoInicio &&
                        x.DataLancamento < dataLancamentoFim &&
                        x.DataVencimento == dataVencimento,
                    cancellationToken);

            if (origemJaExiste)
                continue;

            var origem = new Despesa
            {
                DataHoraCadastro = dataHoraCadastroOrigem,
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                Competencia = competencia,
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
                ContaDestinoId = payload.ContaDestinoId,
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

        await SincronizarReceitasEspelhoTransacaoEntreContasAsync(dbContext, origensCriadas, payload.UsuarioId, cancellationToken);

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
                    DataHoraCadastro = dataHoraCadastroOrigem,
                    DespesaOrigemId = origem.Id,
                    UsuarioCadastroId = amigo.AmigoId,
                    Descricao = origem.Descricao,
                    Observacao = origem.Observacao,
                    Competencia = origem.Competencia,
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
                    ContaDestinoId = origem.ContaDestinoId,
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
        var dataHoraCadastroOrigem = DataHoraBrasil.Converter(payload.DataHoraCadastroOrigem);

        for (var numero = 2; numero <= alvo; numero++)
        {
            var dataLancamento = payload.DataLancamento;
            var dataVencimento = AvancarData(payload.DataVencimento, payload.Recorrencia, numero - 1);
            var competencia = new DateTime(dataVencimento.Year, dataVencimento.Month, 1).ToString("yyyy-MM");
            var dataLancamentoInicio = dataLancamento.Date;
            var dataLancamentoFim = dataLancamentoInicio.AddDays(1);
            var origemJaExiste = await dbContext.Receitas
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ReceitaOrigemId == null &&
                        x.UsuarioCadastroId == payload.UsuarioId &&
                        (
                            (payload.ReceitaRecorrenciaOrigemId.HasValue && x.ReceitaRecorrenciaOrigemId == payload.ReceitaRecorrenciaOrigemId.Value) ||
                            (x.ReceitaRecorrenciaOrigemId == null &&
                             x.DataHoraCadastro == dataHoraCadastroOrigem &&
                             x.Descricao == payload.Descricao &&
                             x.TipoReceita == payload.TipoReceita &&
                             x.TipoRecebimento == payload.TipoRecebimento)
                        ) &&
                        x.DataLancamento >= dataLancamentoInicio &&
                        x.DataLancamento < dataLancamentoFim &&
                        x.DataVencimento == dataVencimento,
                    cancellationToken);

            if (origemJaExiste)
                continue;

            var origem = new Receita
            {
                DataHoraCadastro = dataHoraCadastroOrigem,
                UsuarioCadastroId = payload.UsuarioId,
                Descricao = payload.Descricao,
                Observacao = payload.Observacao,
                Competencia = competencia,
                DataLancamento = dataLancamento,
                DataVencimento = dataVencimento,
                TipoReceita = payload.TipoReceita,
                TipoRecebimento = payload.TipoRecebimento,
                Recorrencia = payload.Recorrencia,
                RecorrenciaFixa = payload.RecorrenciaFixa,
                QuantidadeRecorrencia = payload.QuantidadeRecorrencia,
                ReceitaRecorrenciaOrigemId = payload.ReceitaRecorrenciaOrigemId,
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
                ContaDestinoId = payload.ContaDestinoId,
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

        await SincronizarDespesasEspelhoTransacaoEntreContasAsync(dbContext, origensCriadas, payload.UsuarioId, cancellationToken);

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
                    DataHoraCadastro = dataHoraCadastroOrigem,
                    ReceitaOrigemId = origem.Id,
                    UsuarioCadastroId = amigo.AmigoId,
                    Descricao = origem.Descricao,
                    Observacao = origem.Observacao,
                    Competencia = origem.Competencia,
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
                    ContaDestinoId = origem.ContaDestinoId,
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

    private static async Task SincronizarDespesasEspelhoTransacaoEntreContasAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Receita> origensCriadas,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var recorrenciasTransferencia = origensCriadas
            .Where(receita => EhTransacaoEntreContasReceita(receita.TipoRecebimento, receita.ContaBancariaId, receita.ContaDestinoId, receita.CartaoId))
            .ToArray();

        if (recorrenciasTransferencia.Length == 0)
            return;

        var idsReferenciaReceitas = recorrenciasTransferencia
            .SelectMany(x => new[] { x.Id, x.ReceitaRecorrenciaOrigemId ?? x.Id })
            .Distinct()
            .ToArray();
        var despesasExistentes = await dbContext.Despesas
            .AsNoTracking()
            .Where(x => x.ReceitaTransferenciaId.HasValue && idsReferenciaReceitas.Contains(x.ReceitaTransferenciaId.Value))
            .ToDictionaryAsync(x => x.ReceitaTransferenciaId!.Value, cancellationToken);

        var novasDespesas = new List<(Receita receita, Despesa despesa)>();
        foreach (var receita in recorrenciasTransferencia)
        {
            var receitaBaseId = receita.ReceitaRecorrenciaOrigemId ?? receita.Id;

            if (despesasExistentes.TryGetValue(receita.Id, out var despesaExistente))
            {
                receita.DespesaTransferenciaId = despesaExistente.Id;
                continue;
            }

            var despesa = CriarDespesaEspelhoTransacaoEntreContas(receita, usuarioId);
            if (despesa.Recorrencia != Recorrencia.Unica &&
                receitaBaseId != receita.Id &&
                despesasExistentes.TryGetValue(receitaBaseId, out var despesaBase))
            {
                despesa.DespesaRecorrenciaOrigemId = despesaBase.DespesaRecorrenciaOrigemId ?? despesaBase.Id;
            }

            dbContext.Despesas.Add(despesa);
            novasDespesas.Add((receita, despesa));
        }

        if (novasDespesas.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (receita, despesa) in novasDespesas)
        {
            receita.DespesaTransferenciaId = despesa.Id;
            despesasExistentes[receita.Id] = despesa;
        }

        foreach (var (receita, despesa) in novasDespesas)
        {
            if (despesa.Recorrencia == Recorrencia.Unica)
            {
                despesa.DespesaRecorrenciaOrigemId = null;
                continue;
            }

            var receitaBaseId = receita.ReceitaRecorrenciaOrigemId ?? receita.Id;
            if (despesasExistentes.TryGetValue(receitaBaseId, out var despesaBase))
            {
                despesa.DespesaRecorrenciaOrigemId = despesaBase.DespesaRecorrenciaOrigemId ?? despesaBase.Id;
            }
            else
            {
                despesa.DespesaRecorrenciaOrigemId = despesa.Id;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool EhTransacaoEntreContasReceita(TipoRecebimento tipoRecebimento, long? contaOrigemId, long? contaDestinoId, long? cartaoId) =>
        tipoRecebimento is TipoRecebimento.Transferencia or TipoRecebimento.Pix
        && contaOrigemId.HasValue
        && contaDestinoId.HasValue
        && !cartaoId.HasValue;

    private static Despesa CriarDespesaEspelhoTransacaoEntreContas(Receita origem, int usuarioId) =>
        new()
        {
            UsuarioCadastroId = origem.UsuarioCadastroId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            Competencia = origem.Competencia,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            DataEfetivacao = origem.DataEfetivacao,
            TipoDespesa = TipoDespesa.Outros,
            TipoPagamento = origem.TipoRecebimento == TipoRecebimento.Pix ? TipoPagamento.Pix : TipoPagamento.Transferencia,
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ValorTotal = origem.ValorTotal,
            ValorLiquido = origem.ValorLiquido,
            Desconto = origem.Desconto,
            Acrescimo = origem.Acrescimo,
            Imposto = origem.Imposto,
            Juros = origem.Juros,
            ValorEfetivacao = origem.ValorEfetivacao,
            Status = origem.Status == StatusReceita.Efetivada ? StatusDespesa.Efetivada : StatusDespesa.Pendente,
            ContaBancariaId = origem.ContaDestinoId,
            ContaDestinoId = origem.ContaBancariaId,
            CartaoId = null,
            ReceitaTransferenciaId = origem.Id,
            Logs =
            [
                new DespesaLog
                {
                    UsuarioCadastroId = usuarioId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Despesa espelhada criada automaticamente por transacao entre contas."
                }
            ]
        };

    private static async Task SincronizarReceitasEspelhoTransacaoEntreContasAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Despesa> origensCriadas,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var recorrenciasTransferencia = origensCriadas
            .Where(despesa => EhTransacaoEntreContasDespesa(despesa.TipoPagamento, despesa.ContaBancariaId, despesa.ContaDestinoId, despesa.CartaoId))
            .ToArray();

        if (recorrenciasTransferencia.Length == 0)
            return;

        var idsReferenciaDespesas = recorrenciasTransferencia
            .SelectMany(x => new[] { x.Id, x.DespesaRecorrenciaOrigemId ?? x.Id })
            .Distinct()
            .ToArray();
        var receitasExistentes = await dbContext.Receitas
            .AsNoTracking()
            .Where(x => x.DespesaTransferenciaId.HasValue && idsReferenciaDespesas.Contains(x.DespesaTransferenciaId.Value))
            .ToDictionaryAsync(x => x.DespesaTransferenciaId!.Value, cancellationToken);

        var novasReceitas = new List<(Despesa despesa, Receita receita)>();
        foreach (var despesa in recorrenciasTransferencia)
        {
            var despesaBaseId = despesa.DespesaRecorrenciaOrigemId ?? despesa.Id;

            if (receitasExistentes.TryGetValue(despesa.Id, out var receitaExistente))
            {
                despesa.ReceitaTransferenciaId = receitaExistente.Id;
                continue;
            }

            var receita = CriarReceitaEspelhoTransacaoEntreContas(despesa, usuarioId);
            if (receita.Recorrencia != Recorrencia.Unica &&
                despesaBaseId != despesa.Id &&
                receitasExistentes.TryGetValue(despesaBaseId, out var receitaBase))
            {
                receita.ReceitaRecorrenciaOrigemId = receitaBase.ReceitaRecorrenciaOrigemId ?? receitaBase.Id;
            }

            dbContext.Receitas.Add(receita);
            novasReceitas.Add((despesa, receita));
        }

        if (novasReceitas.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (despesa, receita) in novasReceitas)
        {
            despesa.ReceitaTransferenciaId = receita.Id;
            receitasExistentes[despesa.Id] = receita;
        }

        foreach (var (despesa, receita) in novasReceitas)
        {
            if (receita.Recorrencia == Recorrencia.Unica)
            {
                receita.ReceitaRecorrenciaOrigemId = null;
                continue;
            }

            var despesaBaseId = despesa.DespesaRecorrenciaOrigemId ?? despesa.Id;
            if (receitasExistentes.TryGetValue(despesaBaseId, out var receitaBase))
            {
                receita.ReceitaRecorrenciaOrigemId = receitaBase.ReceitaRecorrenciaOrigemId ?? receitaBase.Id;
            }
            else
            {
                receita.ReceitaRecorrenciaOrigemId = receita.Id;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool EhTransacaoEntreContasDespesa(TipoPagamento tipoPagamento, long? contaOrigemId, long? contaDestinoId, long? cartaoId) =>
        tipoPagamento is TipoPagamento.Transferencia or TipoPagamento.Pix
        && contaOrigemId.HasValue
        && contaDestinoId.HasValue
        && !cartaoId.HasValue;

    private static Receita CriarReceitaEspelhoTransacaoEntreContas(Despesa origem, int usuarioId) =>
        new()
        {
            UsuarioCadastroId = origem.UsuarioCadastroId,
            Descricao = origem.Descricao,
            Observacao = origem.Observacao,
            Competencia = origem.Competencia,
            DataLancamento = origem.DataLancamento,
            DataVencimento = origem.DataVencimento,
            DataEfetivacao = origem.DataEfetivacao,
            TipoReceita = TipoReceita.Outros,
            TipoRecebimento = origem.TipoPagamento == TipoPagamento.Pix ? TipoRecebimento.Pix : TipoRecebimento.Transferencia,
            Recorrencia = origem.Recorrencia,
            RecorrenciaFixa = origem.RecorrenciaFixa,
            QuantidadeRecorrencia = origem.QuantidadeRecorrencia,
            ValorTotal = origem.ValorTotal,
            ValorLiquido = origem.ValorLiquido,
            Desconto = origem.Desconto,
            Acrescimo = origem.Acrescimo,
            Imposto = origem.Imposto,
            Juros = origem.Juros,
            ValorEfetivacao = origem.ValorEfetivacao,
            Status = origem.Status == StatusDespesa.Efetivada ? StatusReceita.Efetivada : StatusReceita.Pendente,
            ContaBancariaId = origem.ContaDestinoId,
            ContaDestinoId = origem.ContaBancariaId,
            CartaoId = null,
            DespesaTransferenciaId = origem.Id,
            Logs =
            [
                new ReceitaLog
                {
                    UsuarioCadastroId = usuarioId,
                    Acao = AcaoLogs.Cadastro,
                    Descricao = "Receita espelhada criada automaticamente por transacao entre contas."
                }
            ]
        };

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
