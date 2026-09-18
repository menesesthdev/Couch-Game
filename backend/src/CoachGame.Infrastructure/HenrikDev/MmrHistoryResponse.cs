using System.Text.Json.Serialization;

namespace CoachGame.Infrastructure.HenrikDev;

// Contrato de GET /valorant/v2/mmr-history/{region}/{platform}/{name}/{tag}.
// Só os campos usados; conferir https://docs.henrikdev.xyz se a API mudar de versão.

internal sealed record MmrHistoryResponse(MmrHistoryData? Data);

internal sealed record MmrHistoryData(MmrAccount Account, List<MmrHistoryEntry> History);

internal sealed record MmrAccount(string Name, string Tag, string Puuid);

internal sealed record MmrHistoryEntry(
    MmrTier Tier,
    MmrMap? Map,
    int Rr,
    [property: JsonPropertyName("last_change")] int LastChange,
    DateTimeOffset Date);

internal sealed record MmrTier(int Id, string Name);

internal sealed record MmrMap(string? Name);
