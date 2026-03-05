using AiCFO.API.Middleware;
using AiCFO.Application.Services;
using AiCFO.Infrastructure.Persistence;
using AiCFO.Infrastructure.Services;
using AiCFO.Infrastructure.BankIntegration;
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

// Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=aicfo;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var tenantContext = sp.GetService<ITenantContext>();
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
    });
});

// CQRS with MediatR
builder.Services.AddMediatR(config => 
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
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

// Accounting Validation & Rules
builder.Services.AddScoped<IAccountingValidationService, AccountingValidationService>();

// AI Services (Anomaly Detection, Prediction, Health Scoring, Recommendations)
builder.Services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IHealthScoreService, HealthScoreService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key-min-32-characters-long!!";
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
app.UseMiddleware<TenantMiddleware>();
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

app.Run();
