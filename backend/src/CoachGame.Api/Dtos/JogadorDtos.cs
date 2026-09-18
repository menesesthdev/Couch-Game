using CoachGame.Application.Jogadores;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Api.Dtos;

public sealed record RankDto(Tier Tier, string Nome, int RR)
{
    public static RankDto De(Rank r) => new(r.Tier, r.Tier.NomeExibicao(), r.RR);
    public static RankDto De(Tier t) => new(t, t.NomeExibicao(), 0);
}

public sealed record SugestaoJogadorDto(string RiotId, string Nome, string Tag, Regiao Regiao, RankDto Rank)
{
    public static SugestaoJogadorDto De(Jogador j) =>
        new(j.RiotId.ToString(), j.RiotId.Nome, j.RiotId.Tag, j.Regiao, RankDto.De(j.RankAtual));
}

public sealed record DesempenhoDto(
    int PartidasAnalisadas, int Vitorias, int Derrotas, int SaldoRR,
    double WinRate, double RrMedioGanho, double RrMedioPerdido, double PartidasPorDia);

public enum ResultadoPartida { Vitoria, Derrota, Empate }

public sealed record PartidaDto(DateTimeOffset Data, string? Mapa, int VariacaoRR, ResultadoPartida Resultado, RankDto? RankApos);

public sealed record PerfilResponse(
    string RiotId,
    string Nome,
    string Tag,
    Regiao Regiao,
    RankDto RankAtual,
    double ProgressoDivisao,
    int RrParaProximaDivisao,
    DesempenhoDto Desempenho,
    IReadOnlyList<PartidaDto> Partidas,
    DateTimeOffset AtualizadoEm)
{
    public static PerfilResponse De(PerfilConsultado c)
    {
        var (perfil, snapshot) = (c.Perfil, c.Snapshot);
        var partidas = perfil.HistoricoRecente;
        return new PerfilResponse(
            perfil.RiotId.ToString(), perfil.RiotId.Nome, perfil.RiotId.Tag, c.Regiao,
            RankDto.De(perfil.RankAtual),
            Math.Round(perfil.RankAtual.ProgressoDivisao, 3),
            perfil.RankAtual.RrParaProximaDivisao,
            new DesempenhoDto(
                snapshot.PartidasAnalisadas,
                partidas.Count(p => p.Vitoria),
                partidas.Count(p => p.Derrota),
                partidas.Sum(p => p.VariacaoRR),
                Math.Round(snapshot.WinRate, 3),
                Math.Round(snapshot.RrMedioGanho, 1),
                Math.Round(snapshot.RrMedioPerdido, 1),
                Math.Round(snapshot.PartidasPorDia, 1)),
            partidas.Select(p => new PartidaDto(
                p.Data, p.Mapa, p.VariacaoRR,
                p.Vitoria ? ResultadoPartida.Vitoria : p.Derrota ? ResultadoPartida.Derrota : ResultadoPartida.Empate,
                p.TierApos is { } t && t.Ranqueado() ? new RankDto(t, t.NomeExibicao(), p.RrApos ?? 0) : null)).ToList(),
            snapshot.CapturadoEm);
    }
}
