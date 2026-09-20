namespace ValorantCoach.Domain.Jogadores;

/// <summary>
/// Foto do rank/RR do jogador em um instante, com as médias reais calculadas a partir do histórico
/// recente de partidas. É o que alimenta o "cenário real" da estimativa.
/// </summary>
public class SnapshotRR
{
    public Guid Id { get; private set; }
    public Guid JogadorId { get; private set; }
    public Rank Rank { get; private set; } = null!;
    public DateTimeOffset CapturadoEm { get; private set; }

    public int PartidasAnalisadas { get; private set; }
    public double WinRate { get; private set; }
    public double RrMedioGanho { get; private set; }
    public double RrMedioPerdido { get; private set; }
    public double PartidasPorDia { get; private set; }

    public bool TemHistorico => PartidasAnalisadas > 0 && RrMedioGanho > 0;

    private SnapshotRR() { } // EF

    public static SnapshotRR Capturar(Guid jogadorId, Rank rank, IReadOnlyCollection<PartidaRR> partidas, DateTimeOffset agora)
    {
        var snapshot = new SnapshotRR
        {
            Id = Guid.NewGuid(),
            JogadorId = jogadorId,
            Rank = rank,
            CapturadoEm = agora,
            PartidasAnalisadas = partidas.Count,
        };
        if (partidas.Count == 0) return snapshot;

        var vitorias = partidas.Where(p => p.Vitoria).ToList();
        var derrotas = partidas.Where(p => p.Derrota).ToList();

        snapshot.WinRate = (double)vitorias.Count / partidas.Count;
        snapshot.RrMedioGanho = vitorias.Count > 0 ? vitorias.Average(p => p.VariacaoRR) : 0;
        snapshot.RrMedioPerdido = derrotas.Count > 0 ? derrotas.Average(p => -p.VariacaoRR) : 0;

        // Ritmo = partidas / dias corridos entre a primeira e a última partida (inclusive).
        var primeiro = DateOnly.FromDateTime(partidas.Min(p => p.Data).UtcDateTime);
        var ultimo = DateOnly.FromDateTime(partidas.Max(p => p.Data).UtcDateTime);
        snapshot.PartidasPorDia = (double)partidas.Count / (ultimo.DayNumber - primeiro.DayNumber + 1);

        return snapshot;
    }
}
