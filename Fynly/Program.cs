using AiCFO.API.Middleware;
using Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add logging with Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// API Documentation with Scalar
builder.Services.AddOpenApi();

// CQRS with MediatR
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3000",
                "https://localhost:3001"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline

// Custom middleware
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

try
{
    Log.Information("Starting AI CFO API");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

app.Run();
