using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Application.Miras;

/// <summary>
/// Traz do catálogo externo as miras de pro player e espelha no nosso banco, já decodificadas.
///
/// Decodificar na importação e não na leitura é de propósito: o tipo (cruz, ponto, cruz com
/// ponto) vira coluna e a galeria filtra no banco, em vez de carregar tudo e peneirar na memória.
/// </summary>
public class SincronizarMiras(ICatalogoMiras catalogo, IMiraRepository repositorio)
{
    public async Task<ResultadoSincronizacao> ExecutarAsync(CancellationToken ct)
    {
        var importadas = await catalogo.ObterMirasDeProsAsync(ct);

        var miras = new List<Mira>(importadas.Count);
        var invalidas = 0;

        foreach (var bruta in importadas)
        {
            // Um código torto não pode derrubar a importação inteira: ele fica de fora e o
            // restante entra normalmente.
            if (!CodigoMira.TentarAnalisar(bruta.Codigo, out var configuracao))
            {
                invalidas++;
                continue;
            }

            miras.Add(new Mira(
                bruta.IdExterno,
                bruta.Nome.Trim(),
                bruta.Codigo.Trim(),
                configuracao.Tipo,
                bruta.Copias,
                bruta.CopiasNaSemana));
        }

        // Fonte vazia quase sempre quer dizer que o formato do site mudou, não que os pros
        // apagaram as miras. Apagar o que temos aqui deixaria a aba vazia sem necessidade.
        if (miras.Count == 0) return new ResultadoSincronizacao(0, invalidas, Aplicada: false);

        await repositorio.SincronizarAsync(miras, ct);
        return new ResultadoSincronizacao(miras.Count, invalidas, Aplicada: true);
    }
}

public sealed record ResultadoSincronizacao(int Importadas, int Invalidas, bool Aplicada);
