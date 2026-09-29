using System.Globalization;
using ValorantCoach.Domain.Comum;

namespace ValorantCoach.Domain.Miras;

/// <summary>
/// Decodifica o código de mira do Valorant — aquele texto que o jogo exporta e importa,
/// tipo <c>0;P;c;5;h;0;0l;4;0o;2;0a;1;0f;0;1b;0</c>.
///
/// O formato é uma sequência de pares chave;valor separados por ponto e vírgula. Tokens soltos
/// (<c>P</c>, <c>A</c>, <c>S</c>, <c>NAME</c>) trocam de seção: perfil primário, mira de ADS,
/// mira telescópica e nome. Só o primário importa aqui — é o que a galeria mostra.
///
/// Chave que não aparece no código fica no padrão do jogo, e é por isso que <c>0</c> sozinho
/// é um código válido: significa "tudo como vem de fábrica". Chave desconhecida é ignorada,
/// mas o valor dela é consumido, senão os pares seguintes saem todos deslocados.
/// </summary>
public static class CodigoMira
{
    /// <summary>Cores predefinidas do jogo, na ordem em que o código as numera (0 a 7).</summary>
    private static readonly string[] Cores =
        ["#FFFFFF", "#00FF00", "#7FFF00", "#DFFF00", "#FFFF00", "#00FFFF", "#FF00FF", "#FF0000"];

    /// <summary>Índice reservado para "cor personalizada", que vem no hexadecimal da chave <c>u</c>.</summary>
    private const int CorPersonalizada = 8;

    public static ConfiguracaoMira Analisar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("Cole um código de mira do Valorant.");

        // Padrões do jogo. Alterados abaixo só pelo que o código trouxer.
        var indiceCor = 0;
        string? corCustomizada = null;
        var contornoVisivel = true;
        var contornoEspessura = 1.0;
        var contornoOpacidade = 0.5;
        var pontoVisivel = false;
        var pontoEspessura = 2.0;
        var pontoOpacidade = 1.0;

        var internas = new Acumulador(Visivel: true, Opacidade: 0.8, Comprimento: 6, Vertical: 6, Espessura: 2, Deslocamento: 3);
        var externas = new Acumulador(Visivel: true, Opacidade: 0.35, Comprimento: 2, Vertical: 2, Espessura: 2, Deslocamento: 10);

        var partes = codigo.Split(';', StringSplitOptions.TrimEntries);
        var noPrimario = false;

        for (var i = 0; i < partes.Length; i++)
        {
            var token = partes[i];

            // Marcadores de seção vêm sozinhos, sem valor.
            if (token is "P") { noPrimario = true; continue; }
            if (token is "A" or "S" or "NAME") { noPrimario = false; continue; }
            if (!noPrimario || i + 1 >= partes.Length) continue;

            var valor = partes[++i];

            // Linhas usam a mesma letra das opções gerais, separadas pelo prefixo:
            // 0 = internas, 1 = externas.
            if (token.Length > 1 && (token[0] is '0' or '1'))
            {
                var alvo = token[0] == '0' ? internas : externas;
                switch (token[1..])
                {
                    case "b": alvo.Visivel = Booleano(valor); break;
                    case "a": alvo.Opacidade = Numero(valor, alvo.Opacidade); break;
                    case "l": alvo.Comprimento = Numero(valor, alvo.Comprimento); break;
                    case "v": alvo.Vertical = Numero(valor, alvo.Vertical); break;
                    case "g": alvo.Destravado = Booleano(valor); break;
                    case "t": alvo.Espessura = Numero(valor, alvo.Espessura); break;
                    case "o": alvo.Deslocamento = Numero(valor, alvo.Deslocamento); break;
                }
                continue;
            }

            switch (token)
            {
                case "c": indiceCor = (int)Numero(valor, indiceCor); break;
                case "u": corCustomizada = valor; break;
                case "h": contornoVisivel = Booleano(valor); break;
                case "t": contornoEspessura = Numero(valor, contornoEspessura); break;
                case "o": contornoOpacidade = Numero(valor, contornoOpacidade); break;
                case "d": pontoVisivel = Booleano(valor); break;
                case "z": pontoEspessura = Numero(valor, pontoEspessura); break;
                case "a": pontoOpacidade = Numero(valor, pontoOpacidade); break;
            }
        }

        return new ConfiguracaoMira(
            Cor: ResolverCor(indiceCor, corCustomizada),
            Contorno: new ContornoMira(contornoVisivel, contornoEspessura, contornoOpacidade),
            Ponto: new PontoCentral(pontoVisivel, pontoEspessura, pontoOpacidade),
            Internas: internas.Concluir(),
            Externas: externas.Concluir());
    }

    /// <summary>Analisa sem estourar: útil ao importar em lote, onde um código torto não pode derrubar o resto.</summary>
    public static bool TentarAnalisar(string? codigo, out ConfiguracaoMira configuracao)
    {
        try
        {
            configuracao = Analisar(codigo);
            return true;
        }
        catch (DomainException)
        {
            configuracao = null!;
            return false;
        }
    }

    /// <summary>
    /// O índice 8 quer dizer "cor personalizada", que vem em <c>u</c> como RRGGBB ou RRGGBBAA —
    /// o alfa é descartado, porque a opacidade já vem em campo próprio.
    /// </summary>
    private static string ResolverCor(int indice, string? customizada)
    {
        if (indice == CorPersonalizada && !string.IsNullOrWhiteSpace(customizada))
        {
            var hex = customizada.Trim().TrimStart('#');
            if (hex.Length >= 6 && hex[..6].All(Uri.IsHexDigit)) return $"#{hex[..6].ToUpperInvariant()}";
        }

        return indice >= 0 && indice < Cores.Length ? Cores[indice] : Cores[0];
    }

    private static bool Booleano(string valor) => valor is not ("0" or "");

    /// <summary>Os valores usam ponto decimal (<c>0.35</c>), então a cultura tem de ser fixa.</summary>
    private static double Numero(string valor, double padrao) =>
        double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : padrao;

    /// <summary>Mutável só durante a leitura; vira o record imutável no fim.</summary>
    private sealed record Acumulador(
        bool Visivel, double Opacidade, double Comprimento, double Vertical, double Espessura, double Deslocamento)
    {
        public bool Visivel { get; set; } = Visivel;
        public double Opacidade { get; set; } = Opacidade;
        public double Comprimento { get; set; } = Comprimento;
        public double Vertical { get; set; } = Vertical;
        public double Espessura { get; set; } = Espessura;
        public double Deslocamento { get; set; } = Deslocamento;

        /// <summary>Comprimentos destravados: as linhas verticais passam a usar <c>v</c>.</summary>
        public bool Destravado { get; set; }

        public LinhasMira Concluir() =>
            new(Visivel, Comprimento, Destravado ? Vertical : Comprimento, Espessura, Deslocamento, Opacidade);
    }
}
