using Core.Application.Services;
using Core.Application.Services.Financeiro;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Financeiro;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Repositories;
using Core.Infrastructure.Persistence.Repositories.Financeiro;
using Core.Infrastructure.Security;
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

        services.AddScoped<IAutenticacaoRepository, AutenticacaoRepository>();
        services.AddScoped<ITentativaLoginRepository, TentativaLoginRepository>();
        services.AddScoped<IContaBancariaRepository, ContaBancariaRepository>();
        services.AddScoped<ICartaoRepository, CartaoRepository>();
        services.AddScoped<IDespesaRepository, DespesaRepository>();
        services.AddScoped<IReceitaRepository, ReceitaRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<AutenticacaoService>();
        services.AddScoped<ContaBancariaService>();
        services.AddScoped<CartaoService>();
        services.AddScoped<DespesaService>();
        services.AddScoped<ReceitaService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<UsuarioService>();

        return services;
    }
}
