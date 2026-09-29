using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Application.Miras;

/// <summary>
/// A galeria de miras. Lê só do nosso banco: a fonte externa é consultada pela sincronização,
/// em segundo plano, para uma visita à aba nunca depender de um site de terceiro estar de pé.
/// </summary>
public class ListarMiras(IMiraRepository repositorio)
{
    public Task<IReadOnlyList<Mira>> ExecutarAsync(TipoMira? tipo, string? busca, CancellationToken ct) =>
        repositorio.ListarAsync(tipo, string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(), ct);
}
