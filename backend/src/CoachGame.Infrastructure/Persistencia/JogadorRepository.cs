using CoachGame.Application.Portas;
using CoachGame.Domain.Jogadores;
using Microsoft.EntityFrameworkCore;

namespace CoachGame.Infrastructure.Persistencia;

public class JogadorRepository(CoachGameDbContext db) : IJogadorRepository
{
    public Task<Jogador?> ObterPorPuuidAsync(string puuid, CancellationToken ct) =>
        db.Jogadores.FirstOrDefaultAsync(j => j.Puuid == puuid, ct);

    public async Task<IReadOnlyList<Jogador>> BuscarAsync(
        string prefixoNome, string? prefixoTag, Regiao? regiao, int limite, CancellationToken ct)
    {
        var consulta = db.Jogadores.AsNoTracking()
            .Where(j => EF.Functions.ILike(j.RiotId.Nome, Escapar(prefixoNome) + "%"));
        if (prefixoTag is not null)
            consulta = consulta.Where(j => EF.Functions.ILike(j.RiotId.Tag, Escapar(prefixoTag) + "%"));
        if (regiao is not null)
            consulta = consulta.Where(j => j.Regiao == regiao);

        return await consulta.OrderByDescending(j => j.AtualizadoEm).Take(limite).ToListAsync(ct);
    }

    /// <summary>Evita que % e _ digitados pelo usuário virem curingas do LIKE.</summary>
    private static string Escapar(string termo) =>
        termo.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public void Adicionar(Jogador jogador) => db.Jogadores.Add(jogador);

    public void AdicionarSnapshot(SnapshotRR snapshot) => db.Snapshots.Add(snapshot);

    public Task SalvarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
