using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Loja;

namespace ValorantCoach.Infrastructure.Riot;

/// <summary>
/// Endpoints do próprio cliente do Valorant (<c>pd.{shard}.a.pvp.net</c>), usados com o token do
/// jogador. Fica fora de <c>HenrikDev/</c> de propósito: fonte, autenticação e ciclo de vida
/// diferentes — lá é proxy comunitário com chave nossa, aqui é a Riot com credencial do usuário.
///
/// Não são endpoints documentados para terceiros: podem mudar sem aviso, e é por isso que toda
/// falha inesperada vira <see cref="FonteIndisponivelException"/> com mensagem legível.
/// </summary>
public class RiotStorefrontClient(HttpClient http, ICatalogoValorant catalogo) : ILojaRiot
{
    /// <summary>Identificação de plataforma que o cliente do jogo envia. Valor fixo e público.</summary>
    private const string ClientPlatform =
        "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9";

    public async Task<SessaoRiot> AbrirSessaoAsync(string accessToken, Regiao regiao, CancellationToken ct)
    {
        var entitlements = await ObterEntitlementsAsync(accessToken, ct);
        var puuid = await ObterPuuidAsync(accessToken, ct);
        return new SessaoRiot(accessToken, entitlements, puuid, regiao, ExpiracaoDe(accessToken));
    }

    public async Task<MinhaLoja> ObterLojaAsync(SessaoRiot sessao, CancellationToken ct)
    {
        var versao = await catalogo.ObterVersaoClienteAsync(ct);
        var itens = await catalogo.ObterItensAsync(ct);
        var baseUrl = $"https://pd.{Shard(sessao.Regiao)}.a.pvp.net";

        // A loja é v3 e POST com corpo vazio. A v2 (GET) foi removida pela Riot e hoje responde
        // 404 em todos os shards — e a documentação da comunidade ainda descreve a v2.
        var loja = await EnviarAsync<StorefrontResponse>(
            HttpMethod.Post, $"{baseUrl}/store/v3/storefront/{sessao.Puuid}", "a loja", sessao, versao, ct,
            corpoVazio: true);
        var carteira = await EnviarAsync<WalletResponse>(
            HttpMethod.Get, $"{baseUrl}/store/v1/wallet/{sessao.Puuid}", "seu saldo", sessao, versao, ct);

        return Montar(loja, carteira, itens);
    }

    // ---- autenticação ----

    private async Task<string> ObterEntitlementsAsync(string accessToken, CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "https://entitlements.auth.riotgames.com/api/token/v1")
        {
            Content = JsonContent.Create(new { }),
        };
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var resposta = await EnviarBrutoAsync(pedido, Etapa.Login, ct);
        var corpo = await resposta.Content.ReadFromJsonAsync<EntitlementsResponse>(ct);
        if (string.IsNullOrWhiteSpace(corpo?.EntitlementsToken))
            throw new SessaoRiotInvalidaException("A Riot não aceitou esse token. Refaça o login.");

