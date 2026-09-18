using CoachGame.Domain.Jogadores;

namespace CoachGame.Application.Portas;

public interface IJogadorRepository
{
    Task<Jogador?> ObterPorPuuidAsync(string puuid, CancellationToken ct);

    /// <summary>Jogadores já consultados cujo nome (e tag, se informada) começa com o termo.</summary>
    Task<IReadOnlyList<Jogador>> BuscarAsync(string prefixoNome, string? prefixoTag, Regiao? regiao, int limite, CancellationToken ct);

    void Adicionar(Jogador jogador);
    void AdicionarSnapshot(SnapshotRR snapshot);
    Task SalvarAsync(CancellationToken ct);
}
