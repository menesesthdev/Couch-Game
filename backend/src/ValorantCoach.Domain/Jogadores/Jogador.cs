namespace ValorantCoach.Domain.Jogadores;

public class Jogador
{
    public Guid Id { get; private set; }
    public string Puuid { get; private set; } = null!;
    public RiotId RiotId { get; private set; } = null!;
    public Regiao Regiao { get; private set; }
    public Rank RankAtual { get; private set; } = null!;
    public DateTimeOffset AtualizadoEm { get; private set; }

    private Jogador() { } // EF

    public Jogador(string puuid, RiotId riotId, Regiao regiao, Rank rankAtual, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        Puuid = puuid;
        RiotId = riotId;
        Regiao = regiao;
        RankAtual = rankAtual;
        AtualizadoEm = agora;
    }

    /// <summary>Riot ID pode mudar (troca de nome); o PUUID é a identidade estável.</summary>
    public void Atualizar(RiotId riotId, Regiao regiao, Rank rankAtual, DateTimeOffset agora)
    {
        RiotId = riotId;
        Regiao = regiao;
        RankAtual = rankAtual;
        AtualizadoEm = agora;
    }
}
