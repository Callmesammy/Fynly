using AiCFO.API.Middleware;
using AiCFO.Application.Services;
using AiCFO.Application.Common;
using AiCFO.Application.Features.Auth.Commands;
using AiCFO.Infastructure.Services;
using AiCFO.Infrastructure.Persistence;
using AiCFO.Infrastructure.Services;
using AiCFO.Infrastructure.BankIntegration;
using AiCFO.Application.Features.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;
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

// Multi-Tenancy
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddHttpContextAccessor();

// Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=aicfo;Trusted_Connection=true;Encrypt=false;";
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var tenantContext = sp.GetService<ITenantContext>();
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
    });
});

// CQRS with MediatR
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly);
});

// Authentication & Authorization
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Ledger Services
builder.Services.AddScoped<ILedgerService, LedgerService>();

// Bank Services
builder.Services.AddScoped<IBankService, BankService>();

// Bank Provider Integration (OAuth2 & API calls)
builder.Services.AddHttpClient<FlutterwaveProvider>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.flutterwave.com");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddScoped<IBankProviderFactory, BankProviderFactory>();

// Reconciliation Services
builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddSwaggerGen();
// Accounting Validation & Rules
builder.Services.AddScoped<IAccountingValidationService, AccountingValidationService>();

// AI Services (Anomaly Detection, Prediction, Health Scoring, Recommendations)
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IHealthScoreService, HealthScoreService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Alert Services (Anomaly-based alerting)
builder.Services.AddScoped<IAlertService, AlertService>();

// Health Report Services
builder.Services.AddScoped<IHealthReportService, HealthReportService>();

// Predictive Alert Services
builder.Services.AddScoped<IPredictiveAlertService, PredictiveAlertService>();

// Background Job Processing with Hangfire
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection") ?? connectionString;
builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConnectionString);
});
builder.Services.AddHangfireServer();
builder.Services.AddScoped<IBackgroundJobService, RecurringJobScheduler>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT secret is not configured. Set 'Jwt:Secret' in configuration, user secrets, or environment variables.");
}

var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ai-cfo",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ai-cfo-api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001"
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
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
});

// Hangfire Dashboard (requires authorization in production)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "AI CFO - Background Jobs",
    DisplayStorageConnectionString = false,
    IsReadOnlyFunc = _ => false,
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
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
