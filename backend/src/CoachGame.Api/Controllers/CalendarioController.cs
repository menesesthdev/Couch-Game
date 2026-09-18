using CoachGame.Application.Calendario;
using Microsoft.AspNetCore.Mvc;

namespace CoachGame.Api.Controllers;

public sealed record AtoDto(string Nome, DateTimeOffset Inicio, DateTimeOffset Fim);

[ApiController]
[Route("api/calendario")]
public class CalendarioController(ObterAtoAtual obterAtoAtual) : ControllerBase
{
    /// <summary>Ato competitivo em andamento. 204 se estiver entre atos.</summary>
    [HttpGet("ato-atual")]
    public async Task<ActionResult<AtoDto>> AtoAtual(CancellationToken ct)
    {
        var ato = await obterAtoAtual.ExecutarAsync(ct);
        return ato is null ? NoContent() : new AtoDto(ato.Nome, ato.Inicio, ato.Fim);
    }
}
