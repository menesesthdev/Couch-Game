using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ValorantCoach.Application.Portas;
using Microsoft.Extensions.Logging;

namespace ValorantCoach.Infrastructure.Vcrdb;

/// <summary>
/// Importa as miras de pro player do vcrdb.net.
///
/// Eles não publicam API. O site é Next.js e a home já traz o catálogo inteiro embutido no
/// payload de renderização, como objetos <c>{"id","name","code","tags","copied","weeklyCopies"}</c>.
/// É de lá que lemos — filtrando <c>tags == "team"</c>, que é justamente o recorte de pro player.
/// O resto do catálogo é envio da comunidade e fica de fora de propósito: queremos a lista de
/// pros, não um espelho do banco deles.
///
/// Por ser leitura de HTML e não de contrato publicado, isto quebra sem aviso. Quem chama tem de
/// tratar falha como "não atualizou hoje" — as miras já importadas continuam no nosso banco.
/// A sincronização roda uma vez por dia, então o site deles recebe uma requisição diária, não uma
/// por visitante.
/// </summary>
public partial class VcrdbCatalogoMiras(HttpClient http, ILogger<VcrdbCatalogoMiras> logger) : ICatalogoMiras
{
    /// <summary>A tag que o vcrdb usa para separar mira de time/pro do envio da comunidade.</summary>
    private const string TagDePro = "team";

    public async Task<IReadOnlyList<MiraImportada>> ObterMirasDeProsAsync(CancellationToken ct)
    {
        string html;
        try
        {
            html = await http.GetStringAsync("/", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new FonteIndisponivelException("Não foi possível falar com o vcrdb.net agora.", ex);
        }

        // No payload do Next.js as aspas vêm escapadas; desfazer isso deixa os objetos como JSON
        // de verdade, que o serializador consegue ler (inclusive acentos e aspas no nome).
        var payload = html.Replace("\\\"", "\"");

        var miras = new List<MiraImportada>();
        var vistos = new HashSet<int>();

        foreach (var casamento in MiraRegex().Matches(payload).Cast<Match>())
        {
            VcrdbMira? bruta;
            try
            {
                bruta = JsonSerializer.Deserialize<VcrdbMira>(casamento.Value);
            }
            catch (JsonException)
            {
                continue;
            }

            if (bruta is null || !string.Equals(bruta.Tags, TagDePro, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(bruta.Name) || string.IsNullOrWhiteSpace(bruta.Code)) continue;
            // A home repete a mesma mira em blocos diferentes ("mais copiadas", "recentes").
            if (!vistos.Add(bruta.Id)) continue;

            miras.Add(new MiraImportada(bruta.Id, bruta.Name, bruta.Code, bruta.Copied, bruta.WeeklyCopies));
        }

        if (miras.Count == 0)
            logger.LogWarning("vcrdb: nenhuma mira de pro encontrada no payload — o formato do site provavelmente mudou.");

        return miras;
    }

    /// <summary>
    /// Casa um objeto de mira inteiro no payload. Os campos vêm sempre nesta ordem; o nome aceita
    /// escapes (<c>\"</c>, <c>é</c>) porque quem decodifica de fato é o JsonSerializer.
    /// </summary>
    [GeneratedRegex(
        """\{"id":\d+,"name":"(?:[^"\\]|\\.)*","code":"[^"]*","tags":"[^"]*","copied":\d+,"weeklyCopies":\d+\}""",
        RegexOptions.Compiled)]
    private static partial Regex MiraRegex();

    private sealed record VcrdbMira(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("tags")] string? Tags,
        [property: JsonPropertyName("copied")] int Copied,
        [property: JsonPropertyName("weeklyCopies")] int WeeklyCopies);
}
