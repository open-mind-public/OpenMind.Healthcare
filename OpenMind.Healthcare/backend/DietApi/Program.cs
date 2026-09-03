using System.Text;
using System.Text.Json.Serialization;
using DietApi.Domain.Repositories;
using DietApi.Domain.Observations;
using DietApi.Domain.Observations.Rules;
using DietApi.Domain.Services;
using DietApi.Features.ActivityCatalogue;
using DietApi.Features.DietPlan;
using DietApi.Features.FoodLibrary;
using DietApi.Features.DietAchievements;
using DietApi.Features.DietAnalytics;
using DietApi.Features.DietGuidance;
using DietApi.Features.DietStats;
using DietApi.Features.Exercise;
using DietApi.Features.ExerciseShortcuts;
using DietApi.Features.FoodLog;
using DietApi.Features.Weight;
using DietApi.Infrastructure.Data;
using DietApi.Infrastructure.Data.Repositories;
using DietApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Accept and emit enums as their names (e.g. a food entry's "Breakfast" meal type) rather than ordinals
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=diet.db";
builder.Services.AddDbContext<DietDbContext>(options =>
    options.UseSqlite(connectionString));

// Same secret, issuer, audience and zero clock skew as the other services, so one sign-in
// covers this area and the areas that already exist.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService>(provider => provider.GetRequiredService<UserService>());

builder.Services.AddScoped<IDietPlanRepository, DietPlanRepository>();
builder.Services.AddScoped<ILoggedDayRepository, LoggedDayRepository>();
builder.Services.AddScoped<IFoodLibraryRepository, FoodLibraryRepository>();
builder.Services.AddScoped<IExerciseDayRepository, ExerciseDayRepository>();
builder.Services.AddScoped<IActivityTypeRepository, ActivityTypeRepository>();
builder.Services.AddScoped<IDietAnalyticsRepository, DietAnalyticsRepository>();
builder.Services.AddScoped<IDietAchievementRepository, DietAchievementRepository>();
builder.Services.AddScoped<IEatingTipRepository, EatingTipRepository>();

builder.Services.AddScoped<TargetSuggestionService>();
builder.Services.AddScoped<StreakCalculator>();
builder.Services.AddScoped<EnergyEstimator>();
builder.Services.AddScoped<ActivitySummaryCalculator>();
builder.Services.AddScoped<ShortcutListBuilder>();
builder.Services.AddScoped<AnalysisPeriodResolver>();
builder.Services.AddScoped<IntakeAnalyser>();
builder.Services.AddScoped<MacronutrientAnalyser>();
builder.Services.AddScoped<PatternAnalyser>();
builder.Services.AddScoped<TrendAnalyser>();

// Every observation rule the programme can produce. Adding one here is all it takes; the engine
// and the tests that assert properties across every rule pick it up automatically.
builder.Services.AddScoped<IObservationRule, LateEatingRule>();
builder.Services.AddScoped<IObservationRule, WeekendHeavierRule>();
builder.Services.AddScoped<IObservationRule, SingleFoodDominanceRule>();
builder.Services.AddScoped<IObservationRule, MealSkewRule>();
builder.Services.AddScoped<IObservationRule, LowPlantShareRule>();
builder.Services.AddScoped<IObservationRule, ProteinBelowTargetRule>();
builder.Services.AddScoped<IObservationRule, LoggingImprovedRule>();
builder.Services.AddScoped<ObservationEngine>();
builder.Services.AddScoped<DietAchievementStatusService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost", "http://localhost:80", "http://localhost:3004")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

app.MapDietPlanEndpoints();
app.MapFoodLibraryEndpoints();
app.MapFoodLogEndpoints();
app.MapExerciseEndpoints();
app.MapActivityCatalogueEndpoints();
app.MapExerciseShortcutsEndpoints();
app.MapDietAnalyticsEndpoints();
app.MapDietStatsEndpoints();
app.MapWeightEndpoints();
app.MapDietAchievementsEndpoints();
app.MapDietGuidanceEndpoints();

// The container's HEALTHCHECK probes this. Deliberately unauthenticated and outside /api.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DietDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");

        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

app.Run();
