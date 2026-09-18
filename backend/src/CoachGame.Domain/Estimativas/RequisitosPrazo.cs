namespace CoachGame.Domain.Estimativas;

/// <summary>
/// O que o jogador precisaria fazer para bater a meta exatamente no prazo, mantendo o resto do
/// desempenho real. Complementa os cenários: em vez de "quando chego?", responde "o que preciso?".
/// </summary>
/// <param name="DiasAtePrazo">Dias corridos de hoje até a data-limite.</param>
/// <param name="PartidasNecessarias">Partidas no seu RR médio real (nulo se o saldo real é negativo).</param>
/// <param name="PartidasPorDiaNecessarias">Ritmo diário necessário mantendo o win rate real.</param>
/// <param name="WinRateNecessario">
/// Win rate necessário mantendo o ritmo real de partidas/dia. Acima de 1 significa que nem vencendo
/// todas dá tempo nesse ritmo.
/// </param>
public sealed record RequisitosPrazo(
    int DiasAtePrazo,
    int? PartidasNecessarias,
    double? PartidasPorDiaNecessarias,
    double? WinRateNecessario);
