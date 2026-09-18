using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Portas;

public interface IJogadorRepository
{
    Task<Jogador?> ObterPorPuuidAsync(string puuid, CancellationToken ct);
    void Adicionar(Jogador jogador);
    void AdicionarSnapshot(SnapshotRR snapshot);
    Task SalvarAsync(CancellationToken ct);
}
