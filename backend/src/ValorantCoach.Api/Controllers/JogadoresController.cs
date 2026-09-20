using ValorantCoach.Api.Dtos;
using ValorantCoach.Application.Jogadores;
using ValorantCoach.Application.Partidas;
using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Partidas;
using Microsoft.AspNetCore.Mvc;

namespace ValorantCoach.Api.Controllers;

[ApiController]
[Route("api/jogadores")]
public class JogadoresController(
    ConsultarPerfil consultarPerfil,
    BuscarJogadores buscarJogadores,
    AnalisarDesempenho analisarDesempenho) : ControllerBase
{
    /// <summary>Autocomplete: jogadores já consultados cujo nome começa com <paramref name="q"/>.</summary>
    [HttpGet("busca")]
    public async Task<IEnumerable<SugestaoJogadorDto>> Buscar(string? q, Regiao? regiao, CancellationToken ct) =>
        (await buscarJogadores.ExecutarAsync(q, regiao, ct)).Select(SugestaoJogadorDto.De);

    /// <summary>Perfil completo: rank atual, desempenho e partidas recentes. Aceita nome#tag ou link do tracker.gg.</summary>
    [HttpGet("perfil")]
    public async Task<PerfilResponse> Perfil([FromQuery] string perfil, [FromQuery] Regiao regiao, CancellationToken ct) =>
        PerfilResponse.De(await consultarPerfil.ExecutarAsync(perfil, regiao, ct));

    /// <summary>
    /// Partidas com desempenho e RR, mais o resumo do período. <paramref name="periodo"/> aceita
    /// <c>Ultimas</c> (padrão, 20 partidas) ou <c>AtoAtual</c> (tudo do ato em andamento).
    /// </summary>
    [HttpGet("desempenho")]
    public async Task<DesempenhoResponse> Desempenho(
        [FromQuery] string perfil,
        [FromQuery] Regiao regiao,
        [FromQuery] PeriodoAnalise periodo = PeriodoAnalise.Ultimas,
        CancellationToken ct = default) =>
        DesempenhoResponse.De(await analisarDesempenho.ExecutarAsync(perfil, regiao, periodo, ct));
}
