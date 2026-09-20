namespace ValorantCoach.Domain.Partidas;

/// <summary>Números brutos de um jogador em uma partida. As médias por round dependem de quantos rounds ela teve.</summary>
public sealed record EstatisticasPartida(
    int Abates,
    int Mortes,
    int Assistencias,
    int TirosCabeca,
    int TirosCorpo,
    int TirosPerna,
    int DanoFeito,
    int DanoRecebido,
    int Pontuacao)
{
    public static readonly EstatisticasPartida Zerada = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public int TirosCertos => TirosCabeca + TirosCorpo + TirosPerna;

    /// <summary>Taxa de headshot como o tracker mostra: tiros na cabeça sobre todos os tiros que acertaram.</summary>
    public double TaxaHeadshot => TirosCertos == 0 ? 0 : (double)TirosCabeca / TirosCertos;

    /// <summary>Sem mortes o KD é o próprio número de abates (evita divisão por zero).</summary>
    public double Kd => Mortes == 0 ? Abates : (double)Abates / Mortes;

    public double DanoPorRound(int rounds) => rounds <= 0 ? 0 : (double)DanoFeito / rounds;

    /// <summary>ACS: pontuação de combate média por round.</summary>
    public double PontuacaoPorRound(int rounds) => rounds <= 0 ? 0 : (double)Pontuacao / rounds;

    public static EstatisticasPartida Somar(IEnumerable<EstatisticasPartida> estatisticas) =>
        estatisticas.Aggregate(Zerada, (a, b) => new EstatisticasPartida(
            a.Abates + b.Abates,
            a.Mortes + b.Mortes,
            a.Assistencias + b.Assistencias,
            a.TirosCabeca + b.TirosCabeca,
            a.TirosCorpo + b.TirosCorpo,
            a.TirosPerna + b.TirosPerna,
            a.DanoFeito + b.DanoFeito,
            a.DanoRecebido + b.DanoRecebido,
            a.Pontuacao + b.Pontuacao));
}
