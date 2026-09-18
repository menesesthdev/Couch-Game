using CoachGame.Domain.Comum;

namespace CoachGame.Domain.Jogadores;

/// <summary>Rank + RR dentro da divisão, em um instante.</summary>
public sealed record Rank
{
    /// <summary>
    /// RR necessário para subir uma divisão. Simplificação do MVP: do Imortal em diante o jogo usa
    /// RR acumulado e cortes por leaderboard, aqui tratados também como 100 RR por degrau.
    /// </summary>
    public const int RrPorDivisao = 100;

    public Tier Tier { get; }
    public int RR { get; }

    private Rank() { } // EF

    public Rank(Tier tier, int rr)
    {
        if (!tier.Ranqueado()) throw new DomainException("O jogador precisa ter um rank competitivo.");
        if (rr < 0) throw new DomainException("RR não pode ser negativo.");
        Tier = tier;
        RR = rr;
    }

    /// <summary>RR total acumulado desde Ferro 1 com 0 RR — permite comparar ranks diferentes.</summary>
    public int RrAbsoluto => ((int)Tier - (int)Tier.Ferro1) * RrPorDivisao + RR;

    /// <summary>Quanto RR falta para chegar ao início do tier-alvo (0 se já chegou).</summary>
    public int RrAte(Tier alvo) => Math.Max(0, new Rank(alvo, 0).RrAbsoluto - RrAbsoluto);

    public override string ToString() => $"{Tier.NomeExibicao()} — {RR} RR";
}
