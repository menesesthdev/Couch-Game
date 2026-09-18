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
    public void Simulacoes_sempre_sobem_e_comecam_acima_do_win_rate_atual()
    {
        var atual = new Rank(Tier.Platina3, 65);
        var meta = new Meta(Tier.Ascendente1, new DateOnly(2026, 10, 14), Hoje);
        // win rate 50%, +20 / −20, 2 partidas em 2 dias → 1 partida/dia
        var s = Snapshot(atual, (1, 20), (0, -20));

        var simulacoes = CalculadoraEstimativa.Calcular(atual, meta, s, Hoje)
            .Where(c => c.Tipo == TipoCenario.Simulacao).ToList();

        Assert.Equal([0.55, 0.60, 0.65], simulacoes.Select(c => c.WinRate));
        Assert.All(simulacoes, c => Assert.True(c.Alcancavel));
        Assert.All(simulacoes, c => Assert.Equal(1, c.PartidasPorDia));

        // 60%: 0,6·20 − 0,4·20 = 4 RR/partida → 335 / 4 = 84 partidas → 84 dias
        var sessenta = simulacoes[1];
        Assert.Equal(4, sessenta.RrMedioPorPartida);
        Assert.Equal(84, sessenta.PartidasNecessarias);
        Assert.Equal(84, sessenta.DiasEstimados);
        Assert.False(sessenta.DentroDoPrazo);
    }

    [Fact]
    public void Simulacoes_pulam_win_rates_abaixo_do_equilibrio()
    {
        var atual = new Rank(Tier.Ouro1, 0);
        var meta = new Meta(Tier.Ouro2, Hoje.AddDays(30), Hoje);
        // ganha +10, perde −30 → só sobe acima de 75% de vitórias; win rate atual 25%
        var s = Snapshot(atual, (0, 10), (0, -30), (0, -30), (0, -30));

        var simulacoes = CalculadoraEstimativa.Calcular(atual, meta, s, Hoje)
            .Where(c => c.Tipo == TipoCenario.Simulacao).ToList();

        Assert.Equal([0.80, 0.85, 0.90], simulacoes.Select(c => c.WinRate));
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
        // equilíbrio com +20 / −18 ≈ 47% → simulações em 50%, 55% e 60%, ritmo padrão de 3/dia
        var cinquenta = cenarios.First(c => c.Tipo == TipoCenario.Simulacao);
        Assert.Equal(0.5, cinquenta.WinRate);
        Assert.Equal(1, cinquenta.RrMedioPorPartida);
        Assert.Equal(CalculadoraEstimativa.PartidasPorDiaPadrao, cinquenta.PartidasPorDia);
    }

    [Fact]
    public void Meta_ja_atingida_retorna_zero_partidas()
    {
        var atual = new Rank(Tier.Diamante1, 30);
        var meta = new Meta(Tier.Platina1, Hoje.AddDays(5), Hoje);

        var cenarios = CalculadoraEstimativa.Calcular(atual, meta, Snapshot(atual, (0, -20)), Hoje);

        Assert.All(cenarios, c => Assert.Equal(0, c.PartidasNecessarias));
    }

    [Fact]
    public void Requisitos_calcula_ritmo_e_win_rate_para_bater_o_prazo()
    {
        var atual = new Rank(Tier.Ouro1, 0);
        var meta = new Meta(Tier.Ouro2, Hoje.AddDays(5), Hoje);
        // win rate 75%, +20 / −20 → 10 RR/partida; 2 partidas/dia
        var s = Snapshot(atual, (1, 20), (1, 20), (0, 20), (0, -20));

        var r = CalculadoraEstimativa.Requisitos(atual, meta, s, Hoje);

        Assert.Equal(5, r.DiasAtePrazo);
        Assert.Equal(10, r.PartidasNecessarias);
        Assert.Equal(2, r.PartidasPorDiaNecessarias);
        // 2 partidas/dia × 5 dias = 10 partidas → 10 RR/partida → w = (10 + 20) / 40 = 75%
        Assert.Equal(0.75, r.WinRateNecessario);
    }

    [Fact]
    public void Requisitos_indica_win_rate_acima_de_100_quando_o_prazo_e_curto_demais()
    {
        var atual = new Rank(Tier.Ouro1, 0);
        var meta = new Meta(Tier.Platina1, Hoje.AddDays(1), Hoje);
        var s = Snapshot(atual, (1, 20), (0, -20));

        Assert.True(CalculadoraEstimativa.Requisitos(atual, meta, s, Hoje).WinRateNecessario > 1);
    }
}
