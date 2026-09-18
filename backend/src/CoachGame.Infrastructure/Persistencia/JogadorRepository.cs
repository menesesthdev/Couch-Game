using CoachGame.Application.Portas;
using CoachGame.Domain.Jogadores;
using Microsoft.EntityFrameworkCore;

namespace CoachGame.Infrastructure.Persistencia;

public class JogadorRepository(CoachGameDbContext db) : IJogadorRepository
{
    public Task<Jogador?> ObterPorPuuidAsync(string puuid, CancellationToken ct) =>
        db.Jogadores.FirstOrDefaultAsync(j => j.Puuid == puuid, ct);

    public void Adicionar(Jogador jogador) => db.Jogadores.Add(jogador);

    public void AdicionarSnapshot(SnapshotRR snapshot) => db.Snapshots.Add(snapshot);

    public Task SalvarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
