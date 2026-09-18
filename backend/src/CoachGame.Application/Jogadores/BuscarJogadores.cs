using CoachGame.Application.Portas;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Jogadores;

/// <summary>
/// Autocomplete da busca. Nem a Riot nem a HenrikDev oferecem busca por nome parcial, então —
/// como o tracker.gg — sugerimos entre os jogadores que já foram consultados aqui.
/// </summary>
public class BuscarJogadores(IJogadorRepository jogadores)
{
    public const int TamanhoMinimo = 2;
    private const int Limite = 8;

    public async Task<IReadOnlyList<Jogador>> ExecutarAsync(string? termo, Regiao? regiao, CancellationToken ct)
    {
        termo = termo?.Trim() ?? "";
        var separador = termo.IndexOf('#');
        var nome = separador >= 0 ? termo[..separador] : termo;
        var tag = separador >= 0 ? termo[(separador + 1)..] : null;

        if (nome.Length < TamanhoMinimo) return [];
        return await jogadores.BuscarAsync(nome, string.IsNullOrEmpty(tag) ? null : tag, regiao, Limite, ct);
    }
}
