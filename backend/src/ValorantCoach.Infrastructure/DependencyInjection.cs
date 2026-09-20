using ValorantCoach.Application.Portas;
using ValorantCoach.Infrastructure.HenrikDev;
using ValorantCoach.Infrastructure.Persistencia;
using ValorantCoach.Infrastructure.Riot;
using ValorantCoach.Infrastructure.ValorantApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ValorantCoach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ValorantCoachDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("ValorantCoach")).UseSnakeCaseNamingConvention());
        services.AddScoped<IJogadorRepository, JogadorRepository>();

        services.AddMemoryCache();
        services.Configure<HenrikDevOptions>(config.GetSection(HenrikDevOptions.Secao));
        services.AddHttpClient<IValorantDataProvider, HenrikDevClient>((sp, http) =>
            {
                var opcoes = sp.GetRequiredService<IOptions<HenrikDevOptions>>().Value;
                http.BaseAddress = new Uri(opcoes.BaseUrl.TrimEnd('/') + "/");
                // Sem chave a API responde 401, que o cliente traduz em mensagem clara.
                if (!string.IsNullOrWhiteSpace(opcoes.ApiKey))
                    http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", opcoes.ApiKey);
            })
            // Retry com backoff exponencial para 429/5xx, respeitando o header Retry-After.
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.Delay = TimeSpan.FromSeconds(2);
            });

        services.AddHttpClient<ICalendarioCompetitivo, ValorantApiCalendario>(http =>
                http.BaseAddress = new Uri("https://valorant-api.com/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<ICatalogoValorant, ValorantApiCatalogo>(http =>
                http.BaseAddress = new Uri("https://valorant-api.com/"))
            .AddStandardResilienceHandler();

        // Endpoints do cliente do jogo: sem chave nossa, autenticados com o token do jogador.
        // Sem retry automático — um 401 aqui significa sessão vencida, não falha transitória.
        services.AddHttpClient<ILojaRiot, RiotStorefrontClient>();

        return services;
    }
}
