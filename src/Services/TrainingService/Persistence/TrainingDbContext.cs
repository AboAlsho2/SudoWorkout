
using Microsoft.EntityFrameworkCore;
using TrainingService.Domain.Entities;

namespace TrainingService.Persistence;

public class TrainingDbContext(
    DbContextOptions<TrainingDbContext> options) : DbContext(options)
{
    public DbSet<TrainingSession> TrainingSessions =>
        Set<TrainingSession>();

    public DbSet<TrainingSessionExercise> TrainingSessionExercises =>
        Set<TrainingSessionExercise>();

    public DbSet<OutboxMessage> OutboxMessages =>
    Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrainingSession>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.WorkoutPlanName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasMany(x => x.Exercises)
                .WithOne(x => x.TrainingSession)
                .HasForeignKey(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.MemberId, x.StartedAtUtc });
        });


        modelBuilder.Entity<TrainingSessionExercise>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExerciseName)
                .HasMaxLength(200)
                .IsRequired();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_TrainingSessionExercises_PlannedSets",
                    "[PlannedSets] > 0");

                table.HasCheckConstraint(
                    "CK_TrainingSessionExercises_PlannedReps",
                    "[PlannedReps] > 0");
            });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.ProcessedAtUtc,
                x.OccurredAtUtc
            });
        });

    }
}
