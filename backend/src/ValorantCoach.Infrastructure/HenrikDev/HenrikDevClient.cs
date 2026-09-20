using System.Net;
using System.Net.Http.Json;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Partidas;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ValorantCoach.Infrastructure.HenrikDev;

/// <summary>
/// Cliente da HenrikDev API (não oficial, não afiliada à Riot). Retry/backoff para 429 e 5xx fica no
/// resilience handler registrado no DI; aqui só há cache e tradução de erros.
/// </summary>
public class HenrikDevClient(HttpClient http, IMemoryCache cache, IOptions<HenrikDevOptions> options) : IValorantDataProvider
{
    private HenrikDevOptions Opcoes => options.Value;

    public Task<PerfilValorant> BuscarPerfilAsync(RiotId riotId, Regiao regiao, CancellationToken ct) =>
        EmCacheAsync($"perfil:{regiao}:{riotId}", Opcoes.Cache, async () =>
        {
            var url = $"valorant/v2/mmr-history/{Afinidade(regiao)}/{Opcoes.Plataforma}/" +
                      $"{Uri.EscapeDataString(riotId.Nome)}/{Uri.EscapeDataString(riotId.Tag)}";
            var dados = (await ObterAsync<MmrHistoryResponse>(url, $"Não encontramos {riotId} nessa região.", ct))?.Data;

            if (dados is null || dados.History.Count == 0)
                throw new JogadorNaoEncontradoException($"{riotId} não tem partidas ranqueadas recentes.");

            var maisRecente = dados.History.MaxBy(h => h.Date)!;
            var rank = new Rank((Tier)maisRecente.Tier.Id, Math.Max(0, maisRecente.Rr));
            var partidas = dados.History
                .OrderByDescending(h => h.Date)
                .Select(h => new PartidaRR(h.Date, h.LastChange, h.MatchId, MapaDe(h.Map), (Tier)h.Tier.Id, h.Rr))
                .ToList();

            return new PerfilValorant(dados.Account.Puuid, new RiotId(dados.Account.Name, dados.Account.Tag), rank, partidas);
        });

    public Task<IReadOnlyList<ResumoPartida>> BuscarPartidasAsync(RiotId riotId, Regiao regiao, int quantidade, int pagina, CancellationToken ct) =>
        EmCacheAsync($"partidas:{regiao}:{riotId}:{quantidade}:{pagina}", Opcoes.Cache, async () =>
        {
            var url = $"valorant/v1/stored-matches/{Afinidade(regiao)}/" +
                      $"{Uri.EscapeDataString(riotId.Nome)}/{Uri.EscapeDataString(riotId.Tag)}" +
                      $"?mode=competitive&size={quantidade}&page={pagina}";
            var resposta = await ObterAsync<StoredMatchesResponse>(url, $"Não encontramos {riotId} nessa região.", ct);

            IReadOnlyList<ResumoPartida> partidas = (resposta?.Data ?? [])
                .Where(m => m.Stats is not null)
                .Select(ParaResumo)
                .OrderByDescending(p => p.Data)
                .ToList();
            return partidas;
        });

    public Task<IReadOnlyList<PartidaRR>> BuscarHistoricoRrAsync(RiotId riotId, Regiao regiao, int quantidade, int pagina, CancellationToken ct) =>
        // O v2/mmr-history devolve só as últimas partidas e ignora paginação; este aqui pagina de verdade.
        EmCacheAsync($"rr:{regiao}:{riotId}:{quantidade}:{pagina}", Opcoes.Cache, async () =>
        {
            var url = $"valorant/v1/stored-mmr-history/{Afinidade(regiao)}/" +
                      $"{Uri.EscapeDataString(riotId.Nome)}/{Uri.EscapeDataString(riotId.Tag)}" +
                      $"?size={quantidade}&page={pagina}";
            var resposta = await ObterAsync<StoredMmrHistoryResponse>(url, $"Não encontramos {riotId} nessa região.", ct);

            IReadOnlyList<PartidaRR> historico = (resposta?.Data ?? [])
                .Select(e => new PartidaRR(
                    e.Date, e.LastMmrChange, e.MatchId, MapaDe(e.Map),
                    (Tier)(e.Tier?.Id ?? 0), e.RankingInTier))
                .OrderByDescending(p => p.Data)
                .ToList();
            return historico;
        });

    public Task<DetalhePartida> BuscarPartidaAsync(Regiao regiao, string matchId, CancellationToken ct) =>
        // Partida encerrada não muda mais, então vale um cache bem mais longo que o do perfil.
        EmCacheAsync($"partida:{regiao}:{matchId}", Opcoes.CacheDetalhePartida, async () =>
        {
            var url = $"valorant/v4/match/{Afinidade(regiao)}/{Uri.EscapeDataString(matchId)}";
            var dados = (await ObterAsync<MatchV4Response>(url, "Não encontramos essa partida.", ct))?.Data;

            if (dados is null)
                throw new JogadorNaoEncontradoException("Não encontramos essa partida.");

            return ParaDetalhe(dados);
        });

    // ---- mapeamento ----

