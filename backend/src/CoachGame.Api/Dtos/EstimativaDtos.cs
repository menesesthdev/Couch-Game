using System.ComponentModel.DataAnnotations;
using CoachGame.Application.Estimativas;
using CoachGame.Domain.Estimativas;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Api.Dtos;

public sealed record EstimativaRequest(
    [Required] string Perfil,
    [Required] Regiao Regiao,
    [Required] Tier RankAlvo,
    [Required] DateOnly DataLimite);

public sealed record RankDto(Tier Tier, string Nome, int RR);

public sealed record DesempenhoDto(int PartidasAnalisadas, double WinRate, double RrMedioGanho, double RrMedioPerdido, double PartidasPorDia);

public sealed record CenarioDto(
    TipoCenario Tipo, string Descricao, double PartidasPorDia, double RrMedioPorPartida,
    bool Alcancavel, int? PartidasNecessarias, int? DiasEstimados, DateOnly? DataEstimada, bool DentroDoPrazo);

public sealed record EstimativaResponse(
    string RiotId,
    RankDto RankAtual,
    RankDto RankAlvo,
    DateOnly DataLimite,
    int RrFaltando,
    DesempenhoDto Desempenho,
    bool UsouMediasPadrao,
    IReadOnlyList<CenarioDto> Cenarios,
    string Aviso)
{
    public const string AvisoEstimativa =
        "Isto é uma estimativa baseada em médias, não uma previsão. O resultado real varia partida a partida.";

    public static EstimativaResponse De(EstimativaResultado r) => new(
        r.RiotId,
        new RankDto(r.RankAtual.Tier, r.RankAtual.Tier.NomeExibicao(), r.RankAtual.RR),
        new RankDto(r.Meta.RankAlvo, r.Meta.RankAlvo.NomeExibicao(), 0),
        r.Meta.DataLimite,
        r.RrFaltando,
        new DesempenhoDto(r.Snapshot.PartidasAnalisadas, Math.Round(r.Snapshot.WinRate, 3),
            Math.Round(r.Snapshot.RrMedioGanho, 1), Math.Round(r.Snapshot.RrMedioPerdido, 1),
            Math.Round(r.Snapshot.PartidasPorDia, 1)),
        r.UsouMediasPadrao,
        r.Cenarios.Select(c => new CenarioDto(c.Tipo, c.Descricao, c.PartidasPorDia, c.RrMedioPorPartida,
            c.Alcancavel, c.PartidasNecessarias, c.DiasEstimados, c.DataEstimada, c.DentroDoPrazo)).ToList(),
        AvisoEstimativa);
}
