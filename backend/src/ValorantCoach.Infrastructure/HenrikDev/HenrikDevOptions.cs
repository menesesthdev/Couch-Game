namespace ValorantCoach.Infrastructure.HenrikDev;

public class HenrikDevOptions
{
    public const string Secao = "HenrikDev";

    public string BaseUrl { get; set; } = "https://api.henrikdev.xyz";
    public string ApiKey { get; set; } = "";
    public string Plataforma { get; set; } = "pc";

    /// <summary>Por quanto tempo reaproveitar a resposta de um perfil — evita estourar o limite (Basic = 30 req/min).</summary>
    public TimeSpan Cache { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Partida encerrada não muda mais, então o scoreboard pode ficar guardado por horas.</summary>
    public TimeSpan CacheDetalhePartida { get; set; } = TimeSpan.FromHours(6);
}
