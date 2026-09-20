using ValorantCoach.Application.Jogadores;
using ValorantCoach.Domain.Estimativas;
using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Application.Estimativas;

public sealed record EstimarProgressaoComando(string Perfil, Regiao Regiao, Tier RankAlvo, DateOnly DataLimite);

public sealed record EstimativaResultado(
    PerfilConsultado Consulta,
    Meta Meta,
    int RrFaltando,
    bool UsouMediasPadrao,
    IReadOnlyList<CenarioEstimativa> Cenarios,
    RequisitosPrazo Requisitos);

/// <summary>Caso de uso principal: consulta o jogador e calcula os cenários para a meta.</summary>
public class EstimarProgressao(ConsultarPerfil consultarPerfil, TimeProvider relogio)
{
    public async Task<EstimativaResultado> ExecutarAsync(EstimarProgressaoComando comando, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var meta = new Meta(comando.RankAlvo, comando.DataLimite, hoje);

        var consulta = await consultarPerfil.ExecutarAsync(comando.Perfil, comando.Regiao, ct);
        var rank = consulta.Perfil.RankAtual;

        return new EstimativaResultado(
            consulta,
            meta,
            rank.RrAte(meta.RankAlvo),
            !consulta.Snapshot.TemHistorico,
            CalculadoraEstimativa.Calcular(rank, meta, consulta.Snapshot, hoje),
            CalculadoraEstimativa.Requisitos(rank, meta, consulta.Snapshot, hoje));
    }
}
