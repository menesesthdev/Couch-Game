namespace CoachGame.Domain.Jogadores;

/// <summary>
/// Tiers competitivos do Valorant. Os valores numéricos seguem os IDs usados pela HenrikDev API
/// (0 = sem rank, 3 = Ferro 1 ... 27 = Radiante).
/// </summary>
public enum Tier
{
    SemRank = 0,
    Ferro1 = 3, Ferro2, Ferro3,
    Bronze1, Bronze2, Bronze3,
    Prata1, Prata2, Prata3,
    Ouro1, Ouro2, Ouro3,
    Platina1, Platina2, Platina3,
    Diamante1, Diamante2, Diamante3,
    Ascendente1, Ascendente2, Ascendente3,
    Imortal1, Imortal2, Imortal3,
    Radiante
}

public static class TierExtensions
{
    private static readonly string[] Nomes =
        ["Ferro", "Bronze", "Prata", "Ouro", "Platina", "Diamante", "Ascendente", "Imortal"];

    public static bool Ranqueado(this Tier tier) => tier >= Tier.Ferro1 && tier <= Tier.Radiante;

    public static string NomeExibicao(this Tier tier)
    {
        if (tier == Tier.SemRank) return "Sem rank";
        if (tier == Tier.Radiante) return "Radiante";
        var indice = (int)tier - (int)Tier.Ferro1;
        return $"{Nomes[indice / 3]} {indice % 3 + 1}";
    }
}
