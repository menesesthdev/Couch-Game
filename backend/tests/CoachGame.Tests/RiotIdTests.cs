using CoachGame.Domain.Comum;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Tests;

public class RiotIdTests
{
    [Theory]
    [InlineData("Jogador#BR1")]
    [InlineData("  Jogador#BR1  ")]
    [InlineData("https://tracker.gg/valorant/profile/riot/Jogador%23BR1/overview")]
    [InlineData("tracker.gg/valorant/profile/riot/Jogador%23BR1/overview?season=abc")]
    [InlineData("https://tracker.gg/valorant/profile/riot/Jogador%23BR1")]
    public void Parse_aceita_riot_id_e_link_do_tracker(string entrada)
    {
        var id = RiotId.Parse(entrada);

        Assert.Equal("Jogador", id.Nome);
        Assert.Equal("BR1", id.Tag);
    }

    [Fact]
    public void Parse_decodifica_espacos_no_nome()
    {
        var id = RiotId.Parse("tracker.gg/valorant/profile/riot/Meu%20Nick%23777/overview");

        Assert.Equal("Meu Nick", id.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("semtag")]
    [InlineData("nome#")]
    [InlineData("#tag")]
    [InlineData("ab#BR1")]
    public void Parse_rejeita_entradas_invalidas(string entrada) =>
        Assert.Throws<DomainException>(() => RiotId.Parse(entrada));
}
