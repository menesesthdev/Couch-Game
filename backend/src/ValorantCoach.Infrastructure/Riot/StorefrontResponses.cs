using System.Text.Json.Serialization;

namespace ValorantCoach.Infrastructure.Riot;

// Contratos dos endpoints do cliente do Valorant. Só os campos usados; não são documentados para
// terceiros, então podem mudar sem aviso.
//
// Atenção: a convenção de nomes NÃO é uniforme entre eles. O storefront e a carteira devolvem
// PascalCase, mas os endpoints de autenticação usam snake_case — e o ReadFromJsonAsync ignora
// diferença de maiúsculas, não de underscore. Campo snake_case sem [JsonPropertyName] chega nulo
// em silêncio.

internal sealed record EntitlementsResponse(
    [property: JsonPropertyName("entitlements_token")] string? EntitlementsToken);

/// <summary>O PUUID vem na claim <c>sub</c> do /userinfo.</summary>
internal sealed record UserInfoResponse(string? Sub);

internal sealed record WalletResponse(Dictionary<string, int>? Balances);

internal sealed record StorefrontResponse(
    FeaturedBundleNode? FeaturedBundle,
    SkinsPanelNode? SkinsPanelLayout,
    BonusStoreNode? BonusStore);

internal sealed record SkinsPanelNode(
    List<StoreOffer>? SingleItemStoreOffers,
    int SingleItemOffersRemainingDurationInSeconds);

internal sealed record StoreOffer(string? OfferID, Dictionary<string, int>? Cost);

internal sealed record FeaturedBundleNode(List<BundleNode>? Bundles);

internal sealed record BundleNode(
    string? DataAssetID,
    List<BundleItem>? Items,
    Dictionary<string, int>? TotalBaseCost,
    Dictionary<string, int>? TotalDiscountedCost,
    int DurationRemainingInSeconds);

internal sealed record BundleItem(BundleItemInfo? Item, int BasePrice, int DiscountedPrice);

internal sealed record BundleItemInfo(string? ItemID);

/// <summary>Mercado noturno — só existe em algumas semanas do ato.</summary>
internal sealed record BonusStoreNode(
    List<BonusOffer>? BonusStoreOffers,
    int BonusStoreRemainingDurationInSeconds);

internal sealed record BonusOffer(StoreOffer? Offer, Dictionary<string, int>? DiscountCosts);
