using CoachGame.Domain.Jogadores;

namespace CoachGame.Domain.Estimativas;

/// <summary>
/// Regra central: RR que falta ÷ RR médio por partida → partidas; partidas ÷ ritmo diário → dias.
/// Aritmética simples, sem estatística — o resultado é sempre uma estimativa.
/// </summary>
public static class CalculadoraEstimativa
{
    /// <summary>Médias usadas quando o jogador não tem histórico recente suficiente.</summary>
    public const double RrGanhoPadrao = 20;
    public const double RrPerdidoPadrao = 18;

    public static IReadOnlyList<CenarioEstimativa> Calcular(Rank atual, Meta meta, SnapshotRR snapshot, DateOnly hoje)
    {
        var rrFaltando = atual.RrAte(meta.RankAlvo);
        var ganho = snapshot.TemHistorico ? snapshot.RrMedioGanho : RrGanhoPadrao;
        var perda = snapshot.TemHistorico && snapshot.RrMedioPerdido > 0 ? snapshot.RrMedioPerdido : RrPerdidoPadrao;

        var cenarios = new List<CenarioEstimativa>
        {
            PorDia(TipoCenario.TresVitorias, 3, 0),
            PorDia(TipoCenario.DuasVitorias, 2, 1),
            PorDia(TipoCenario.UmaVitoria, 1, 2),
        };

        if (snapshot.TemHistorico && snapshot.PartidasPorDia > 0)
        {
            var rrPorPartida = snapshot.WinRate * ganho - (1 - snapshot.WinRate) * perda;
            cenarios.Add(Montar(TipoCenario.Real,
                $"Seu ritmo real: {snapshot.WinRate:P0} de vitórias, ~{snapshot.PartidasPorDia:0.#} partidas/dia",
                snapshot.PartidasPorDia, rrPorPartida, rrFaltando, meta, hoje));
        }

        return cenarios;

        CenarioEstimativa PorDia(TipoCenario tipo, int vitorias, int derrotas)
        {
            var partidas = vitorias + derrotas;
            var rrPorPartida = (vitorias * ganho - derrotas * perda) / partidas;
            return Montar(tipo, $"{vitorias}V / {derrotas}D por dia", partidas, rrPorPartida, rrFaltando, meta, hoje);
        }
    }

    private static CenarioEstimativa Montar(
        TipoCenario tipo, string descricao, double partidasPorDia, double rrPorPartida,
        int rrFaltando, Meta meta, DateOnly hoje)
    {
        if (rrFaltando == 0)
            return new(tipo, descricao, partidasPorDia, rrPorPartida, 0, 0, hoje, true);

        if (rrPorPartida <= 0)
            return new(tipo, descricao, partidasPorDia, rrPorPartida, null, null, null, false);

        var partidas = (int)Math.Ceiling(rrFaltando / rrPorPartida);
        var dias = (int)Math.Ceiling(partidas / partidasPorDia);
        var data = hoje.AddDays(dias);
        return new(tipo, descricao, partidasPorDia, Math.Round(rrPorPartida, 1), partidas, dias, data, data <= meta.DataLimite);
    }
}
