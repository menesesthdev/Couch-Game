namespace CoachGame.Domain.Jogadores;

/// <summary>Variação de RR de uma partida competitiva do histórico, com o rank logo após ela.</summary>
public sealed record PartidaRR(
    DateTimeOffset Data,
    int VariacaoRR,
    string? Mapa = null,
    Tier? TierApos = null,
    int? RrApos = null)
{
    public bool Vitoria => VariacaoRR > 0;
    public bool Derrota => VariacaoRR < 0;
}
