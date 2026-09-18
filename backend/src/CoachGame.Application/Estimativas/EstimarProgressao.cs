using CoachGame.Application.Portas;
using CoachGame.Domain.Estimativas;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Estimativas;

public sealed record EstimarProgressaoComando(string Perfil, Regiao Regiao, Tier RankAlvo, DateOnly DataLimite);

public sealed record EstimativaResultado(
    string RiotId,
    Rank RankAtual,
    Meta Meta,
    int RrFaltando,
    SnapshotRR Snapshot,
    bool UsouMediasPadrao,
    IReadOnlyList<CenarioEstimativa> Cenarios);

/// <summary>
/// Caso de uso principal: resolve o jogador, registra um snapshot de RR e calcula os cenários.
/// </summary>
public class EstimarProgressao(IValorantDataProvider fonte, IJogadorRepository jogadores, TimeProvider relogio)
{
    public async Task<EstimativaResultado> ExecutarAsync(EstimarProgressaoComando comando, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);

        var riotId = RiotId.Parse(comando.Perfil);
        var meta = new Meta(comando.RankAlvo, comando.DataLimite, hoje);
        var perfil = await fonte.BuscarPerfilAsync(riotId, comando.Regiao, ct);

        var jogador = await jogadores.ObterPorPuuidAsync(perfil.Puuid, ct);
        if (jogador is null)
        {
            jogador = new Jogador(perfil.Puuid, perfil.RiotId, comando.Regiao, perfil.RankAtual, agora);
            jogadores.Adicionar(jogador);
        }
        else
        {
            jogador.Atualizar(perfil.RiotId, comando.Regiao, perfil.RankAtual, agora);
        }

        var snapshot = SnapshotRR.Capturar(jogador.Id, perfil.RankAtual, perfil.HistoricoRecente.ToList(), agora);
        jogadores.AdicionarSnapshot(snapshot);
        await jogadores.SalvarAsync(ct);

        var cenarios = CalculadoraEstimativa.Calcular(perfil.RankAtual, meta, snapshot, hoje);
        return new EstimativaResultado(
            perfil.RiotId.ToString(), perfil.RankAtual, meta, perfil.RankAtual.RrAte(meta.RankAlvo),
            snapshot, !snapshot.TemHistorico, cenarios);
    }
}
