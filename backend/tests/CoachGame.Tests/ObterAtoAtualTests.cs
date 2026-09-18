using CoachGame.Application.Calendario;
using CoachGame.Application.Portas;
using CoachGame.Domain.Calendario;
using Microsoft.Extensions.Time.Testing;

namespace CoachGame.Tests;

public class ObterAtoAtualTests
{
    private sealed class CalendarioFixo(params AtoCompetitivo[] atos) : ICalendarioCompetitivo
    {
        public Task<IReadOnlyList<AtoCompetitivo>> ListarAtosAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AtoCompetitivo>>(atos);
    }

    private static readonly AtoCompetitivo AtoV = new("V26 · Ato V",
        new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero));
    private static readonly AtoCompetitivo AtoVI = new("V26 · Ato VI",
        AtoV.Fim, new DateTimeOffset(2027, 1, 6, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Retorna_o_ato_em_andamento()
    {
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero));

        var ato = await new ObterAtoAtual(new CalendarioFixo(AtoV, AtoVI), relogio).ExecutarAsync(default);

        Assert.Equal(AtoV, ato);
    }

    [Fact]
    public async Task No_instante_da_virada_ja_e_o_proximo_ato()
    {
        var relogio = new FakeTimeProvider(AtoV.Fim);

        var ato = await new ObterAtoAtual(new CalendarioFixo(AtoV, AtoVI), relogio).ExecutarAsync(default);

        Assert.Equal(AtoVI, ato);
    }
}
