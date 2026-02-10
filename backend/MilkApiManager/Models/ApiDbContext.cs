using Microsoft.EntityFrameworkCore;

namespace MilkApiManager.Models
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<Quota> Quotas { get; set; }
        public DbSet<UsageRecord> UsageRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiKey>()
                .HasIndex(k => k.KeyHash)
                .IsUnique();

            modelBuilder.Entity<Quota>()
                .HasOne(q => q.ApiKey)
                .WithOne(k => k.Quota)
                .HasForeignKey<Quota>(q => q.ApiKeyId);

            modelBuilder.Entity<UsageRecord>()
                .HasOne(u => u.ApiKey)
                .WithMany()
                .HasForeignKey(u => u.ApiKeyId);
        }
    }
}