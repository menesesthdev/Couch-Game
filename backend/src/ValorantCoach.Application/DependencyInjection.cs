using ValorantCoach.Application.Calendario;
using ValorantCoach.Application.Estimativas;
using ValorantCoach.Application.Jogadores;
using ValorantCoach.Application.Loja;
using ValorantCoach.Application.Partidas;
using Microsoft.Extensions.DependencyInjection;

namespace ValorantCoach.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ConsultarPerfil>();
        services.AddScoped<BuscarJogadores>();
        services.AddScoped<EstimarProgressao>();
        services.AddScoped<ObterAtoAtual>();
        services.AddScoped<AnalisarDesempenho>();
        services.AddScoped<ObterDetalhePartida>();
        services.AddScoped<ConectarConta>();
        services.AddScoped<ObterMinhaLoja>();
        return services;
    }
}
