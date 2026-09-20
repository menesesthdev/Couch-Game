using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Domain.Partidas;

public enum ResultadoPartida { Vitoria, Derrota, Empate }

/// <summary>Recorte de partidas que o jogador escolhe analisar.</summary>
public enum PeriodoAnalise
{
    /// <summary>As mais recentes, independente de ato. É o padrão da tela.</summary>
    Ultimas,

    /// <summary>Tudo que foi jogado dentro do ato em andamento.</summary>
    AtoAtual,
}

/// <summary>Agente jogado. O id é o mesmo do valorant-api.com, usado para montar a URL do ícone.</summary>
public sealed record Agente(string Id, string Nome);

/// <summary>Mapa da partida. O id vem junto pelo mesmo motivo do <see cref="Agente"/>.</summary>
public sealed record Mapa(string Id, string Nome);

/// <summary>
/// Uma partida competitiva vista pelo lado de um jogador só: o que ele jogou, como foi e quanto de RR
/// rendeu. Desempenho e RR vêm de endpoints diferentes e são casados pelo <paramref name="MatchId"/>,
/// por isso os campos de RR são opcionais — nem toda partida tem entrada no histórico de RR.
/// Para o scoreboard completo existe <see cref="DetalhePartida"/>.
/// </summary>
public sealed record ResumoPartida(
    string MatchId,
    DateTimeOffset Data,
    Mapa? Mapa,
    Agente? Agente,
    int RoundsGanhos,
    int RoundsPerdidos,
    EstatisticasPartida Estatisticas,
    int? VariacaoRR = null,
    Tier? TierApos = null,
    int? RrApos = null)
{
    public int Rounds => RoundsGanhos + RoundsPerdidos;

    public ResultadoPartida Resultado => RoundsGanhos > RoundsPerdidos ? ResultadoPartida.Vitoria
        : RoundsGanhos < RoundsPerdidos ? ResultadoPartida.Derrota
        : ResultadoPartida.Empate;

    /// <summary>Casa esta partida com a entrada correspondente do histórico de RR.</summary>
    public ResumoPartida ComRr(PartidaRR rr) =>
        this with { VariacaoRR = rr.VariacaoRR, TierApos = rr.TierApos, RrApos = rr.RrApos };
}
