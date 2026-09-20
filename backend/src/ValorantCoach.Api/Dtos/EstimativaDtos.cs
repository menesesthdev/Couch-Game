using System.ComponentModel.DataAnnotations;
using ValorantCoach.Application.Estimativas;
using ValorantCoach.Domain.Estimativas;
using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Api.Dtos;

public sealed record EstimativaRequest(
    [Required] string Perfil,
    [Required] Regiao Regiao,
    [Required] Tier RankAlvo,
    [Required] DateOnly DataLimite);

public sealed record CenarioDto(
    TipoCenario Tipo, string Descricao, double WinRate, double PartidasPorDia, double RrMedioPorPartida,
    bool Alcancavel, int? PartidasNecessarias, int? DiasEstimados, DateOnly? DataEstimada, bool DentroDoPrazo);

public sealed record DegrauDto(RankDto Rank, int RrFaltando);

public sealed record EstimativaResponse(
    PerfilResponse Perfil,
    RankDto RankAlvo,
    DateOnly DataLimite,
    int RrFaltando,
    IReadOnlyList<DegrauDto> Degraus,
    bool UsouMediasPadrao,
    IReadOnlyList<CenarioDto> Cenarios,
    RequisitosPrazo Requisitos,
    string Aviso)
{
    public const string AvisoEstimativa =
        "Isto é uma estimativa baseada em médias, não uma previsão. O resultado real varia partida a partida.";

    public static EstimativaResponse De(EstimativaResultado r) => new(
        PerfilResponse.De(r.Consulta),
        RankDto.De(r.Meta.RankAlvo),
        r.Meta.DataLimite,
        r.RrFaltando,
        r.Consulta.Perfil.RankAtual.DegrausAte(r.Meta.RankAlvo)
            .Select(d => new DegrauDto(RankDto.De(d.Tier), d.RrFaltando)).ToList(),
        r.UsouMediasPadrao,
        r.Cenarios.Select(c => new CenarioDto(c.Tipo, c.Descricao, Math.Round(c.WinRate, 3), c.PartidasPorDia, c.RrMedioPorPartida,
            c.Alcancavel, c.PartidasNecessarias, c.DiasEstimados, c.DataEstimada, c.DentroDoPrazo)).ToList(),
        r.Requisitos,
        AvisoEstimativa);
}
