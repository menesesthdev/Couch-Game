using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Tests;

public class RankTests
{
    [Fact]
    public void RrAte_considera_troca_de_divisao()
    {
        // Platina 3 com 65 RR → Ascendente 1: faltam 35 (P3→D1) + 300 (D1→A1)
        var atual = new Rank(Tier.Platina3, 65);

        Assert.Equal(335, atual.RrAte(Tier.Ascendente1));
    }

    [Fact]
    public void RrAte_e_zero_quando_ja_atingiu()
    {
        Assert.Equal(0, new Rank(Tier.Diamante2, 10).RrAte(Tier.Diamante1));
    }

    [Fact]
    public void DegrausAte_lista_cada_divisao_com_o_rr_que_falta()
    {
        var degraus = new Rank(Tier.Platina3, 65).DegrausAte(Tier.Diamante2);

        Assert.Equal([(Tier.Platina3, 35), (Tier.Diamante1, 100)], degraus);
    }

    [Fact]
    public void Imortal_usa_rr_acumulado_desde_o_imortal_1()
    {
        // Imortal 2 com 156 RR acumulados: Imortal 3 começa em 200, Radiante em 300.
        var atual = new Rank(Tier.Imortal2, 156);

        Assert.Equal(144, atual.RrAte(Tier.Radiante));
        Assert.Equal(44, atual.RrParaProximaDivisao);
        Assert.Equal([(Tier.Imortal2, 44), (Tier.Imortal3, 100)], atual.DegrausAte(Tier.Radiante));
    }

    [Fact]
    public void ProgressoDivisao_reflete_o_rr_na_divisao()
    {
        Assert.Equal(0.65, new Rank(Tier.Platina3, 65).ProgressoDivisao, 3);
        Assert.Equal(1, new Rank(Tier.Radiante, 816).ProgressoDivisao);
    }

    [Theory]
    [InlineData(Tier.Ferro1, "Ferro 1")]
    [InlineData(Tier.Platina3, "Platina 3")]
    [InlineData(Tier.Imortal2, "Imortal 2")]
    [InlineData(Tier.Radiante, "Radiante")]
    public void NomeExibicao(Tier tier, string esperado) => Assert.Equal(esperado, tier.NomeExibicao());
}
