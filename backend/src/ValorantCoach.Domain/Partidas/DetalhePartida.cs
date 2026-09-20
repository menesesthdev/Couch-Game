using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Domain.Partidas;

public enum TimePartida { Azul, Vermelho }

/// <summary>
/// Um dos dez jogadores do scoreboard. Nome e tag ficam como texto solto, e não como
/// <see cref="RiotId"/>: a fonte devolve vazio para quem está anonimizado.
/// </summary>
public sealed record JogadorPartida(
    string Puuid,
    string Nome,
    string Tag,
    Agente? Agente,
    TimePartida Time,
    Tier Tier,
    int Nivel,
    EstatisticasPartida Estatisticas)
{
    public bool Anonimo => string.IsNullOrWhiteSpace(Nome);

    public string NomeExibicao => Anonimo ? "Jogador oculto" : $"{Nome}#{Tag}";
}

/// <summary>Como um round terminou — só o suficiente para desenhar a faixa de rounds.</summary>
public sealed record RoundPartida(int Numero, TimePartida Vencedor, string Desfecho);

/// <summary>
/// Partida completa, com os dois times. Partida encerrada não muda mais, então pode ficar em cache por horas.
/// </summary>
public sealed record DetalhePartida(
    string MatchId,
    DateTimeOffset Data,
    Mapa? Mapa,
    string? Modo,
    TimeSpan Duracao,
    int RoundsAzul,
    int RoundsVermelho,
    IReadOnlyList<JogadorPartida> Jogadores,
    IReadOnlyList<RoundPartida> Rounds)
{
    public int TotalRounds => RoundsAzul + RoundsVermelho;

    public IEnumerable<JogadorPartida> Time(TimePartida time) =>
        Jogadores.Where(j => j.Time == time)
            .OrderByDescending(j => j.Estatisticas.Pontuacao);

    public int RoundsDoTime(TimePartida time) => time == TimePartida.Azul ? RoundsAzul : RoundsVermelho;

    public ResultadoPartida ResultadoDe(TimePartida time) =>
        RoundsAzul == RoundsVermelho ? ResultadoPartida.Empate
        : RoundsDoTime(time) > RoundsDoTime(Oposto(time)) ? ResultadoPartida.Vitoria
        : ResultadoPartida.Derrota;

    private static TimePartida Oposto(TimePartida time) =>
        time == TimePartida.Azul ? TimePartida.Vermelho : TimePartida.Azul;
}
