using CoachGame.Api.Dtos;
using CoachGame.Domain.Jogadores;
using Microsoft.AspNetCore.Mvc;

namespace CoachGame.Api.Controllers;

[ApiController]
[Route("api/ranks")]
public class RanksController : ControllerBase
{
    /// <summary>Lista de ranks para o seletor de meta.</summary>
    [HttpGet]
    public IEnumerable<RankDto> Listar() =>
        Enum.GetValues<Tier>().Where(t => t.Ranqueado()).Select(t => new RankDto(t, t.NomeExibicao(), 0));
}
