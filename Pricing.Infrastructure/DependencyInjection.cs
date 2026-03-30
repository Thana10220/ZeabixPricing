using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.Interfaces;
using Pricing.Infrastructure.Repositories;
using Pricing.Infrastructure.Configurations;

namespace Pricing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RuleSettings>(
            configuration.GetSection("RuleSettings")
        );
        services.AddScoped<IRuleRepository, RuleRepository>();
        return services;
    }
}