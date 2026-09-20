using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Domain.Jogadores;

/// <summary>
/// Variação de RR de uma partida competitiva do histórico, com o rank logo após ela.
/// O <paramref name="MatchId"/> é o que liga a linha do histórico ao scoreboard completo da partida.
/// </summary>
public sealed record PartidaRR(
    DateTimeOffset Data,
    int VariacaoRR,
    string? MatchId = null,
    Mapa? Mapa = null,
    Tier? TierApos = null,
    int? RrApos = null)
{
    public bool Vitoria => VariacaoRR > 0;
    public bool Derrota => VariacaoRR < 0;
}
