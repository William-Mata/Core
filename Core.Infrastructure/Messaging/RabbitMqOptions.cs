namespace Core.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 15672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string QueueRecorrenciaDespesa { get; set; } = "financeiro.recorrencia.despesa";
    public string QueueRecorrenciaReceita { get; set; } = "financeiro.recorrencia.receita";
}
