using CoachGame.Application.Portas;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Jogadores;

public sealed record PerfilConsultado(PerfilValorant Perfil, Regiao Regiao, SnapshotRR Snapshot);

/// <summary>
/// Busca o perfil na fonte externa, registra/atualiza o jogador e grava um snapshot de RR.
/// Registrar o jogador é também o que alimenta o autocomplete da busca.
/// </summary>
public class ConsultarPerfil(IValorantDataProvider fonte, IJogadorRepository jogadores, TimeProvider relogio)
{
    public async Task<PerfilConsultado> ExecutarAsync(string entrada, Regiao regiao, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var riotId = RiotId.Parse(entrada);
        var perfil = await fonte.BuscarPerfilAsync(riotId, regiao, ct);

        var jogador = await jogadores.ObterPorPuuidAsync(perfil.Puuid, ct);
        if (jogador is null)
        {
            jogador = new Jogador(perfil.Puuid, perfil.RiotId, regiao, perfil.RankAtual, agora);
            jogadores.Adicionar(jogador);
        }
        else
        {
            jogador.Atualizar(perfil.RiotId, regiao, perfil.RankAtual, agora);
        }

        var snapshot = SnapshotRR.Capturar(jogador.Id, perfil.RankAtual, perfil.HistoricoRecente.ToList(), agora);
        jogadores.AdicionarSnapshot(snapshot);
        await jogadores.SalvarAsync(ct);

        return new PerfilConsultado(perfil, regiao, snapshot);
    }
}
