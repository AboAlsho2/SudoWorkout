using Microsoft.EntityFrameworkCore;
using WorkoutService.Domain.Entities;

namespace WorkoutService.Persistence;

public class SudoWorkoutDbContext : DbContext
{
    public SudoWorkoutDbContext(
        DbContextOptions<SudoWorkoutDbContext> options)
        : base(options)
    {
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();

    public DbSet<WorkoutPlanExercise> WorkoutPlanExercises
        => Set<WorkoutPlanExercise>();

    public DbSet<PlanAssignment> PlanAssignments
        => Set<PlanAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkoutPlanExercise>()
            .HasKey(x => new { x.WorkoutPlanId, x.ExerciseId });

        modelBuilder.Entity<PlanAssignment>()
            .HasIndex(x => x.MemberId)
            .IsUnique();
    }
}