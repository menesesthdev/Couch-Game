using CoachGame.Application.Portas;
using CoachGame.Infrastructure.HenrikDev;
using CoachGame.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoachGame.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CoachGameDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("CoachGame")).UseSnakeCaseNamingConvention());
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

        return services;
    }
}