    private static ResumoPartida ParaResumo(StoredMatch m)
    {
        var stats = m.Stats!;
        var meuTime = TimeDe(stats.Team);
        var (ganhos, perdidos) = m.Teams is { } t
            ? meuTime == TimePartida.Azul ? (t.Blue, t.Red) : (t.Red, t.Blue)
            : (0, 0);

        return new ResumoPartida(
            m.Meta.Id,
            m.Meta.StartedAt,
            MapaDe(m.Meta.Map),
            AgenteDe(stats.Character),
            ganhos,
            perdidos,
            new EstatisticasPartida(
                stats.Kills, stats.Deaths, stats.Assists,
                stats.Shots?.Head ?? 0, stats.Shots?.Body ?? 0, stats.Shots?.Leg ?? 0,
                stats.Damage?.Made ?? 0, stats.Damage?.Received ?? 0,
                stats.Score));
    }

    private static DetalhePartida ParaDetalhe(MatchV4Data d)
    {
        var jogadores = (d.Players ?? []).Select(p => new JogadorPartida(
            p.Puuid,
            p.Name ?? "",
            p.Tag ?? "",
            AgenteDe(p.Agent),
            TimeDe(p.TeamId),
            (Tier)(p.Tier?.Id ?? 0),
            p.AccountLevel,
            new EstatisticasPartida(
                p.Stats?.Kills ?? 0, p.Stats?.Deaths ?? 0, p.Stats?.Assists ?? 0,
                p.Stats?.Headshots ?? 0, p.Stats?.Bodyshots ?? 0, p.Stats?.Legshots ?? 0,
                p.Stats?.Damage?.Dealt ?? 0, p.Stats?.Damage?.Received ?? 0,
                p.Stats?.Score ?? 0)))
            .ToList();

        var rounds = (d.Rounds ?? [])
            .Select(r => new RoundPartida(r.Id + 1, TimeDe(r.WinningTeam), Desfecho(r.Result)))
            .ToList();

        return new DetalhePartida(
            d.Metadata.MatchId,
            d.Metadata.StartedAt,
            MapaDe(d.Metadata.Map),
            d.Metadata.Queue?.Name,
            TimeSpan.FromMilliseconds(d.Metadata.GameLengthInMs),
            RoundsDoTime(d.Teams, "Blue"),
            RoundsDoTime(d.Teams, "Red"),
            jogadores,
            rounds);
    }

    private static int RoundsDoTime(List<MatchV4Team>? times, string id) =>
        times?.FirstOrDefault(t => string.Equals(t.TeamId, id, StringComparison.OrdinalIgnoreCase))?.Rounds?.Won ?? 0;

    private static TimePartida TimeDe(string? time) =>
        string.Equals(time, "Red", StringComparison.OrdinalIgnoreCase) ? TimePartida.Vermelho : TimePartida.Azul;

    private static Mapa? MapaDe(MatchMap? mapa) =>
        mapa?.Id is { Length: > 0 } id && mapa.Name is { Length: > 0 } nome ? new Mapa(id, nome) : null;

    private static Agente? AgenteDe(MatchCharacter? personagem) =>
        personagem?.Id is { Length: > 0 } id && personagem.Name is { Length: > 0 } nome ? new Agente(id, nome) : null;

    /// <summary>Como o round terminou, em português. Valor desconhecido passa direto.</summary>
    private static string Desfecho(string? resultado) => resultado switch
    {
        "Elimination" => "Eliminação",
        "Bomb detonated" => "Spike detonou",
        "Bomb defused" => "Spike desarmada",
        "Round timer expired" => "Tempo esgotado",
        "Surrendered" => "Rendição",
        null or "" => "—",
        _ => resultado,
    };

    private static string Afinidade(Regiao regiao) => regiao.ToString().ToLowerInvariant();

    // ---- infraestrutura ----

    private async Task<T> EmCacheAsync<T>(string chave, TimeSpan duracao, Func<Task<T>> carregar)
    {
        chave = $"henrikdev:{chave}".ToLowerInvariant();
        if (cache.TryGetValue(chave, out T? emCache)) return emCache!;

        var valor = await carregar();
        cache.Set(chave, valor, duracao);
        return valor;
    }

    /// <summary>GET com tradução de erro: 404 vira "não encontrado", o resto vira "fonte indisponível".</summary>
    private async Task<T?> ObterAsync<T>(string url, string mensagemNaoEncontrado, CancellationToken ct)
    {
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
                    throw new JogadorNaoEncontradoException(mensagemNaoEncontrado);
                case HttpStatusCode.TooManyRequests:
                    throw new FonteIndisponivelException("Muitas consultas seguidas. Tente de novo em alguns instantes.");
                case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                    throw new FonteIndisponivelException("Chave da HenrikDev API ausente ou inválida.");
            }
            if (!resposta.IsSuccessStatusCode)
                throw new FonteIndisponivelException($"HenrikDev API respondeu {(int)resposta.StatusCode}.");

            return await resposta.Content.ReadFromJsonAsync<T>(ct);
        }
    }
}
