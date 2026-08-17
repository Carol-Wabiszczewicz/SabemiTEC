using Microsoft.EntityFrameworkCore;
using SabemiTec.Api.Models;

namespace SabemiTec.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EventoLog> EventosLog => Set<EventoLog>();
    public DbSet<StatusContrato> StatusContratos => Set<StatusContrato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventoLog>(entity =>
        {
            entity.HasIndex(e => e.IdTransacao).IsUnique();
            entity.Property(e => e.StatusProcessamento).HasConversion<string>();
        });

        modelBuilder.Entity<StatusContrato>(entity =>
        {
            entity.HasIndex(e => e.IdContrato).IsUnique();
        });
    }
}
