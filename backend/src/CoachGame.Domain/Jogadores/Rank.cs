using CoachGame.Domain.Comum;

namespace CoachGame.Domain.Jogadores;

/// <summary>Rank + RR, em um instante.</summary>
/// <remarks>
/// Até Ascendente 3 o RR vai de 0 a 99 dentro de cada divisão. Do Imortal 1 em diante o jogo
/// acumula o RR (ex.: Imortal 2 com 156 RR, Radiante com 816 RR) e os cortes de Imortal 2,
/// Imortal 3 e Radiante dependem do leaderboard da região. Simplificação do MVP: esses cortes são
/// tratados como 100, 200 e 300 RR acima do início do Imortal 1.
/// </remarks>
public sealed record Rank
{
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

    /// <summary>RR acumulado desde Ferro 1 com 0 RR necessário para entrar no tier.</summary>
    public static int InicioDoTier(Tier tier) => ((int)tier - (int)Tier.Ferro1) * RrPorDivisao;

    /// <summary>RR total acumulado desde Ferro 1 com 0 RR — permite comparar ranks diferentes.</summary>
    public int RrAbsoluto => Tier >= Tier.Imortal1
        ? InicioDoTier(Tier.Imortal1) + RR
        : InicioDoTier(Tier) + RR;

    /// <summary>Quanto RR falta para chegar ao início do tier-alvo (0 se já chegou).</summary>
    public int RrAte(Tier alvo) => Math.Max(0, InicioDoTier(alvo) - RrAbsoluto);

    /// <summary>RR que falta para a próxima divisão (0 em Radiante).</summary>
    public int RrParaProximaDivisao => Tier == Tier.Radiante ? 0 : RrAte(Tier + 1);

    /// <summary>Fração da divisão atual já completada (0 a 1).</summary>
    public double ProgressoDivisao => Tier == Tier.Radiante
        ? 1
        : Math.Clamp(1 - (double)RrParaProximaDivisao / RrPorDivisao, 0, 1);

    /// <summary>
    /// Cada divisão entre o rank atual e o alvo, com o RR que falta para completá-la.
    /// Ex.: Platina 3 (65 RR) → Diamante 2 = [Platina 3: 35, Diamante 1: 100].
    /// </summary>
    public IReadOnlyList<(Tier Tier, int RrFaltando)> DegrausAte(Tier alvo)
    {
        var degraus = new List<(Tier, int)>();
        for (var t = Tier; t < alvo; t++)
        {
            var faltando = InicioDoTier(t + 1) - Math.Max(RrAbsoluto, InicioDoTier(t));
            if (faltando > 0) degraus.Add((t, faltando));
        }
        return degraus;
    }

    public override string ToString() => $"{Tier.NomeExibicao()} — {RR} RR";
}
