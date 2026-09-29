using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Application.Portas;

public interface IMiraRepository
{
    /// <summary>Miras da galeria, opcionalmente de um tipo só e filtradas por nome.</summary>
    Task<IReadOnlyList<Mira>> ListarAsync(TipoMira? tipo, string? busca, CancellationToken ct);

    /// <summary>Quantas miras já temos. Serve para saber se a primeira sincronização já rodou.</summary>
    Task<int> ContarAsync(CancellationToken ct);

    /// <summary>
    /// Substitui o conteúdo importado pelo que veio da fonte. É um "espelho": mira que sumiu lá
    /// (pro trocou de mira) tem de sumir aqui, senão a galeria acumula coisa desatualizada.
    /// </summary>
    Task SincronizarAsync(IReadOnlyList<Mira> miras, CancellationToken ct);
}
