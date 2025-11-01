using Microsoft.EntityFrameworkCore;

namespace Owlet.Infrastructure.Database;

/// <summary>
/// Main database context for Owlet service.
/// This is a stub implementation - full implementation will be in E20 (Core Service).
/// </summary>
public sealed class OwletDbContext : DbContext
{
    public OwletDbContext(DbContextOptions<OwletDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Documents table - stub for health check queries.
    /// Full implementation in E20.
    /// </summary>
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Stub configuration - will be expanded in E20
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });
    }
}

/// <summary>
/// Stub document entity for health check queries.
/// Full implementation in E20.
/// </summary>
public sealed class DocumentEntity
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
}
