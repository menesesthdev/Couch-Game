using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Portas;

/// <summary>Dados públicos de um perfil de Valorant, como vêm da fonte externa.</summary>
public sealed record PerfilValorant(string Puuid, RiotId RiotId, Rank RankAtual, IReadOnlyList<PartidaRR> HistoricoRecente);

public interface IValorantDataProvider
{
    /// <exception cref="JogadorNaoEncontradoException">Perfil inexistente ou sem partidas ranqueadas.</exception>
    /// <exception cref="FonteIndisponivelException">Rate limit ou falha da API externa.</exception>
    Task<PerfilValorant> BuscarPerfilAsync(RiotId riotId, Regiao regiao, CancellationToken ct);
}

public class JogadorNaoEncontradoException(string message) : Exception(message);

public class FonteIndisponivelException(string message, Exception? inner = null) : Exception(message, inner);
