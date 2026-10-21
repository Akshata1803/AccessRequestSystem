using AccessRequestSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);

            // Foreign Key to Role
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Resource configuration
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // AccessRequest configuration
        modelBuilder.Entity<AccessRequest>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Justification).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.GrantTokenHash).HasMaxLength(128);

            // Store enum as string in the database
            entity.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Requested by User
            entity.HasOne(a => a.Requester)
                .WithMany(u => u.RequestedAccesses)
                .HasForeignKey(a => a.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decided by User (optional until decision made)
            entity.HasOne(a => a.DecidedBy)
                .WithMany(u => u.DecidedAccesses)
                .HasForeignKey(a => a.DecidedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Target Resource
            entity.HasOne(a => a.Resource)
                .WithMany(r => r.AccessRequests)
                .HasForeignKey(a => a.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for fast lookup on status and expiration checks
            entity.HasIndex(a => a.Status).HasDatabaseName("IX_AccessRequests_Status");
            entity.HasIndex(a => a.ExpiresAt).HasDatabaseName("IX_AccessRequests_ExpiresAt");
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Action).IsRequired().HasMaxLength(100);
            entity.Property(l => l.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Details).IsRequired().HasMaxLength(2000);

            // Nullable foreign key for actor user (null for automated worker events)
            entity.HasOne(l => l.ActorUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(l => l.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
