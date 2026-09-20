namespace ValorantCoach.Domain.Loja;

/// <summary>Um item à venda, já resolvido com nome e arte do catálogo público.</summary>
public sealed record ItemLoja(string Id, string Nome, string? Imagem, int Preco)
{
    /// <summary>Item que a Riot devolveu mas que o catálogo não conhece (skin nova, por exemplo).</summary>
    public bool Desconhecido => string.IsNullOrWhiteSpace(Nome);
}

/// <summary>Oferta do mercado noturno: o mesmo item, com desconto.</summary>
public sealed record OfertaComDesconto(ItemLoja Item, int PrecoOriginal, int PrecoComDesconto)
{
    public int DescontoPercentual => PrecoOriginal == 0
        ? 0
        : (int)Math.Round(100.0 * (PrecoOriginal - PrecoComDesconto) / PrecoOriginal);
}

/// <summary>Pacote em destaque. O preço base só vem quando a Riot informa desconto.</summary>
public sealed record BundleLoja(
    string Id,
    string Nome,
    string? Imagem,
    int Preco,
    int? PrecoBase,
    TimeSpan Restante,
    IReadOnlyList<ItemLoja> Itens);

/// <summary>Saldo do jogador. VP compra skins; Radianite evolui skins já compradas.</summary>
public sealed record Carteira(int ValorantPoints, int Radianite, int Kingdom);

/// <summary>
/// A loja de um jogador em um instante. Tudo aqui é privado da conta — nada disso vem
/// da HenrikDev, e sim dos endpoints do próprio cliente do jogo.
/// </summary>
public sealed record MinhaLoja(
    IReadOnlyList<ItemLoja> Diaria,
    TimeSpan RestanteDiaria,
    IReadOnlyList<BundleLoja> Bundles,
    IReadOnlyList<OfertaComDesconto> MercadoNoturno,
    TimeSpan? RestanteMercadoNoturno,
    Carteira Carteira)
{
    /// <summary>O mercado noturno só existe em algumas semanas do ato.</summary>
    public bool TemMercadoNoturno => MercadoNoturno.Count > 0;

    public int TotalDiaria => Diaria.Sum(i => i.Preco);
}
