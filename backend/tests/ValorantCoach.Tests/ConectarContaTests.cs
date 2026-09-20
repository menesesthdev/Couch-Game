using ValorantCoach.Application.Loja;
using ValorantCoach.Domain.Comum;

namespace ValorantCoach.Tests;

public class ConectarContaTests
{
    // Formato de JWT (três blocos base64url); o conteúdo não importa para a extração.
    private const string Token = "eyJhbGciOiJSUzI1NiJ9.eyJleHAiOjE3NjAwMDAwMDB9.Zm9yamFkby1wYXJhLXRlc3Rl";

    [Fact]
    public void Extrai_o_token_da_url_de_redirecionamento_completa()
    {
        var url = $"http://localhost/redirect#access_token={Token}&scope=openid&iss=https%3A%2F%2Fauth.riotgames.com&token_type=Bearer&expires_in=3600";

        Assert.Equal(Token, ConectarConta.ExtrairAccessToken(url));
    }

    [Fact]
    public void Aceita_o_token_colado_sozinho()
    {
        Assert.Equal(Token, ConectarConta.ExtrairAccessToken($"  {Token}  "));
    }

    [Fact]
    public void Nao_confunde_id_token_com_access_token()
    {
        // O fragmento traz os dois; pegar o id_token daria uma sessão que a Riot recusa.
        var outro = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJvdXRybyJ9.b3V0cm8tdG9rZW4";
        var url = $"http://localhost/redirect#id_token={outro}&access_token={Token}&expires_in=3600";

        Assert.Equal(Token, ConectarConta.ExtrairAccessToken(url));
    }

    [Fact]
    public void Extrai_o_token_do_fragmento_real_com_id_token_junto()
    {
        // Como a Riot responde de verdade: response_type=token id_token devolve os dois.
        var idToken = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJvdXRybyJ9.b3V0cm8tdG9rZW4";
        var url = $"http://localhost/redirect#access_token={Token}&scope=openid&iss=https%3A%2F%2Fauth.riotgames.com" +
                  $"&id_token={idToken}&token_type=Bearer&expires_in=3600";

        Assert.Equal(Token, ConectarConta.ExtrairAccessToken(url));
    }

    [Fact]
    public void Url_de_erro_repete_o_motivo_dado_pela_riot()
    {
        var url = "https://localhost/redirect#error=unsupported_response_type&iss=https%3A%2F%2Fauth.riotgames.com" +
                  "&error_description=Unsupported+response+type";

        var erro = Assert.Throws<DomainException>(() => ConectarConta.ExtrairAccessToken(url));

        Assert.Contains("Unsupported response type", erro.Message);
    }

    [Fact]
    public void Url_de_erro_sem_descricao_usa_o_codigo()
    {
        var erro = Assert.Throws<DomainException>(
            () => ConectarConta.ExtrairAccessToken("http://localhost/redirect#error=invalid_request"));

        Assert.Contains("invalid_request", erro.Message);
    }

    [Fact]
    public void Login_cancelado_explica_o_que_houve()
    {
        var erro = Assert.Throws<DomainException>(
            () => ConectarConta.ExtrairAccessToken("http://localhost/redirect#error=access_denied"));

        Assert.Contains("cancelado", erro.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://localhost/redirect")]
    [InlineData("não é uma url")]
    public void Entrada_sem_token_vira_erro_de_dominio(string entrada)
    {
        Assert.Throws<DomainException>(() => ConectarConta.ExtrairAccessToken(entrada));
    }
}
