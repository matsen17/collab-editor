using CollabEditor.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CollabEditor.Infrastructure.Persistence;

public class CollabEditorDbContext : DbContext
{
    public CollabEditorDbContext(DbContextOptions<CollabEditorDbContext> options) : base(options)
    {
    }

    public DbSet<EditSessionEntity> Sessions => Set<EditSessionEntity>();
    public DbSet<ParticipantEntity> Participants => Set<ParticipantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEditSession(modelBuilder);
        ConfigureParticipant(modelBuilder);
    }
    
    private static void ConfigureEditSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EditSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Content)
                .IsRequired();
            
            entity.Property(e => e.Version)
                .IsRequired();
            
            entity.Property(e => e.IsClosed)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.LastModifiedAt)
                .IsRequired();
            
            entity.HasMany(e => e.Participants)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsClosed);
        });
    }

    private static void ConfigureParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParticipantEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.SessionId)
                .IsRequired();
            
            entity.Property(e => e.ParticipantId)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.JoinedAt)
                .IsRequired();
            
            entity.Property(e => e.LastActiveAt)
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .IsRequired();
            
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ParticipantId);
        });
    }
}