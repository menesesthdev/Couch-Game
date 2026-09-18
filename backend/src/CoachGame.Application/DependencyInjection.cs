using CoachGame.Application.Estimativas;
using Microsoft.Extensions.DependencyInjection;

namespace CoachGame.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<EstimarProgressao>();
        return services;
    }
}
