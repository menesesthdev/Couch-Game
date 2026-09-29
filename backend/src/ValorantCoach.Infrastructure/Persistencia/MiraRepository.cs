using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Miras;
using Microsoft.EntityFrameworkCore;

namespace ValorantCoach.Infrastructure.Persistencia;

public class MiraRepository(ValorantCoachDbContext db) : IMiraRepository
{
    public async Task<IReadOnlyList<Mira>> ListarAsync(TipoMira? tipo, string? busca, CancellationToken ct)
    {
        var consulta = db.Miras.AsNoTracking();

        if (tipo is not null)
            consulta = consulta.Where(m => m.Tipo == tipo);

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(m => EF.Functions.ILike(m.Nome, "%" + Escapar(busca) + "%"));

        // Mais copiada primeiro: é o mais próximo que temos de "mira que as pessoas realmente usam".
        return await consulta.OrderByDescending(m => m.Copias).ThenBy(m => m.Nome).ToListAsync(ct);
    }

    public Task<int> ContarAsync(CancellationToken ct) => db.Miras.CountAsync(ct);

    /// <summary>
    /// Espelha a fonte: remove o que saiu de lá, insere o que é novo e atualiza o resto. Fica numa
    /// transação para a galeria nunca ser lida no meio da troca, com metade das miras.
    /// </summary>
    public async Task SincronizarAsync(IReadOnlyList<Mira> miras, CancellationToken ct)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var atuais = await db.Miras.ToDictionaryAsync(m => m.Id, ct);
        var novos = miras.ToDictionary(m => m.Id);

        foreach (var (id, existente) in atuais)
            if (!novos.ContainsKey(id))
                db.Miras.Remove(existente);

        foreach (var mira in miras)
        {
            if (atuais.TryGetValue(mira.Id, out var existente))
                db.Entry(existente).CurrentValues.SetValues(mira);
            else
                db.Miras.Add(mira);
        }

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
    }

    /// <summary>Evita que % e _ digitados pelo usuário virem curingas do LIKE.</summary>
    private static string Escapar(string termo) =>
        termo.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
