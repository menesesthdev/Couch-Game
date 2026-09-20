using System.Text.RegularExpressions;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Comum;
using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Application.Loja;

/// <summary>
/// Abre a sessão a partir da URL de redirecionamento que o jogador cola.
///
/// O login acontece na página oficial da Riot: a senha nunca passa por aqui. Como o
/// <c>client_id=riot-client</c> obriga <c>redirect_uri=http://localhost/redirect</c>, o navegador
/// cai numa página de erro e o jogador copia a URL inteira — o token vem no fragmento (<c>#</c>),
/// que por definição nunca é enviado a servidor nenhum até ele colar aqui.
/// </summary>
public partial class ConectarConta(ILojaRiot loja)
{
    public Task<SessaoRiot> ExecutarAsync(string urlRedirecionamento, Regiao regiao, CancellationToken ct)
    {
        var token = ExtrairAccessToken(urlRedirecionamento);
        return loja.AbrirSessaoAsync(token, regiao, ct);
    }

    /// <summary>
    /// Aceita a URL inteira (<c>http://localhost/redirect#access_token=...&amp;expires_in=3600</c>)
    /// ou só o token colado sozinho.
    /// </summary>
    public static string ExtrairAccessToken(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
            throw new DomainException("Cole a URL que o navegador abriu depois do login.");

        var texto = entrada.Trim();

        var casamento = AccessTokenRegex().Match(texto);
        if (casamento.Success) return casamento.Groups["token"].Value;

        // Token colado sem a URL em volta: três blocos base64url separados por ponto.
        if (JwtRegex().IsMatch(texto)) return texto;

        // A URL pode ser a de um login que deu errado. Repetir o motivo da Riot ajuda mais
        // do que dizer que o token não foi encontrado.
        var erro = ErroRegex().Match(texto);
        if (erro.Success)
        {
            if (erro.Groups["codigo"].Value.Equals("access_denied", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("O login foi cancelado na página da Riot. Tente de novo.");

            var descricao = DescricaoErroRegex().Match(texto);
            throw new DomainException(descricao.Success
                ? $"A Riot recusou esse login: {Decodificar(descricao.Groups["texto"].Value)}"
                : $"A Riot recusou esse login ({erro.Groups["codigo"].Value}).");
        }

        throw new DomainException(
            "Não encontramos o token nessa URL. Copie a barra de endereço inteira da página de erro, " +
            "incluindo o trecho depois do #.");
    }

    /// <summary>Parâmetros de URL usam "+" para espaço, que o UnescapeDataString não converte.</summary>
    private static string Decodificar(string valor) => Uri.UnescapeDataString(valor.Replace("+", " "));

    [GeneratedRegex(@"access_token=(?<token>[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+)")]
    private static partial Regex AccessTokenRegex();

    [GeneratedRegex(@"[#&?]error=(?<codigo>[^&\s]+)")]
    private static partial Regex ErroRegex();

    [GeneratedRegex(@"error_description=(?<texto>[^&\s]+)")]
    private static partial Regex DescricaoErroRegex();

    [GeneratedRegex(@"^[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+$")]
    private static partial Regex JwtRegex();
}
