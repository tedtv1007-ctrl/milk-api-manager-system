using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;

namespace MilkApiManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }
    public DbSet<BlacklistEntry> BlacklistEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<BlacklistEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IpOrCidr).IsRequired();
            entity.Property(e => e.AddedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }
}
