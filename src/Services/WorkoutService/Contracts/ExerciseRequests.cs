
namespace WorkoutService.Contracts;

public record CreateExerciseRequest(
    string Name,
    string? Description);

public record UpdateExerciseRequest(
    string Name,
    string? Description);