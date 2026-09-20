namespace ValorantCoach.Application.Portas;

/// <summary>Nome e arte de um item vendável, do catálogo público.</summary>
public sealed record ItemCatalogo(string Id, string Nome, string? Imagem);

/// <summary>
/// Catálogo público de skins (valorant-api.com). A Riot devolve a loja só com ids;
/// é isto que transforma id em nome e imagem.
/// </summary>
public interface ICatalogoValorant
{
    /// <summary>Indexado por id de nível de skin. Muda a cada patch, então vale cache longo.</summary>
    Task<IReadOnlyDictionary<string, ItemCatalogo>> ObterItensAsync(CancellationToken ct);

    /// <summary>Versão do cliente exigida no header das chamadas à Riot.</summary>
    Task<string> ObterVersaoClienteAsync(CancellationToken ct);
}
