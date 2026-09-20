using System.Text.Json.Serialization;

namespace ValorantCoach.Infrastructure.HenrikDev;

// Contratos de GET /valorant/v1/stored-matches/{region}/{name}/{tag} e
// GET /valorant/v4/match/{region}/{matchid}. Só os campos usados; conferir
// https://docs.henrikdev.xyz se a API mudar de versão.

internal sealed record StoredMatchesResponse(List<StoredMatch>? Data);

internal sealed record StoredMatch(StoredMatchMeta Meta, StoredMatchStats? Stats, StoredMatchTeams? Teams);

internal sealed record StoredMatchMeta(
    string Id,
    MatchMap? Map,
    string? Mode,
    [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt);

internal sealed record StoredMatchStats(
    string? Team,
    MatchCharacter? Character,
    int Kills,
    int Deaths,
    int Assists,
    int Score,
    MatchShots? Shots,
    MatchDamage? Damage);

internal sealed record MatchShots(int Head, int Body, int Leg);

internal sealed record MatchDamage(int Made, int Received);

/// <summary>Rounds ganhos por cada lado — o time do jogador vem em <c>stats.team</c>.</summary>
internal sealed record StoredMatchTeams(int Red, int Blue);

internal sealed record MatchMap(string? Id, string? Name);

internal sealed record MatchCharacter(string? Id, string? Name);

// ---- v1/stored-mmr-history ----

internal sealed record StoredMmrHistoryResponse(List<StoredMmrEntry>? Data);

internal sealed record StoredMmrEntry(
    [property: JsonPropertyName("match_id")] string? MatchId,
    MmrTier? Tier,
    MatchMap? Map,
    [property: JsonPropertyName("ranking_in_tier")] int RankingInTier,
    [property: JsonPropertyName("last_mmr_change")] int LastMmrChange,
    DateTimeOffset Date);

// ---- v4/match ----

internal sealed record MatchV4Response(MatchV4Data? Data);

internal sealed record MatchV4Data(
    MatchV4Metadata Metadata,
    List<MatchV4Player>? Players,
    List<MatchV4Team>? Teams,
    List<MatchV4Round>? Rounds);

internal sealed record MatchV4Metadata(
    [property: JsonPropertyName("match_id")] string MatchId,
    MatchMap? Map,
    [property: JsonPropertyName("game_length_in_ms")] long GameLengthInMs,
    [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt,
    MatchV4Queue? Queue);

internal sealed record MatchV4Queue(string? Id, string? Name);

internal sealed record MatchV4Player(
    string Puuid,
    string? Name,
    string? Tag,
    [property: JsonPropertyName("team_id")] string? TeamId,
    MatchCharacter? Agent,
    MatchV4PlayerStats? Stats,
    MatchV4Tier? Tier,
    [property: JsonPropertyName("account_level")] int AccountLevel);

internal sealed record MatchV4PlayerStats(
    int Score,
    int Kills,
    int Deaths,
    int Assists,
    int Headshots,
    int Bodyshots,
    int Legshots,
    MatchV4Damage? Damage);

internal sealed record MatchV4Damage(int Dealt, int Received);

internal sealed record MatchV4Tier(int Id, string? Name);

internal sealed record MatchV4Team(
    [property: JsonPropertyName("team_id")] string? TeamId,
    MatchV4TeamRounds? Rounds);

internal sealed record MatchV4TeamRounds(int Won, int Lost);

internal sealed record MatchV4Round(
    int Id,
    string? Result,
    [property: JsonPropertyName("winning_team")] string? WinningTeam);
