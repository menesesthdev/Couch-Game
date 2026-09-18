using System.Net;
using System.Net.Http.Json;
using CoachGame.Application.Portas;
using CoachGame.Domain.Jogadores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoachGame.Infrastructure.HenrikDev;

/// <summary>
/// Cliente da HenrikDev API (não oficial, não afiliada à Riot). Retry/backoff para 429 e 5xx fica no
/// resilience handler registrado no DI; aqui só há cache por perfil e tradução de erros.
/// </summary>
public class HenrikDevClient(HttpClient http, IMemoryCache cache, IOptions<HenrikDevOptions> options) : IValorantDataProvider
{
    public async Task<PerfilValorant> BuscarPerfilAsync(RiotId riotId, Regiao regiao, CancellationToken ct)
    {
        var chave = $"henrikdev:{regiao}:{riotId}".ToLowerInvariant();
        if (cache.TryGetValue(chave, out PerfilValorant? emCache)) return emCache!;

        var perfil = await BuscarNaApiAsync(riotId, regiao, ct);
        cache.Set(chave, perfil, options.Value.Cache);
        return perfil;
    }

    private async Task<PerfilValorant> BuscarNaApiAsync(RiotId riotId, Regiao regiao, CancellationToken ct)
    {
        var url = $"valorant/v2/mmr-history/{regiao.ToString().ToLowerInvariant()}/{options.Value.Plataforma}/" +
                  $"{Uri.EscapeDataString(riotId.Nome)}/{Uri.EscapeDataString(riotId.Tag)}";

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.GetAsync(url, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FonteIndisponivelException("Não foi possível falar com a HenrikDev API agora.", ex);
        }

        using (resposta)
        {
            switch (resposta.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new JogadorNaoEncontradoException($"Não encontramos {riotId} nessa região.");
                case HttpStatusCode.TooManyRequests:
                    throw new FonteIndisponivelException("Muitas consultas seguidas. Tente de novo em alguns instantes.");
                case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                    throw new FonteIndisponivelException("Chave da HenrikDev API ausente ou inválida.");
            }
            if (!resposta.IsSuccessStatusCode)
                throw new FonteIndisponivelException($"HenrikDev API respondeu {(int)resposta.StatusCode}.");

            var corpo = await resposta.Content.ReadFromJsonAsync<MmrHistoryResponse>(ct);
            var dados = corpo?.Data;
            if (dados is null || dados.History.Count == 0)
                throw new JogadorNaoEncontradoException($"{riotId} não tem partidas ranqueadas recentes.");

            var maisRecente = dados.History.MaxBy(h => h.Date)!;
            var rank = new Rank((Tier)maisRecente.Tier.Id, Math.Max(0, maisRecente.Rr));
            var partidas = dados.History.Select(h => new PartidaRR(h.Date, h.LastChange)).ToList();

            return new PerfilValorant(dados.Account.Puuid, new RiotId(dados.Account.Name, dados.Account.Tag), rank, partidas);
        }
    }
}
