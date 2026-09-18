namespace CoachGame.Domain.Estimativas;

public enum TipoCenario { TresVitorias, DuasVitorias, UmaVitoria, Real }

/// <summary>Resultado do cálculo para um cenário de desempenho. É uma estimativa, não uma previsão.</summary>
public sealed record CenarioEstimativa(
    TipoCenario Tipo,
    string Descricao,
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
