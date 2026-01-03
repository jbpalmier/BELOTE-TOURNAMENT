using BeloteTournament.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeloteTournament.Infrastructure.Persistence;

/// <summary>
/// DbContext EF Core (SQLite).
/// La configuration est faite ici en Fluent API.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public DbSet<Team> Teams => Set<Team>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name).HasMaxLength(40).IsRequired();

            entity.HasIndex(t => t.Name).IsUnique();
        });
    }
}
