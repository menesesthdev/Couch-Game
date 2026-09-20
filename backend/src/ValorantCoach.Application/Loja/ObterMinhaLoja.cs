using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Loja;

namespace ValorantCoach.Application.Loja;

/// <summary>A loja da conta conectada. Nada aqui toca o lado de progressão de rank.</summary>
public class ObterMinhaLoja(ILojaRiot loja, TimeProvider relogio)
{
    public Task<MinhaLoja> ExecutarAsync(SessaoRiot sessao, CancellationToken ct)
    {
        if (sessao.Expirada(relogio.GetUtcNow()))
            throw new SessaoRiotInvalidaException("Sua conexão com a Riot expirou. Conecte de novo.");

        return loja.ObterLojaAsync(sessao, ct);
    }
}
