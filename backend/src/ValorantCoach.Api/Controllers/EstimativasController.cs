using ValorantCoach.Api.Dtos;
using ValorantCoach.Application.Estimativas;
using Microsoft.AspNetCore.Mvc;

namespace ValorantCoach.Api.Controllers;

[ApiController]
[Route("api/estimativas")]
public class EstimativasController(EstimarProgressao estimarProgressao) : ControllerBase
{
    [HttpPost]
    public async Task<EstimativaResponse> Estimar(EstimativaRequest request, CancellationToken ct)
    {
        var resultado = await estimarProgressao.ExecutarAsync(
            new EstimarProgressaoComando(request.Perfil, request.Regiao, request.RankAlvo, request.DataLimite), ct);
        return EstimativaResponse.De(resultado);
    }
}
