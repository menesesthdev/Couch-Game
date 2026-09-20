using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using ValorantCoach.Application.Portas;

namespace ValorantCoach.Infrastructure.ValorantApi;

/// <summary>
/// Catálogo público de skins e versão do cliente (valorant-api.com, sem chave). A Riot devolve a
/// loja só com ids; é isto que vira nome e arte. Muda a cada patch, então o cache é longo.
/// </summary>
public class ValorantApiCatalogo(HttpClient http, IMemoryCache cache) : ICatalogoValorant
{
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromHours(12);

    public async Task<IReadOnlyDictionary<string, ItemCatalogo>> ObterItensAsync(CancellationToken ct)
    {
        const string chave = "valorant-api:itens-loja";
        if (cache.TryGetValue(chave, out IReadOnlyDictionary<string, ItemCatalogo>? emCache)) return emCache!;

        // Níveis de skin cobrem o que aparece na loja diária e no mercado noturno;
        // os bundles são identificados à parte.
        var skins = await BuscarAsync<SkinLevelsResponse>("v1/weapons/skinlevels?language=pt-BR", ct);
        var bundles = await BuscarAsync<BundlesResponse>("v1/bundles?language=pt-BR", ct);

        var itens = new Dictionary<string, ItemCatalogo>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skins?.Data ?? [])
            itens[s.Uuid] = new ItemCatalogo(s.Uuid, s.DisplayName, s.DisplayIcon);
        foreach (var b in bundles?.Data ?? [])
            itens[b.Uuid] = new ItemCatalogo(b.Uuid, b.DisplayName, b.DisplayIcon ?? b.VerticalPromoImage);

        IReadOnlyDictionary<string, ItemCatalogo> resultado = itens;
        cache.Set(chave, resultado, DuracaoCache);
        return resultado;
    }

    public async Task<string> ObterVersaoClienteAsync(CancellationToken ct)
    {
        const string chave = "valorant-api:versao";
        if (cache.TryGetValue(chave, out string? emCache)) return emCache!;

        var versao = (await BuscarAsync<VersionResponse>("v1/version", ct))?.Data?.RiotClientVersion;
        if (string.IsNullOrWhiteSpace(versao))
            throw new FonteIndisponivelException("Não foi possível descobrir a versão do cliente do jogo.");

        cache.Set(chave, versao, DuracaoCache);
        return versao;
    }

    private async Task<T?> BuscarAsync<T>(string caminho, CancellationToken ct)
    {
        try
        {
            return await http.GetFromJsonAsync<T>(caminho, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FonteIndisponivelException("Não foi possível obter o catálogo de skins agora.", ex);
        }
    }

    private sealed record SkinLevelsResponse(List<SkinLevel>? Data);

    private sealed record SkinLevel(string Uuid, string DisplayName, string? DisplayIcon);

    private sealed record BundlesResponse(List<BundleInfo>? Data);

    private sealed record BundleInfo(string Uuid, string DisplayName, string? DisplayIcon, string? VerticalPromoImage);

    private sealed record VersionResponse(VersionData? Data);

    private sealed record VersionData(string? RiotClientVersion);
}
