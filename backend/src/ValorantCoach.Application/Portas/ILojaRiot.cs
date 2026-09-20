using ValorantCoach.Domain.Jogadores;
using ValorantCoach.Domain.Loja;

namespace ValorantCoach.Application.Portas;

/// <summary>
/// Sessão da conta do jogador nos endpoints do cliente do jogo. Vive pouco (~1h) e nunca é
/// persistida: viaja em cookie criptografado e morre com ele.
/// </summary>
public sealed record SessaoRiot(
    string AccessToken,
    string EntitlementsToken,
    string Puuid,
    Regiao Regiao,
    DateTimeOffset ExpiraEm)
{
    public bool Expirada(DateTimeOffset agora) => agora >= ExpiraEm;
}

/// <summary>
/// Loja pessoal, vinda dos endpoints do próprio cliente do Valorant (não da HenrikDev).
/// Porta separada de <see cref="IValorantDataProvider"/> de propósito: outra fonte, outra
/// autenticação e outro ciclo de vida.
/// </summary>
public interface ILojaRiot
{
    /// <summary>
    /// Troca o access token recebido do login da Riot por uma sessão utilizável
    /// (entitlements + PUUID).
    /// </summary>
    /// <exception cref="SessaoRiotInvalidaException">Token ausente, expirado ou recusado.</exception>
    Task<SessaoRiot> AbrirSessaoAsync(string accessToken, Regiao regiao, CancellationToken ct);

    /// <exception cref="SessaoRiotInvalidaException">Sessão expirada ou recusada pela Riot.</exception>
    /// <exception cref="FonteIndisponivelException">Falha ao falar com a Riot.</exception>
    Task<MinhaLoja> ObterLojaAsync(SessaoRiot sessao, CancellationToken ct);
}

/// <summary>Sessão inválida ou vencida — a UI pede para conectar de novo. Vira HTTP 401.</summary>
public class SessaoRiotInvalidaException(string message) : Exception(message);
