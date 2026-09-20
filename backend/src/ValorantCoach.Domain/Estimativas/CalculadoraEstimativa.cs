using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Domain.Estimativas;

/// <summary>
/// Regra central: RR que falta ÷ RR médio por partida → partidas; partidas ÷ ritmo diário → dias.
/// Aritmética simples, sem estatística — o resultado é sempre uma estimativa.
/// </summary>
public static class CalculadoraEstimativa
{
    /// <summary>Médias usadas quando o jogador não tem histórico recente suficiente.</summary>
    public const double RrGanhoPadrao = 20;
    public const double RrPerdidoPadrao = 18;

    /// <summary>Ritmo usado nas simulações quando não há histórico recente.</summary>
    public const double PartidasPorDiaPadrao = 3;

    private const int QuantidadeSimulacoes = 3;

    public static IReadOnlyList<CenarioEstimativa> Calcular(Rank atual, Meta meta, SnapshotRR snapshot, DateOnly hoje)
    {
        var rrFaltando = atual.RrAte(meta.RankAlvo);
        var (ganho, perda) = Medias(snapshot);
        var ritmo = Ritmo(snapshot);
        var cenarios = new List<CenarioEstimativa>();

        if (snapshot.TemHistorico && snapshot.PartidasPorDia > 0)
        {
            cenarios.Add(Montar(TipoCenario.Real,
                $"Seu ritmo real: {snapshot.WinRate * 100:0}% de vitórias, ~{snapshot.PartidasPorDia:0.#} partidas/dia",
                snapshot.WinRate, ritmo, SaldoPorPartida(snapshot.WinRate, ganho, perda), rrFaltando, meta, hoje));
        }

        // Simulações "e se você vencer mais?": começam no primeiro múltiplo de 5% acima tanto do
        // win rate atual quanto do ponto de equilíbrio (onde o saldo de RR é zero), então todas sobem.
        var equilibrio = perda / (ganho + perda);
        var piso = Math.Max(equilibrio, snapshot.TemHistorico ? snapshot.WinRate : 0);
        for (var pct = (int)Math.Floor(piso * 100 / 5) * 5 + 5; pct <= 100 && cenarios.Count(c => c.Tipo == TipoCenario.Simulacao) < QuantidadeSimulacoes; pct += 5)
        {
            var winRate = pct / 100.0;
            cenarios.Add(Montar(TipoCenario.Simulacao, $"{pct}% de vitórias",
                winRate, ritmo, SaldoPorPartida(winRate, ganho, perda), rrFaltando, meta, hoje));
        }

        return cenarios;
    }

    private static (double Ganho, double Perda) Medias(SnapshotRR snapshot) => (
        snapshot.TemHistorico ? snapshot.RrMedioGanho : RrGanhoPadrao,
        snapshot.TemHistorico && snapshot.RrMedioPerdido > 0 ? snapshot.RrMedioPerdido : RrPerdidoPadrao);

    private static double Ritmo(SnapshotRR snapshot) =>
        snapshot.TemHistorico && snapshot.PartidasPorDia > 0 ? snapshot.PartidasPorDia : PartidasPorDiaPadrao;

    private static double SaldoPorPartida(double winRate, double ganho, double perda) =>
        winRate * ganho - (1 - winRate) * perda;

    public static RequisitosPrazo Requisitos(Rank atual, Meta meta, SnapshotRR snapshot, DateOnly hoje)
    {
        var dias = Math.Max(1, meta.DataLimite.DayNumber - hoje.DayNumber);
        var rrFaltando = atual.RrAte(meta.RankAlvo);
        if (rrFaltando == 0) return new RequisitosPrazo(dias, 0, 0, 0);

        var (ganho, perda) = Medias(snapshot);

        int? partidas = null;
        double? partidasPorDia = null;
        if (snapshot.TemHistorico)
        {
            var rrPorPartida = SaldoPorPartida(snapshot.WinRate, ganho, perda);
            if (rrPorPartida > 0)
            {
                partidas = (int)Math.Ceiling(rrFaltando / rrPorPartida);
                partidasPorDia = Math.Round((double)partidas.Value / dias, 1);
            }
        }

        // Com o ritmo fixo, qual win rate w satisfaz: w·ganho − (1−w)·perda = RR necessário por partida?
        var ritmo = Ritmo(snapshot);
        var rrPorPartidaNecessario = rrFaltando / (ritmo * dias);
        var winRate = Math.Max(0, (rrPorPartidaNecessario + perda) / (ganho + perda));

        return new RequisitosPrazo(dias, partidas, partidasPorDia, Math.Round(winRate, 3));
    }

    private static CenarioEstimativa Montar(
        TipoCenario tipo, string descricao, double winRate, double partidasPorDia, double rrPorPartida,
        int rrFaltando, Meta meta, DateOnly hoje)
    {
        var saldo = Math.Round(rrPorPartida, 1);
        if (rrFaltando == 0)
            return new(tipo, descricao, winRate, partidasPorDia, saldo, 0, 0, hoje, true);

        if (rrPorPartida <= 0)
            return new(tipo, descricao, winRate, partidasPorDia, saldo, null, null, null, false);

        var partidas = (int)Math.Ceiling(rrFaltando / rrPorPartida);
        var dias = (int)Math.Ceiling(partidas / partidasPorDia);
        var data = hoje.AddDays(dias);
        return new(tipo, descricao, winRate, partidasPorDia, saldo, partidas, dias, data, data <= meta.DataLimite);
    }
}
