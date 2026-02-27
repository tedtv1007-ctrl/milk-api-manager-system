using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;

namespace MilkApiManager.Data;

public class AuditContext : DbContext
{
    public AuditContext(DbContextOptions<AuditContext> options) : base(options) { }

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntry>().Ignore(e => e.Details);
        base.OnModelCreating(modelBuilder);
    }
}
