namespace Pricing.Application;

using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CalculateQuoteService>();
        return services;
    }
}