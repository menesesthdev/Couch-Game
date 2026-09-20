using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Application.Portas;

/// <summary>Dados públicos de um perfil de Valorant, como vêm da fonte externa.</summary>
public sealed record PerfilValorant(string Puuid, RiotId RiotId, Rank RankAtual, IReadOnlyList<PartidaRR> HistoricoRecente);

public interface IValorantDataProvider
{
    /// <exception cref="JogadorNaoEncontradoException">Perfil inexistente ou sem partidas ranqueadas.</exception>
    /// <exception cref="FonteIndisponivelException">Rate limit ou falha da API externa.</exception>
    Task<PerfilValorant> BuscarPerfilAsync(RiotId riotId, Regiao regiao, CancellationToken ct);

    /// <summary>
    /// Partidas competitivas com agente, abates e dano, da mais recente para a mais antiga.
    /// Sem RR: isso vem de <see cref="BuscarHistoricoRrAsync"/> e é casado pelo match id.
    /// </summary>
    /// <param name="pagina">1 é a página mais recente.</param>
    Task<IReadOnlyList<ResumoPartida>> BuscarPartidasAsync(RiotId riotId, Regiao regiao, int quantidade, int pagina, CancellationToken ct);

    /// <summary>Histórico de RR por partida, da mais recente para a mais antiga.</summary>
    /// <param name="pagina">1 é a página mais recente.</param>
    Task<IReadOnlyList<PartidaRR>> BuscarHistoricoRrAsync(RiotId riotId, Regiao regiao, int quantidade, int pagina, CancellationToken ct);

    /// <summary>Scoreboard completo de uma partida encerrada.</summary>
    /// <exception cref="JogadorNaoEncontradoException">Partida inexistente nessa região.</exception>
    /// <exception cref="FonteIndisponivelException">Rate limit ou falha da API externa.</exception>
    Task<DetalhePartida> BuscarPartidaAsync(Regiao regiao, string matchId, CancellationToken ct);
}

public class JogadorNaoEncontradoException(string message) : Exception(message);

public class FonteIndisponivelException(string message, Exception? inner = null) : Exception(message, inner);
