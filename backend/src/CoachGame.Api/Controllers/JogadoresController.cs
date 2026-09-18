using CoachGame.Api.Dtos;
using CoachGame.Application.Jogadores;
using CoachGame.Domain.Jogadores;
using Microsoft.AspNetCore.Mvc;

namespace CoachGame.Api.Controllers;

[ApiController]
[Route("api/jogadores")]
public class JogadoresController(ConsultarPerfil consultarPerfil, BuscarJogadores buscarJogadores) : ControllerBase
{
    /// <summary>Autocomplete: jogadores já consultados cujo nome começa com <paramref name="q"/>.</summary>
    [HttpGet("busca")]
    public async Task<IEnumerable<SugestaoJogadorDto>> Buscar(string? q, Regiao? regiao, CancellationToken ct) =>
        (await buscarJogadores.ExecutarAsync(q, regiao, ct)).Select(SugestaoJogadorDto.De);

    /// <summary>Perfil completo: rank atual, desempenho e partidas recentes. Aceita nome#tag ou link do tracker.gg.</summary>
    [HttpGet("perfil")]
    public async Task<PerfilResponse> Perfil([FromQuery] string perfil, [FromQuery] Regiao regiao, CancellationToken ct) =>
        PerfilResponse.De(await consultarPerfil.ExecutarAsync(perfil, regiao, ct));
}
