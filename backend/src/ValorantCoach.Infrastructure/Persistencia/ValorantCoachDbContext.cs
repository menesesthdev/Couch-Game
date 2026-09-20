using ValorantCoach.Domain.Jogadores;
using Microsoft.EntityFrameworkCore;

namespace ValorantCoach.Infrastructure.Persistencia;

public class ValorantCoachDbContext(DbContextOptions<ValorantCoachDbContext> options) : DbContext(options)
{
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<SnapshotRR> Snapshots => Set<SnapshotRR>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Jogador>(e =>
        {
            e.ToTable("jogadores");
            e.HasKey(j => j.Id);
            e.Property(j => j.Puuid).HasMaxLength(100).IsRequired();
            e.HasIndex(j => j.Puuid).IsUnique();
            e.Property(j => j.Regiao).HasConversion<string>().HasMaxLength(10);
            e.ComplexProperty(j => j.RiotId, r =>
            {
                r.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(16);
                r.Property(x => x.Tag).HasColumnName("tag").HasMaxLength(5);
            });
            e.ComplexProperty(j => j.RankAtual, r =>
            {
                r.Property(x => x.Tier).HasColumnName("tier_atual");
                r.Property(x => x.RR).HasColumnName("rr_atual");
            });
        });

        modelBuilder.Entity<SnapshotRR>(e =>
        {
            e.ToTable("snapshots_rr");
            e.HasKey(s => s.Id);
            e.HasOne<Jogador>().WithMany().HasForeignKey(s => s.JogadorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.JogadorId, s.CapturadoEm });
            e.ComplexProperty(s => s.Rank, r =>
            {
                r.Property(x => x.Tier).HasColumnName("tier");
                r.Property(x => x.RR).HasColumnName("rr");
            });
            e.Ignore(s => s.TemHistorico);
        });
    }
}
