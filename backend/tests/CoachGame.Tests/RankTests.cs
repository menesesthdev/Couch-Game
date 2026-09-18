using CoachGame.Domain.Jogadores;

namespace CoachGame.Tests;

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

    [Theory]
    [InlineData(Tier.Ferro1, "Ferro 1")]
    [InlineData(Tier.Platina3, "Platina 3")]
    [InlineData(Tier.Imortal2, "Imortal 2")]
    [InlineData(Tier.Radiante, "Radiante")]
    public void NomeExibicao(Tier tier, string esperado) => Assert.Equal(esperado, tier.NomeExibicao());
}