        return corpo.EntitlementsToken;
    }

    private async Task<string> ObterPuuidAsync(string accessToken, CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, "https://auth.riotgames.com/userinfo");
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var resposta = await EnviarBrutoAsync(pedido, Etapa.Login, ct);
        var corpo = await resposta.Content.ReadFromJsonAsync<UserInfoResponse>(ct);
        if (string.IsNullOrWhiteSpace(corpo?.Sub))
            throw new SessaoRiotInvalidaException("Não conseguimos identificar a conta desse token.");

        return corpo.Sub;
    }

    /// <summary>
    /// Validade do próprio token (claim <c>exp</c>), com margem para não vencer em pleno uso.
    /// Lê o payload direto em vez de trazer uma biblioteca de JWT só para isso — o token não é
    /// validado aqui, quem valida é a Riot; só queremos saber quando parar de tentar.
    /// </summary>
    private static DateTimeOffset ExpiracaoDe(string accessToken)
    {
        var padrao = DateTimeOffset.UtcNow.AddHours(1);
        try
        {
            var partes = accessToken.Split('.');
            if (partes.Length != 3) return padrao;

            var payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var segundos))
                return DateTimeOffset.FromUnixTimeSeconds(segundos).AddMinutes(-2);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            // Token fora do formato esperado: fica com o padrão de 1h, que é o que a Riot entrega.
        }
        return padrao;
    }

    // ---- montagem ----

    private static MinhaLoja Montar(
        StorefrontResponse? loja, WalletResponse? carteira, IReadOnlyDictionary<string, ItemCatalogo> catalogo)
    {
        ItemLoja Item(string id, int preco)
        {
            var meta = catalogo.GetValueOrDefault(id);
            return new ItemLoja(id, meta?.Nome ?? "", meta?.Imagem, preco);
        }

        var painel = loja?.SkinsPanelLayout;
        var diaria = (painel?.SingleItemStoreOffers ?? [])
            .Select(o => Item(o.OfferID ?? "", PrecoDe(o.Cost)))
            .ToList();

        var bundles = (loja?.FeaturedBundle?.Bundles ?? [])
            .Select(b => new BundleLoja(
                b.DataAssetID ?? "",
                catalogo.GetValueOrDefault(b.DataAssetID ?? "")?.Nome ?? "Pacote em destaque",
                catalogo.GetValueOrDefault(b.DataAssetID ?? "")?.Imagem,
                PrecoDe(b.TotalDiscountedCost),
                b.TotalBaseCost is { } bruto ? PrecoDe(bruto) : null,
                TimeSpan.FromSeconds(Math.Max(0, b.DurationRemainingInSeconds)),
                (b.Items ?? [])
                    .Select(i => Item(i.Item?.ItemID ?? "", i.DiscountedPrice))
                    .ToList()))
            .ToList();

        var noturno = (loja?.BonusStore?.BonusStoreOffers ?? [])
            .Select(o => new OfertaComDesconto(
                Item(o.Offer?.OfferID ?? "", PrecoDe(o.DiscountCosts)),
                PrecoDe(o.Offer?.Cost),
                PrecoDe(o.DiscountCosts)))
            .ToList();

        return new MinhaLoja(
            diaria,
            TimeSpan.FromSeconds(Math.Max(0, painel?.SingleItemOffersRemainingDurationInSeconds ?? 0)),
            bundles,
            noturno,
            loja?.BonusStore is { } b2 ? TimeSpan.FromSeconds(Math.Max(0, b2.BonusStoreRemainingDurationInSeconds)) : null,
            new Carteira(
                MoedaDe(carteira?.Balances, Moedas.ValorantPoints),
                MoedaDe(carteira?.Balances, Moedas.Radianite),
                MoedaDe(carteira?.Balances, Moedas.Kingdom)));
    }

    /// <summary>Os preços vêm num mapa por id de moeda; o que interessa é sempre Valorant Points.</summary>
    private static int PrecoDe(Dictionary<string, int>? custos) =>
        custos?.GetValueOrDefault(Moedas.ValorantPoints) ?? custos?.Values.FirstOrDefault() ?? 0;

    private static int MoedaDe(Dictionary<string, int>? saldos, string moeda) =>
        saldos?.GetValueOrDefault(moeda) ?? 0;

    private static class Moedas
    {
        public const string ValorantPoints = "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741";
        public const string Radianite = "e59aa87c-4cbf-517a-5983-6e81511be9b7";
        public const string Kingdom = "85ca954a-41f2-ce94-9b45-8ca3dd39a00d";
    }

    /// <summary>BR e LATAM são servidos pelo shard da América do Norte.</summary>
    private static string Shard(Regiao regiao) => regiao switch
    {
        Regiao.Br or Regiao.Latam or Regiao.Na => "na",
        Regiao.Eu => "eu",
        Regiao.Ap => "ap",
        Regiao.Kr => "kr",
        _ => "na",
    };

    // ---- transporte ----

    private async Task<T?> EnviarAsync<T>(
        HttpMethod metodo, string url, string oQue, SessaoRiot sessao, string versaoCliente,
        CancellationToken ct, bool corpoVazio = false)
    {
        using var pedido = new HttpRequestMessage(metodo, url);
        if (corpoVazio) pedido.Content = JsonContent.Create(new { });
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
        pedido.Headers.TryAddWithoutValidation("X-Riot-Entitlements-JWT", sessao.EntitlementsToken);
        pedido.Headers.TryAddWithoutValidation("X-Riot-ClientVersion", versaoCliente);
        pedido.Headers.TryAddWithoutValidation("X-Riot-ClientPlatform", ClientPlatform);

        var resposta = await EnviarBrutoAsync(pedido, Etapa.Loja, ct, oQue);
        return await resposta.Content.ReadFromJsonAsync<T>(ct);
    }

    /// <summary>
    /// Em qual etapa a chamada está. Muda como o erro é lido: durante o login o token é a única
    /// variável, então quase toda recusa significa "refaça o login" — inclusive um 500, que a Riot
    /// devolve para token malformado. Mandar o usuário "tentar de novo" nesse caso o deixaria preso.
    /// </summary>
    private enum Etapa { Login, Loja }

    private async Task<HttpResponseMessage> EnviarBrutoAsync(
        HttpRequestMessage pedido, Etapa etapa, CancellationToken ct, string oQue = "a loja")
    {
        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(pedido, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FonteIndisponivelException("Não foi possível falar com a Riot agora.", ex);
        }

        if (resposta.IsSuccessStatusCode) return resposta;

        var status = (int)resposta.StatusCode;
        resposta.Dispose();

        if (resposta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new SessaoRiotInvalidaException("Sua conexão com a Riot expirou. Conecte de novo.");

        if (etapa == Etapa.Login)
            throw new SessaoRiotInvalidaException(
                $"A Riot não validou esse login (respondeu {status}). Copie a URL de novo — ela vale por poucos minutos.");

        throw new FonteIndisponivelException($"A Riot respondeu {status} ao consultar {oQue}.");
    }
}
