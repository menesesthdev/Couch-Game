using CoachGame.Application.Calendario;
using CoachGame.Application.Estimativas;
using CoachGame.Application.Jogadores;
using Microsoft.Extensions.DependencyInjection;

namespace CoachGame.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ConsultarPerfil>();
        services.AddScoped<BuscarJogadores>();
        services.AddScoped<EstimarProgressao>();
        services.AddScoped<ObterAtoAtual>();
        return services;
    }
}
