using ValorantCoach.Application.Calendario;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Calendario;
using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Partidas;

namespace ValorantCoach.Application.Partidas;

/// <summary>Partidas do período pedido, já com RR, e o resumo calculado em cima delas.</summary>
public sealed record DesempenhoAnalisado(
    PeriodoAnalise Periodo,
    AtoCompetitivo? Ato,
    IReadOnlyList<ResumoPartida> Partidas,
    EstatisticasRecentes Estatisticas);

/// <summary>
/// Junta as duas fontes de partida — desempenho (agente, abates, dano) e histórico de RR — casando
/// pelo match id, e resume o resultado.
///
/// Fica separado de <c>ConsultarPerfil</c> de propósito: são chamadas extras à fonte externa e a
/// página abre sem elas, então uma falha aqui não derruba o perfil inteiro.
/// </summary>
public class AnalisarDesempenho(IValorantDataProvider fonte, ObterAtoAtual obterAtoAtual)
{
    /// <summary>Quantas partidas o período padrão analisa.</summary>
    public const int PartidasPadrao = 20;

    /// <summary>
    /// Teto de páginas ao varrer um ato inteiro. 300 partidas cobrem com folga o ato de qualquer
    /// jogador humano e limitam o estrago no rate limit se a fonte devolver algo inesperado.
    /// </summary>
    private const int TamanhoPagina = 100;
    private const int MaximoPaginas = 3;

    public async Task<DesempenhoAnalisado> ExecutarAsync(
        string entrada, Regiao regiao, PeriodoAnalise periodo, CancellationToken ct)
    {
        var riotId = RiotId.Parse(entrada);

        var ato = periodo == PeriodoAnalise.AtoAtual
            ? await AtoAtualAsync(ct)
            : null;

        // Sem ato em andamento não há o que varrer: cai no comportamento padrão.
        if (periodo == PeriodoAnalise.AtoAtual && ato is null) periodo = PeriodoAnalise.Ultimas;

        var partidas = periodo == PeriodoAnalise.AtoAtual
            ? await PartidasDoAtoAsync(riotId, regiao, ato!, ct)
            : await PartidasRecentesAsync(riotId, regiao, ct);

        return new DesempenhoAnalisado(periodo, ato, partidas, EstatisticasRecentes.Calcular(partidas));
    }

    private async Task<AtoCompetitivo?> AtoAtualAsync(CancellationToken ct)
    {
        try
        {
            return await obterAtoAtual.ExecutarAsync(ct);
        }
        catch (FonteIndisponivelException)
        {
            // O calendário é um extra; sem ele a tela ainda funciona com as últimas partidas.
            return null;
        }
    }

    private async Task<IReadOnlyList<ResumoPartida>> PartidasRecentesAsync(RiotId riotId, Regiao regiao, CancellationToken ct)
    {
        var desempenho = await fonte.BuscarPartidasAsync(riotId, regiao, PartidasPadrao, 1, ct);
        var rr = await fonte.BuscarHistoricoRrAsync(riotId, regiao, PartidasPadrao, 1, ct);
        return Casar(desempenho, rr);
    }

    private async Task<IReadOnlyList<ResumoPartida>> PartidasDoAtoAsync(
        RiotId riotId, Regiao regiao, AtoCompetitivo ato, CancellationToken ct)
    {
        var desempenho = await VarrerAtoAsync(
            (pagina, c) => fonte.BuscarPartidasAsync(riotId, regiao, TamanhoPagina, pagina, c),
            p => p.Data, ato, ct);

        var rr = await VarrerAtoAsync(
            (pagina, c) => fonte.BuscarHistoricoRrAsync(riotId, regiao, TamanhoPagina, pagina, c),
            p => p.Data, ato, ct);

        return Casar(desempenho, rr);
    }

    /// <summary>
    /// Pede páginas até a fonte entregar algo anterior ao início do ato (ou acabar), e devolve só o
    /// que caiu dentro da janela. As páginas vêm da mais recente para a mais antiga.
    /// </summary>
    private static async Task<List<T>> VarrerAtoAsync<T>(
        Func<int, CancellationToken, Task<IReadOnlyList<T>>> buscarPagina,
        Func<T, DateTimeOffset> data,
        AtoCompetitivo ato,
        CancellationToken ct)
    {
        var coletadas = new List<T>();
        for (var pagina = 1; pagina <= MaximoPaginas; pagina++)
        {
            var lote = await buscarPagina(pagina, ct);
            if (lote.Count == 0) break;

            coletadas.AddRange(lote.Where(x => data(x) >= ato.Inicio && data(x) <= ato.Fim));

            // Chegou em partida anterior ao ato: daqui para trás é tudo mais antigo.
            if (lote.Any(x => data(x) < ato.Inicio)) break;
            if (lote.Count < TamanhoPagina) break;
        }
        return coletadas;
    }

    /// <summary>
    /// Casa desempenho com RR pelo match id. Partida sem entrada de RR entra mesmo assim, só que sem
    /// a variação — é o que acontece em partidas de colocação e quando as fontes ficam dessincronizadas.
    /// </summary>
    private static List<ResumoPartida> Casar(IReadOnlyList<ResumoPartida> partidas, IReadOnlyList<PartidaRR> historicoRr)
    {
        var porMatchId = historicoRr
            .Where(r => !string.IsNullOrEmpty(r.MatchId))
            .GroupBy(r => r.MatchId!)
            .ToDictionary(g => g.Key, g => g.First());

        return partidas
            .Select(p => porMatchId.TryGetValue(p.MatchId, out var rr) ? p.ComRr(rr) : p)
            .OrderByDescending(p => p.Data)
            .ToList();
    }
}
