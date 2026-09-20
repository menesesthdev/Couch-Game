using ValorantCoach.Domain.Loja;

namespace ValorantCoach.Api.Dtos;

public sealed record ConexaoLojaResponse(bool Conectado, DateTimeOffset? ExpiraEm);

public sealed record ItemLojaDto(string Id, string Nome, string? Imagem, int Preco)
{
    public static ItemLojaDto De(ItemLoja i) =>
        new(i.Id, i.Desconhecido ? "Item novo" : i.Nome, i.Imagem, i.Preco);
}

public sealed record OfertaDescontoDto(ItemLojaDto Item, int PrecoOriginal, int PrecoComDesconto, int DescontoPercentual)
{
    public static OfertaDescontoDto De(OfertaComDesconto o) =>
        new(ItemLojaDto.De(o.Item), o.PrecoOriginal, o.PrecoComDesconto, o.DescontoPercentual);
}

public sealed record BundleLojaDto(
    string Id, string Nome, string? Imagem, int Preco, int? PrecoBase,
    int RestanteEmSegundos, IReadOnlyList<ItemLojaDto> Itens)
{
    public static BundleLojaDto De(BundleLoja b) => new(
        b.Id, b.Nome, b.Imagem, b.Preco, b.PrecoBase,
        (int)b.Restante.TotalSeconds,
        b.Itens.Select(ItemLojaDto.De).ToList());
}

public sealed record CarteiraDto(int ValorantPoints, int Radianite, int Kingdom);

public sealed record MinhaLojaResponse(
    IReadOnlyList<ItemLojaDto> Diaria,
    int RestanteDiariaEmSegundos,
    int TotalDiaria,
    IReadOnlyList<BundleLojaDto> Bundles,
    IReadOnlyList<OfertaDescontoDto> MercadoNoturno,
    int? RestanteMercadoNoturnoEmSegundos,
    CarteiraDto Carteira)
{
    public static MinhaLojaResponse De(MinhaLoja l) => new(
        l.Diaria.Select(ItemLojaDto.De).ToList(),
        (int)l.RestanteDiaria.TotalSeconds,
        l.TotalDiaria,
        l.Bundles.Select(BundleLojaDto.De).ToList(),
        l.MercadoNoturno.Select(OfertaDescontoDto.De).ToList(),
        l.RestanteMercadoNoturno is { } r ? (int)r.TotalSeconds : null,
        new CarteiraDto(l.Carteira.ValorantPoints, l.Carteira.Radianite, l.Carteira.Kingdom));
}
