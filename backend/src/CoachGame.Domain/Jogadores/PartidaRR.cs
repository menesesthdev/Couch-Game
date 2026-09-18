namespace CoachGame.Domain.Jogadores;

/// <summary>Variação de RR de uma partida competitiva do histórico.</summary>
public sealed record PartidaRR(DateTimeOffset Data, int VariacaoRR)
{
    public bool Vitoria => VariacaoRR > 0;
    public bool Derrota => VariacaoRR < 0;
}
