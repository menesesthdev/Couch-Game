using System.Text.Json;
using ValorantCoach.Infrastructure.Riot;

namespace ValorantCoach.Tests;

/// <summary>
/// Desserializa amostras no formato real dos endpoints da Riot. Existe porque um campo que não
/// casa não estoura erro nenhum: chega nulo em silêncio, e o sintoma aparece longe da causa
/// ("a Riot não aceitou esse token"). A convenção de nomes muda entre os endpoints deles.
/// </summary>
public class ContratosRiotTests
{
    private const string ValorantPoints = "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741";

    // Mesmas opções do ReadFromJsonAsync: tolera maiúsculas diferentes, mas não underscore.
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    private static T? Ler<T>(string json) => JsonSerializer.Deserialize<T>(json, Opcoes);

    [Fact]
    public void Entitlements_usa_snake_case()
    {
        var resposta = Ler<EntitlementsResponse>("""{"entitlements_token":"abc.def.ghi"}""");

        Assert.Equal("abc.def.ghi", resposta?.EntitlementsToken);
    }

    [Fact]
    public void Userinfo_traz_o_puuid_em_sub()
    {
        var resposta = Ler<UserInfoResponse>("""{"country":"bra","sub":"1e4ac9e2-20ee-501a-af44-10136242b6ac"}""");

        Assert.Equal("1e4ac9e2-20ee-501a-af44-10136242b6ac", resposta?.Sub);
    }

    [Fact]
    public void Carteira_traz_saldo_por_id_de_moeda()
    {
        // Recorte de uma resposta real: a carteira traz moedas que não usamos junto.
        var resposta = Ler<WalletResponse>(
            """
            {"Balances":{"85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741":165,
                         "85ca954a-41f2-ce94-9b45-8ca3dd39a00d":10000,
                         "e59aa87c-4cbf-517a-5983-6e81511be9b7":310}}
            """);

        Assert.Equal(165, resposta?.Balances?[ValorantPoints]);
    }

    [Fact]
    public void Storefront_v3_real_casa_com_o_contrato()
    {
        // Recorte de uma resposta real de POST /store/v3/storefront, com os campos que usamos.
        var resposta = Ler<StorefrontResponse>(
            """
            {
              "FeaturedBundle": {
                "Bundles": [{
                  "ID": "f0ffb0ca-4c45-4b8a-a9ba-4a8dcb1e4a5a",
                  "DataAssetID": "1aefc04b-4455-8b2f-6332-79b4260aaffe",
                  "Items": [{
                    "Item": { "ItemTypeID": "dd3bf334-87f3-40bd-b043-682a57a8dc3a",
                              "ItemID": "5ff2ffdc-49c9-ffbe-2009-1cb63d4ff9d7", "Amount": 2 },
                    "BasePrice": 675, "DiscountPercent": 0.25, "DiscountedPrice": 507
                  }],
                  "TotalBaseCost": { "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741": 1575 },
                  "TotalDiscountedCost": { "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741": 1160 },
                  "DurationRemainingInSeconds": 176850
                }]
              },
              "SkinsPanelLayout": {
                "SingleItemOffers": ["a922bb74-4729-f6c8-a125-a4af49e7871a"],
                "SingleItemStoreOffers": [{
                  "OfferID": "a922bb74-4729-f6c8-a125-a4af49e7871a",
                  "IsDirectPurchase": true,
                  "Cost": { "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741": 5350 }
                }],
                "SingleItemOffersRemainingDurationInSeconds": 32850
              }
            }
            """);

        var oferta = Assert.Single(resposta!.SkinsPanelLayout!.SingleItemStoreOffers!);
        Assert.Equal("a922bb74-4729-f6c8-a125-a4af49e7871a", oferta.OfferID);
        Assert.Equal(5350, oferta.Cost![ValorantPoints]);
        Assert.Equal(32850, resposta.SkinsPanelLayout.SingleItemOffersRemainingDurationInSeconds);

        var bundle = Assert.Single(resposta.FeaturedBundle!.Bundles!);
        Assert.Equal("1aefc04b-4455-8b2f-6332-79b4260aaffe", bundle.DataAssetID);
        // O total vem pronto da Riot: somar os itens daria outro número (507 contra 1160).
        Assert.Equal(1160, bundle.TotalDiscountedCost![ValorantPoints]);
        Assert.Equal(1575, bundle.TotalBaseCost![ValorantPoints]);
        Assert.Equal("5ff2ffdc-49c9-ffbe-2009-1cb63d4ff9d7", bundle.Items![0].Item!.ItemID);
    }

    [Fact]
    public void Sem_mercado_noturno_o_campo_simplesmente_nao_vem()
    {
        // Fora da temporada de mercado noturno a Riot omite BonusStore — não pode estourar.
        var resposta = Ler<StorefrontResponse>("""{"SkinsPanelLayout":{"SingleItemStoreOffers":[]}}""");

        Assert.Null(resposta!.BonusStore);
        Assert.Empty(resposta.SkinsPanelLayout!.SingleItemStoreOffers!);
    }

    [Fact]
    public void Mercado_noturno_quando_existe_traz_preco_cheio_e_com_desconto()
    {
        var resposta = Ler<StorefrontResponse>(
            """
            {
              "BonusStore": {
                "BonusStoreOffers": [{
                  "Offer": { "OfferID": "abc", "Cost": { "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741": 2175 } },
                  "DiscountPercent": 50,
                  "DiscountCosts": { "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741": 1087 }
                }],
                "BonusStoreRemainingDurationInSeconds": 3600
              }
            }
            """);

        var oferta = Assert.Single(resposta!.BonusStore!.BonusStoreOffers!);
        Assert.Equal(2175, oferta.Offer!.Cost![ValorantPoints]);
        Assert.Equal(1087, oferta.DiscountCosts![ValorantPoints]);
    }
}
