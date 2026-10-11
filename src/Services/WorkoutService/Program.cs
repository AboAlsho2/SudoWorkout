
using Microsoft.EntityFrameworkCore;
using WorkoutService.Endpoints;
using WorkoutService.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SudoWorkoutDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/", () => "Workout Service is running!");

app.MapExerciseEndpoints();
app.MapWorkoutPlanEndpoints();
app.MapMemberPlanEndpoints();


app.Run();
public partial class Program
{
}