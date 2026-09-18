using CoachGame.Application.Portas;
using CoachGame.Domain.Calendario;

namespace CoachGame.Application.Calendario;

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
