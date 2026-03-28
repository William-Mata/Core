using Core.Application.Services;
using Core.Application.Services.Administracao;
using Core.Application.Services.Financeiro;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Financeiro;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Repositories;
using Core.Infrastructure.Persistence.Repositories.Administracao;
using Core.Infrastructure.Persistence.Repositories.Financeiro;
using Core.Infrastructure.Security;
using Core.Infrastructure.Messaging;
using Core.Application.Contracts.Financeiro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddScoped<IAutenticacaoRepository, AutenticacaoRepository>();
        services.AddScoped<ITentativaLoginRepository, TentativaLoginRepository>();
        services.AddScoped<IContaBancariaRepository, ContaBancariaRepository>();
        services.AddScoped<ICartaoRepository, CartaoRepository>();
        services.AddScoped<IDespesaRepository, DespesaRepository>();
        services.AddScoped<IReceitaRepository, ReceitaRepository>();
        services.AddScoped<IReembolsoRepository, ReembolsoRepository>();
        services.AddScoped<IHistoricoTransacaoFinanceiraRepository, HistoricoTransacaoFinanceiraRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IRecorrenciaBackgroundPublisher, RabbitMqRecorrenciaBackgroundPublisher>();
        services.AddHostedService<RabbitMqRecorrenciaBackgroundConsumerService>();

        services.AddScoped<AutenticacaoService>();
        services.AddScoped<ContaBancariaService>();
        services.AddScoped<CartaoService>();
        services.AddScoped<AreaSubAreaFinanceiroService>();
        services.AddScoped<AmigoFinanceiroService>();
        services.AddScoped<DespesaService>();
        services.AddScoped<ReceitaService>();
        services.AddScoped<ReembolsoService>();
        services.AddScoped<HistoricoTransacaoFinanceiraService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<UsuarioService>();

        return services;
    }
}
