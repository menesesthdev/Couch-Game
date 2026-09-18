using System.Text.RegularExpressions;
using CoachGame.Domain.Comum;

namespace CoachGame.Domain.Jogadores;

/// <summary>Riot ID no formato <c>nome#tag</c>.</summary>
public sealed partial record RiotId
{
    public string Nome { get; }
    public string Tag { get; }

    public RiotId(string nome, string tag)
    {
        nome = nome.Trim();
        tag = tag.Trim();
        if (nome.Length is < 3 or > 16) throw new DomainException("O nome do Riot ID deve ter entre 3 e 16 caracteres.");
        if (tag.Length is < 3 or > 5) throw new DomainException("A tag do Riot ID deve ter entre 3 e 5 caracteres.");
        Nome = nome;
        Tag = tag;
    }

    /// <summary>
    /// Aceita <c>nome#tag</c> digitado direto ou um link do tracker.gg
    /// (<c>tracker.gg/valorant/profile/riot/nome%23tag/overview</c>).
    /// </summary>
    public static RiotId Parse(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) throw new DomainException("Informe o link do perfil ou o Riot ID.");

        var texto = entrada.Trim();
        var link = TrackerGgRegex().Match(texto);
        if (link.Success) texto = link.Groups["id"].Value;

        texto = Uri.UnescapeDataString(texto);
        var separador = texto.LastIndexOf('#');
        if (separador <= 0 || separador == texto.Length - 1)
            throw new DomainException("Riot ID inválido. Use o formato nome#tag ou cole o link do tracker.gg.");

        return new RiotId(texto[..separador], texto[(separador + 1)..]);
    }

    public override string ToString() => $"{Nome}#{Tag}";

    [GeneratedRegex(@"tracker\.gg/valorant/profile/riot/(?<id>[^/?#]+)", RegexOptions.IgnoreCase)]
    private static partial Regex TrackerGgRegex();
}
