using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Calendario;

namespace ValorantCoach.Application.Calendario;

/// <summary>Ato em andamento — usado como prazo padrão da meta ("até o final do ato").</summary>
public class ObterAtoAtual(ICalendarioCompetitivo calendario, TimeProvider relogio)
{
    public async Task<AtoCompetitivo?> ExecutarAsync(CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var atos = await calendario.ListarAtosAsync(ct);
        return atos.FirstOrDefault(a => a.EmAndamento(agora));
    }
}
