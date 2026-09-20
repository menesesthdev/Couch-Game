using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Calendario;
using Microsoft.Extensions.Caching.Memory;

namespace ValorantCoach.Infrastructure.ValorantApi;

/// <summary>
/// Calendário de atos via valorant-api.com (<c>GET /v1/seasons</c>) — mesma fonte dos ícones de rank,
/// pública e sem chave. Muda poucas vezes por ano, então fica em cache por horas.
/// </summary>
public partial class ValorantApiCalendario(HttpClient http, IMemoryCache cache) : ICalendarioCompetitivo
{
    private const string ChaveCache = "valorant-api:atos";
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromHours(6);

    public async Task<IReadOnlyList<AtoCompetitivo>> ListarAtosAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(ChaveCache, out IReadOnlyList<AtoCompetitivo>? emCache)) return emCache!;

        SeasonsResponse? resposta;
        try
        {
            resposta = await http.GetFromJsonAsync<SeasonsResponse>("v1/seasons", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FonteIndisponivelException("Não foi possível obter o calendário de atos agora.", ex);
        }

        var temporadas = resposta?.Data ?? [];
        var episodios = temporadas.Where(s => s.ParentUuid is null).ToDictionary(s => s.Uuid, s => s.DisplayName);
        var atos = temporadas
            .Where(s => s.Type?.EndsWith("Act", StringComparison.Ordinal) == true)
            .Select(s => new AtoCompetitivo(
                NomeAto(s.ParentUuid is { } pai ? episodios.GetValueOrDefault(pai) : null, s.DisplayName),
                s.StartTime, s.EndTime))
            .OrderBy(a => a.Inicio)
            .ToList();

        cache.Set(ChaveCache, (IReadOnlyList<AtoCompetitivo>)atos, DuracaoCache);
        return atos;
    }

    /// <summary>"V26" + "ACT V" → "V26 · Ato V".</summary>
    private static string NomeAto(string? episodio, string ato)
    {
        var nome = AtoRegex().Replace(ato.Trim(), "Ato ");
        return string.IsNullOrWhiteSpace(episodio) ? nome : $"{episodio} · {nome}";
    }

    [GeneratedRegex(@"^ACT\s+", RegexOptions.IgnoreCase)]
    private static partial Regex AtoRegex();

    private sealed record SeasonsResponse(List<Season>? Data);

    private sealed record Season(Guid Uuid, string DisplayName, string? Type, DateTimeOffset StartTime, DateTimeOffset EndTime, Guid? ParentUuid);
}
