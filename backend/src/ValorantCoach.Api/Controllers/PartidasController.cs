using ValorantCoach.Api.Dtos;
using ValorantCoach.Application.Partidas;
using ValorantCoach.Domain.Jogadores;
using Microsoft.AspNetCore.Mvc;

namespace ValorantCoach.Api.Controllers;

[ApiController]
[Route("api/partidas")]
public class PartidasController(ObterDetalhePartida obterDetalhe) : ControllerBase
{
    /// <summary>Scoreboard completo de uma partida: os dois times, agentes, K/D/A e rounds.</summary>
    [HttpGet("{regiao}/{matchId}")]
    public async Task<DetalhePartidaResponse> Detalhe(Regiao regiao, string matchId, CancellationToken ct) =>
        DetalhePartidaResponse.De(await obterDetalhe.ExecutarAsync(regiao, matchId, ct));
}
