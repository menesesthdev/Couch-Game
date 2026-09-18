using CoachGame.Domain.Comum;
using CoachGame.Domain.Jogadores;

namespace CoachGame.Domain.Estimativas;

/// <summary>Rank-alvo + data-limite definidos pelo usuário.</summary>
public sealed record Meta
{
    public Tier RankAlvo { get; }
    public DateOnly DataLimite { get; }

    public Meta(Tier rankAlvo, DateOnly dataLimite, DateOnly hoje)
    {
        if (!rankAlvo.Ranqueado()) throw new DomainException("Escolha um rank-alvo válido.");
        if (dataLimite <= hoje) throw new DomainException("A data-limite precisa ser no futuro.");
        RankAlvo = rankAlvo;
        DataLimite = dataLimite;
    }
}
