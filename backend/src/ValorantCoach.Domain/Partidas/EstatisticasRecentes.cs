namespace ValorantCoach.Domain.Partidas;

public sealed record DesempenhoAgente(Agente Agente, int Partidas, int Vitorias, double Kd)
{
    public double WinRate => Partidas == 0 ? 0 : (double)Vitorias / Partidas;
}

/// <summary>Sequência em andamento: "3 vitórias seguidas". Zero quando não há partidas.</summary>
public sealed record Sequencia(ResultadoPartida Tipo, int Quantidade);

/// <summary>
/// Resumo das partidas analisadas: o cartão de visitas do jogador (KD, headshot, dano e pontuação por
/// round) mais os agentes mais jogados. Só agregação — nada de previsão aqui.
/// </summary>
public sealed record EstatisticasRecentes(
    int PartidasAnalisadas,
    int Vitorias,
    int Derrotas,
    int Empates,
    double WinRate,
    double Kd,
    double TaxaHeadshot,
    double DanoPorRound,
    double PontuacaoPorRound,
    double AbatesPorPartida,
    double MortesPorPartida,
    Sequencia? Sequencia,
    IReadOnlyList<DesempenhoAgente> TopAgentes)
{
    public static readonly EstatisticasRecentes Vazia =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, []);

    /// <param name="top">Quantos agentes trazer — a UI mostra poucos, de propósito.</param>
    public static EstatisticasRecentes Calcular(IReadOnlyList<ResumoPartida> partidas, int top = 3)
    {
        if (partidas.Count == 0) return Vazia;

        // As médias por round usam o total de rounds do período: uma partida de 25 rounds
        // não pode pesar igual a uma de 13 no dano por round.
        var rounds = partidas.Sum(p => p.Rounds);
        var total = EstatisticasPartida.Somar(partidas.Select(p => p.Estatisticas));
        var vitorias = partidas.Count(p => p.Resultado == ResultadoPartida.Vitoria);
        var derrotas = partidas.Count(p => p.Resultado == ResultadoPartida.Derrota);

        return new EstatisticasRecentes(
            partidas.Count,
            vitorias,
            derrotas,
            partidas.Count - vitorias - derrotas,
            (double)vitorias / partidas.Count,
            total.Kd,
            total.TaxaHeadshot,
            total.DanoPorRound(rounds),
            total.PontuacaoPorRound(rounds),
            (double)total.Abates / partidas.Count,
            (double)total.Mortes / partidas.Count,
            SequenciaDe(partidas),
            TopAgentesDe(partidas, top));
    }

    /// <summary>Conta quantas partidas seguidas terminaram igual, a partir da mais recente.</summary>
    private static Sequencia? SequenciaDe(IReadOnlyList<ResumoPartida> partidas)
    {
        var maisRecentes = partidas.OrderByDescending(p => p.Data).ToList();
        var tipo = maisRecentes[0].Resultado;
        if (tipo == ResultadoPartida.Empate) return null;

        return new Sequencia(tipo, maisRecentes.TakeWhile(p => p.Resultado == tipo).Count());
    }

    private static List<DesempenhoAgente> TopAgentesDe(IReadOnlyList<ResumoPartida> partidas, int top) =>
        partidas.Where(p => p.Agente is not null)
            .GroupBy(p => p.Agente!.Id)
            .Select(g => new DesempenhoAgente(
                g.First().Agente!,
                g.Count(),
                g.Count(p => p.Resultado == ResultadoPartida.Vitoria),
                EstatisticasPartida.Somar(g.Select(p => p.Estatisticas)).Kd))
            .OrderByDescending(a => a.Partidas).ThenByDescending(a => a.WinRate)
            .Take(top)
            .ToList();
}
