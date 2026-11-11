using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Middleware;
using SchoolAdmissionSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (Configurable via Environment Variable)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=SchoolAdmissionDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Dependency Injection Services (SOLID Principles)
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();
builder.Services.AddHostedService<HoldCleanupBackgroundService>();

// 3. Centralized Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. Controllers & JSON Serialization (camelCase)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 5. CORS for React Frontend (allows X-Student-Id header)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Student-Id", "Location");
    });
});

// 6. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "School Admission System API",
        Version = "v1",
        Description = "API for student admission reservations with optimistic concurrency and dynamic seat release."
    });
});

var app = builder.Build();

// 7. Automatic Database Schema Initialization on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database schema check/creation deferred or failed on startup.");
    }
}

// 8. Middleware Pipeline
app.UseExceptionHandler();
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "School Admission System API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();

// Accessible to WebApplicationFactory for Integration Tests
public partial class Program { }
