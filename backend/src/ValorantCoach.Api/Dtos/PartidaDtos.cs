using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Application.Partidas;
using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Api.Dtos;

public sealed record AgenteDto(string Id, string Nome)
{
    public static AgenteDto? De(Agente? a) => a is null ? null : new AgenteDto(a.Id, a.Nome);
}

public sealed record MapaDto(string Id, string Nome)
{
    public static MapaDto? De(Mapa? m) => m is null ? null : new MapaDto(m.Id, m.Nome);
}

/// <summary>Números de um jogador já prontos para exibição: as médias por round vêm calculadas.</summary>
public sealed record EstatisticasPartidaDto(
    int Abates, int Mortes, int Assistencias,
    double Kd, double TaxaHeadshot, int DanoFeito, int DanoRecebido,
    int Pontuacao, double DanoPorRound, double PontuacaoPorRound)
{
    public static EstatisticasPartidaDto De(EstatisticasPartida e, int rounds) => new(
        e.Abates, e.Mortes, e.Assistencias,
        Math.Round(e.Kd, 2), Math.Round(e.TaxaHeadshot, 3),
        e.DanoFeito, e.DanoRecebido, e.Pontuacao,
        Math.Round(e.DanoPorRound(rounds), 0), Math.Round(e.PontuacaoPorRound(rounds), 0));
}

public sealed record DesempenhoAgenteDto(AgenteDto Agente, int Partidas, int Vitorias, double WinRate, double Kd)
{
    public static DesempenhoAgenteDto De(DesempenhoAgente d) =>
        new(AgenteDto.De(d.Agente)!, d.Partidas, d.Vitorias, Math.Round(d.WinRate, 3), Math.Round(d.Kd, 2));
}

public sealed record SequenciaDto(ResultadoPartida Tipo, int Quantidade);

public sealed record EstatisticasRecentesResponse(
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
    SequenciaDto? Sequencia,
    IReadOnlyList<DesempenhoAgenteDto> TopAgentes)
{
    public static EstatisticasRecentesResponse De(EstatisticasRecentes e) => new(
        e.PartidasAnalisadas, e.Vitorias, e.Derrotas, e.Empates,
        Math.Round(e.WinRate, 3),
        Math.Round(e.Kd, 2),
        Math.Round(e.TaxaHeadshot, 3),
        Math.Round(e.DanoPorRound, 0),
        Math.Round(e.PontuacaoPorRound, 0),
        Math.Round(e.AbatesPorPartida, 1),
        Math.Round(e.MortesPorPartida, 1),
        e.Sequencia is { } s ? new SequenciaDto(s.Tipo, s.Quantidade) : null,
        e.TopAgentes.Select(DesempenhoAgenteDto.De).ToList());
}

/// <summary>Linha da lista de partidas: desempenho e RR já casados.</summary>
public sealed record PartidaResumoDto(
    string MatchId,
    DateTimeOffset Data,
    MapaDto? Mapa,
    AgenteDto? Agente,
    ResultadoPartida Resultado,
    int RoundsGanhos,
    int RoundsPerdidos,
    int? VariacaoRR,
    RankDto? RankApos,
    EstatisticasPartidaDto Estatisticas)
{
    public static PartidaResumoDto De(ResumoPartida p) => new(
        p.MatchId, p.Data, MapaDto.De(p.Mapa), AgenteDto.De(p.Agente),
        p.Resultado, p.RoundsGanhos, p.RoundsPerdidos,
        p.VariacaoRR,
        p.TierApos is { } t && t.Ranqueado() ? new RankDto(t, t.NomeExibicao(), p.RrApos ?? 0) : null,
        EstatisticasPartidaDto.De(p.Estatisticas, p.Rounds));
}

public sealed record AtoDto(string Nome, DateTimeOffset Inicio, DateTimeOffset Fim);

public sealed record DesempenhoResponse(
    PeriodoAnalise Periodo,
    AtoDto? Ato,
    EstatisticasRecentesResponse Estatisticas,
    IReadOnlyList<PartidaResumoDto> Partidas)
{
    public static DesempenhoResponse De(DesempenhoAnalisado d) => new(
        d.Periodo,
        d.Ato is { } a ? new AtoDto(a.Nome, a.Inicio, a.Fim) : null,
        EstatisticasRecentesResponse.De(d.Estatisticas),
        d.Partidas.Select(PartidaResumoDto.De).ToList());
}

public sealed record JogadorPartidaDto(
    string Puuid,
    string RiotId,
    string Nome,
    string Tag,
    bool Anonimo,
    AgenteDto? Agente,
    TimePartida Time,
    RankDto Rank,
    int Nivel,
    EstatisticasPartidaDto Estatisticas)
{
    public static JogadorPartidaDto De(JogadorPartida j, int rounds) => new(
        j.Puuid, j.NomeExibicao, j.Nome, j.Tag, j.Anonimo,
        AgenteDto.De(j.Agente), j.Time, RankDto.De(j.Tier), j.Nivel,
        EstatisticasPartidaDto.De(j.Estatisticas, rounds));
}

public sealed record TimeDto(TimePartida Time, int Rounds, ResultadoPartida Resultado, IReadOnlyList<JogadorPartidaDto> Jogadores);

public sealed record RoundDto(int Numero, TimePartida Vencedor, string Desfecho);

public sealed record DetalhePartidaResponse(
    string MatchId,
    DateTimeOffset Data,
    MapaDto? Mapa,
    string? Modo,
    int DuracaoEmMinutos,
    int TotalRounds,
    IReadOnlyList<TimeDto> Times,
    IReadOnlyList<RoundDto> Rounds)
{
    public static DetalhePartidaResponse De(DetalhePartida p)
    {
        var rounds = p.TotalRounds;
        TimeDto Time(TimePartida t) => new(
            t, p.RoundsDoTime(t), p.ResultadoDe(t),
            p.Time(t).Select(j => JogadorPartidaDto.De(j, rounds)).ToList());

        return new DetalhePartidaResponse(
            p.MatchId, p.Data, MapaDto.De(p.Mapa), p.Modo,
            (int)Math.Round(p.Duracao.TotalMinutes), rounds,
            [Time(TimePartida.Azul), Time(TimePartida.Vermelho)],
            p.Rounds.Select(r => new RoundDto(r.Numero, r.Vencedor, r.Desfecho)).ToList());
    }
}
