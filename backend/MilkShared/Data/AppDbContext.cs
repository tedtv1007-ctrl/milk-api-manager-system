using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
    public DbSet<CircuitBreakerConfig> CircuitBreakerConfigs { get; set; }
    public DbSet<CachePolicy> CachePolicies { get; set; }
    public DbSet<RequestTransformRule> RequestTransformRules { get; set; }
    public DbSet<HealthCheckConfig> HealthCheckConfigs { get; set; }
    public DbSet<CanaryRelease> CanaryReleases { get; set; }
    public DbSet<ApiLifecycleEntry> ApiLifecycleEntries { get; set; }

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
            entity.Property(e => e.Timestamp).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            
            // PostgreSQL Optimization: BRIN index for timestamp (great for sequential time-series data)
            entity.HasIndex(e => e.Timestamp)
                  .HasMethod("BRIN");
            
            entity.HasIndex(e => e.Action);

            // PostgreSQL Optimization: Store JSON details as native jsonb
            entity.Property(e => e.DetailsJson)
                  .HasColumnType("jsonb");

            // PostgreSQL Optimization: GIN index for JSONB queries on DetailsJson
            entity.HasIndex(e => e.DetailsJson)
                  .HasMethod("GIN")
                  .HasDatabaseName("IX_AuditLogs_DetailsJson_GIN");

            // PostgreSQL Optimization: Index on User for audit log filtering
            entity.HasIndex(e => e.User)
                  .HasDatabaseName("IX_AuditLogs_User");
        });

        modelBuilder.Entity<BlacklistEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.IpOrCidr)
                  .IsRequired();
            
            entity.HasIndex(e => e.IpOrCidr).IsUnique();
            entity.Property(e => e.AddedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<WhitelistEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RouteId).IsRequired();
            
            entity.Property(e => e.IpCidr)
                  .IsRequired();

            // PostgreSQL Optimization: Composite index for GetWhitelistForRouteAsync query
            entity.HasIndex(e => new { e.RouteId, e.ExpiresAt });
            entity.Property(e => e.AddedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // PostgreSQL Optimization: Partial index for active API keys
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(e => e.KeyHash)
                  .HasFilter("\"IsActive\" = true")
                  .HasDatabaseName("IX_ApiKeys_KeyHash_Active");
        });

        // Index for MockRules route lookups
        modelBuilder.Entity<MockRule>(entity =>
        {
            entity.HasIndex(e => e.RouteId);
        });

        modelBuilder.Entity<SyncOutboxEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => new { e.Status, e.NextAttemptAt });
            
            // PostgreSQL Optimization: Partial index for pending items only
            entity.HasIndex(e => e.CreatedAt)
                  .HasFilter("Status = 'Pending'");

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

        // Circuit Breaker Config
        modelBuilder.Entity<CircuitBreakerConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteId).IsUnique();
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // Cache Policy
        modelBuilder.Entity<CachePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteId).IsUnique();
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // Request Transform Rule
        modelBuilder.Entity<RequestTransformRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RouteId, e.Phase, e.Priority });
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.Phase).IsRequired();
            entity.Property(e => e.OperationType).IsRequired();
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // Health Check Config
        modelBuilder.Entity<HealthCheckConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UpstreamId).IsUnique();
            entity.Property(e => e.UpstreamId).IsRequired();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // Canary Release
        modelBuilder.Entity<CanaryRelease>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteId);
            entity.Property(e => e.RouteId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.MatchRulesJson).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // API Lifecycle
        modelBuilder.Entity<ApiLifecycleEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApiIdentifier, e.Version }).IsUnique();
            entity.Property(e => e.ApiIdentifier).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt).HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }
}
