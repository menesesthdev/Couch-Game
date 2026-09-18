using CoachGame.Domain.Calendario;

namespace CoachGame.Application.Portas;

public interface ICalendarioCompetitivo
{
    /// <summary>Atos conhecidos (passados, atual e o próximo, se já anunciado).</summary>
    /// <exception cref="FonteIndisponivelException">Fonte do calendário fora do ar.</exception>
    Task<IReadOnlyList<AtoCompetitivo>> ListarAtosAsync(CancellationToken ct);
}
