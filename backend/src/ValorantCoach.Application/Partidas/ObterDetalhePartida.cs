using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Comum;
using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Application.Partidas;

/// <summary>Scoreboard completo de uma partida, para a página aberta a partir do histórico.</summary>
public class ObterDetalhePartida(IValorantDataProvider fonte)
{
    public Task<DetalhePartida> ExecutarAsync(Regiao regiao, string matchId, CancellationToken ct)
    {
        if (!Guid.TryParse(matchId, out _))
            throw new DomainException("Identificador de partida inválido.");

        return fonte.BuscarPartidaAsync(regiao, matchId, ct);
    }
}
