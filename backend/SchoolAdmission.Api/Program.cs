
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register PostgreSQL database context.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// OpenAPI configuration.
builder.Services.AddOpenApi();

builder.Services.Configure<HoldSettings>(options =>
{
    options.DurationSeconds = int.TryParse(
        builder.Configuration["HOLD_DURATION_SECONDS"],
        out var duration
    ) && duration > 0
        ? duration
        : 600;
});

builder.Services.AddScoped<AdmissionHoldService>();

builder.Services.AddHostedService<ExpiredHoldCleanupService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Automatically apply pending database migrations at startup.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseAuthorization();
app.MapControllers();
app.Run();