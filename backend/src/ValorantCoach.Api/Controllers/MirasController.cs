using Microsoft.AspNetCore.Mvc;
using ValorantCoach.Api.Dtos;
using ValorantCoach.Application.Miras;
using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Api.Controllers;

/// <summary>
/// Galeria de miras de pro player. Área anônima, como a de progressão — nada aqui depende de
/// conta conectada.
/// </summary>
[ApiController]
[Route("api/miras")]
public class MirasController(ListarMiras listarMiras) : ControllerBase
{
    /// <summary>
    /// As miras da galeria. <paramref name="tipo"/> é o que separa as coleções (só cruz, só ponto);
    /// sem ele vêm todas.
    /// </summary>
    [HttpGet]
    public async Task<IReadOnlyList<MiraResponse>> Listar(
        [FromQuery] TipoMira? tipo,
        [FromQuery] string? q,
        CancellationToken ct)
    {
        var miras = await listarMiras.ExecutarAsync(tipo, q, ct);
        return [.. miras.Select(MiraResponse.De)];
    }
}
