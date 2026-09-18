namespace CoachGame.Domain.Estimativas;

/// <summary>
/// <see cref="Real"/>: desempenho atual do jogador. <see cref="Simulacao"/>: "e se você vencer X%?",
/// no mesmo ritmo de partidas por dia — sempre com win rates em que o jogador sobe.
/// </summary>
public enum TipoCenario { Real, Simulacao }

/// <summary>Resultado do cálculo para um cenário de desempenho. É uma estimativa, não uma previsão.</summary>
public sealed record CenarioEstimativa(
    TipoCenario Tipo,
    string Descricao,
    double WinRate,
    double PartidasPorDia,
    double RrMedioPorPartida,
    int? PartidasNecessarias,
    int? DiasEstimados,
    DateOnly? DataEstimada,
    bool DentroDoPrazo)
{
    /// <summary>Falso quando o saldo de RR por partida é zero ou negativo — nesse ritmo o jogador não sobe.</summary>
    public bool Alcancavel => PartidasNecessarias is not null;
}
