
namespace TrainingService.Contracts.WorkoutService;

public sealed record MemberPlanResponse(
    int Id,
    Guid MemberId,
    int WorkoutPlanId,
    string WorkoutPlanName);
