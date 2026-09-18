using CoachGame.Domain.Estimativas;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Tests;

public class CalculadoraEstimativaTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 18);
    private static readonly DateTimeOffset Agora = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static SnapshotRR Snapshot(Rank rank, params (int dia, int rr)[] partidas) =>
        SnapshotRR.Capturar(Guid.NewGuid(), rank,
            partidas.Select(p => new PartidaRR(Agora.AddDays(-p.dia), p.rr)).ToList(), Agora);

    [Fact]
    public void Snapshot_calcula_medias_reais()
    {
        // 4 partidas em 2 dias: 3 vitórias (+20, +22, +24) e 1 derrota (-18)
        var s = Snapshot(new Rank(Tier.Ouro1, 50), (1, 20), (1, 22), (0, 24), (0, -18));

        Assert.Equal(0.75, s.WinRate);
        Assert.Equal(22, s.RrMedioGanho);
        Assert.Equal(18, s.RrMedioPerdido);
        Assert.Equal(2, s.PartidasPorDia);
    }

    [Fact]
    public void Cenarios_fixos_usam_medias_reais()
    {
        var atual = new Rank(Tier.Platina3, 65);
        var meta = new Meta(Tier.Ascendente1, new DateOnly(2026, 10, 14), Hoje);
        var s = Snapshot(atual, (1, 20), (0, -20));

        var cenarios = CalculadoraEstimativa.Calcular(atual, meta, s, Hoje);

        // 335 RR; 3V/0D = 20 RR/partida → 17 partidas → 6 dias
        var tres = cenarios.Single(c => c.Tipo == TipoCenario.TresVitorias);
        Assert.Equal(17, tres.PartidasNecessarias);
        Assert.Equal(6, tres.DiasEstimados);
        Assert.Equal(Hoje.AddDays(6), tres.DataEstimada);
        Assert.True(tres.DentroDoPrazo);

        // 1V/2D: (20 − 40) / 3 < 0 → não sobe
        Assert.False(cenarios.Single(c => c.Tipo == TipoCenario.UmaVitoria).Alcancavel);
    }

    [Fact]
    public void Cenario_real_usa_win_rate_e_ritmo_do_historico()
    {
        var atual = new Rank(Tier.Ouro1, 0);
        var meta = new Meta(Tier.Ouro2, new DateOnly(2026, 9, 20), Hoje);
        // win rate 75%, +20 / −20, 2 partidas/dia → 10 RR/partida → 10 partidas → 5 dias
        var s = Snapshot(atual, (1, 20), (1, 20), (0, 20), (0, -20));

        var real = CalculadoraEstimativa.Calcular(atual, meta, s, Hoje).Single(c => c.Tipo == TipoCenario.Real);

        Assert.Equal(10, real.RrMedioPorPartida);
        Assert.Equal(10, real.PartidasNecessarias);
        Assert.Equal(5, real.DiasEstimados);
        Assert.False(real.DentroDoPrazo);
    }

    [Fact]
    public void Sem_historico_usa_medias_padrao_e_omite_cenario_real()
    {
        var atual = new Rank(Tier.Prata1, 0);
        var meta = new Meta(Tier.Prata2, Hoje.AddDays(30), Hoje);

        var cenarios = CalculadoraEstimativa.Calcular(atual, meta, Snapshot(atual), Hoje);

        Assert.DoesNotContain(cenarios, c => c.Tipo == TipoCenario.Real);
        Assert.Equal(CalculadoraEstimativa.RrGanhoPadrao,
            cenarios.Single(c => c.Tipo == TipoCenario.TresVitorias).RrMedioPorPartida);
    }

    [Fact]
    public void Meta_ja_atingida_retorna_zero_partidas()
    {
        var atual = new Rank(Tier.Diamante1, 30);
        var meta = new Meta(Tier.Platina1, Hoje.AddDays(5), Hoje);

        var cenarios = CalculadoraEstimativa.Calcular(atual, meta, Snapshot(atual, (0, -20)), Hoje);

        Assert.All(cenarios, c => Assert.Equal(0, c.PartidasNecessarias));
    }
}
