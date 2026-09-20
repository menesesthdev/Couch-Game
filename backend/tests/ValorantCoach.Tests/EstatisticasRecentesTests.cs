using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Tests;

public class EstatisticasRecentesTests
{
    private static readonly Mapa Ascent = new("mapa-ascent", "Ascent");
    private static readonly Mapa Split = new("mapa-split", "Split");
    private static readonly Agente Sova = new("agente-sova", "Sova");
    private static readonly Agente Jett = new("agente-jett", "Jett");

    private static ResumoPartida Partida(
        Mapa mapa, Agente agente, int ganhos, int perdidos,
        int abates = 0, int mortes = 0, int cabeca = 0, int corpo = 0, int dano = 0, int pontos = 0,
        int diasAtras = 0) =>
        new($"{Guid.NewGuid()}", DateTimeOffset.UnixEpoch.AddDays(-diasAtras), mapa, agente, ganhos, perdidos,
            new EstatisticasPartida(abates, mortes, 0, cabeca, corpo, 0, dano, 0, pontos));

    [Fact]
    public void Sem_partidas_devolve_vazio()
    {
        var estatisticas = EstatisticasRecentes.Calcular([]);

        Assert.Equal(0, estatisticas.PartidasAnalisadas);
        Assert.Empty(estatisticas.TopAgentes);
        Assert.Null(estatisticas.Sequencia);
    }

    [Fact]
    public void Kd_e_headshot_somam_o_periodo_inteiro_em_vez_de_media_de_medias()
    {
        // 30 abates / 20 mortes = 1,5 — e não a média entre 2,0 e 1,0.
        var partidas = new[]
        {
            Partida(Ascent, Sova, 13, 5, abates: 20, mortes: 10, cabeca: 30, corpo: 70),
            Partida(Split, Sova, 5, 13, abates: 10, mortes: 10, cabeca: 10, corpo: 90),
        };

        var estatisticas = EstatisticasRecentes.Calcular(partidas);

        Assert.Equal(1.5, estatisticas.Kd);
        Assert.Equal(0.2, estatisticas.TaxaHeadshot, 3);
    }

    [Fact]
    public void Dano_por_round_usa_o_total_de_rounds_e_nao_o_de_partidas()
    {
        // 3600 de dano em 36 rounds = 100 por round, mesmo com partidas de tamanhos diferentes.
        var partidas = new[]
        {
            Partida(Ascent, Sova, 13, 11, dano: 2400, pontos: 4800),
            Partida(Split, Sova, 7, 5, dano: 1200, pontos: 2400),
        };

        var estatisticas = EstatisticasRecentes.Calcular(partidas);

        Assert.Equal(100, estatisticas.DanoPorRound);
        Assert.Equal(200, estatisticas.PontuacaoPorRound);
    }

    [Fact]
    public void Top_agentes_respeita_o_limite_pedido()
    {
        var partidas = new[]
        {
            Partida(Ascent, Sova, 13, 4),
            Partida(Split, Jett, 13, 4),
        };

        Assert.Single(EstatisticasRecentes.Calcular(partidas, top: 1).TopAgentes);
    }

    [Fact]
    public void Sequencia_conta_a_partir_da_partida_mais_recente()
    {
        // Fora de ordem de propósito: a sequência tem de olhar a data, não a posição na lista.
        var partidas = new[]
        {
            Partida(Ascent, Sova, 4, 13, diasAtras: 3),
            Partida(Ascent, Sova, 13, 4, diasAtras: 0),
            Partida(Split, Sova, 13, 9, diasAtras: 1),
            Partida(Ascent, Sova, 13, 2, diasAtras: 2),
        };

        var sequencia = EstatisticasRecentes.Calcular(partidas).Sequencia;

        Assert.Equal(ResultadoPartida.Vitoria, sequencia!.Tipo);
        Assert.Equal(3, sequencia.Quantidade);
    }

    [Fact]
    public void Empate_nao_conta_como_vitoria_nem_derrota()
    {
        var estatisticas = EstatisticasRecentes.Calcular([Partida(Ascent, Sova, 12, 12)]);

        Assert.Equal(0, estatisticas.Vitorias);
        Assert.Equal(0, estatisticas.Derrotas);
        Assert.Equal(1, estatisticas.Empates);
    }
}
