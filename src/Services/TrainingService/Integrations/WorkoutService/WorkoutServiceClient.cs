
using System.Net.Http.Json;
using TrainingService.Contracts.WorkoutService;

namespace TrainingService.Integrations.WorkoutService;

public sealed class WorkoutServiceClient(HttpClient httpClient)
{

    public async Task<MemberPlanResponse?> GetMemberPlanAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/members/{memberId}/plan",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MemberPlanResponse>(
            cancellationToken: cancellationToken);
    }



    public async Task<List<WorkoutPlanExerciseResponse>?> GetWorkoutPlanExercisesAsync(
        int workoutPlanId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/workout-plans/{workoutPlanId}/exercises",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<WorkoutPlanExerciseResponse>>(
                cancellationToken: cancellationToken);
    }


}

