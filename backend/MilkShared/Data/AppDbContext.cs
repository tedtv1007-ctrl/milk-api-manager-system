using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;

namespace MilkApiManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }
    public DbSet<BlacklistEntry> BlacklistEntries { get; set; }
    public DbSet<WhitelistEntry> WhitelistEntries { get; set; }
    public DbSet<PiiMaskingRule> PiiMaskingRules { get; set; }
    public DbSet<NotificationChannel> NotificationChannels { get; set; }
    public DbSet<MockRule> MockRules { get; set; }
    public DbSet<AccessRequest> AccessRequests { get; set; }
    public DbSet<ApiServiceMetadata> ApiServices { get; set; }
    public DbSet<ApiTestScenario> ApiTestScenarios { get; set; }
    public DbSet<SyncOutboxEntry> SyncOutboxEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Owner).IsRequired();
            entity.Property(e => e.KeyHash).IsRequired();
            entity.HasIndex(e => new { e.IsActive, e.ExpiresAt });
            entity.HasIndex(e => e.Owner);
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.ExpiresAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
        
        modelBuilder.Entity<PiiMaskingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.FieldPath).IsRequired();
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Action);
            entity.Property(e => e.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<BlacklistEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IpOrCidr).IsRequired();
            entity.HasIndex(e => e.IpOrCidr).IsUnique();
            entity.Property(e => e.AddedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<WhitelistEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.IpCidr).IsRequired();
            entity.HasIndex(e => e.RouteId);
            entity.Property(e => e.AddedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<SyncOutboxEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => new { e.Status, e.NextAttemptAt });
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.NextAttemptAt).HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
            entity.Property(e => e.ProcessedAt).HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        });
    }
}
