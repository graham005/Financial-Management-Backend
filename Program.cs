using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Financial_management_backend.Services.BackgroundServices;
using Financial_management_backend.Services.Dtos;
using Financial_management_backend.Services.ItemManagement;
using Financial_management_backend.Services.Reports;
using Financial_management_backend.Services.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CONNECTION STRING CONFIGURATION 
// =========================================================================
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
string connectionString;

if (string.IsNullOrEmpty(dbServer))
{
    // Development: Fallback to local appsettings.json connection string
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
}
else
{
    // Production: Dynamically build the standard PostgreSQL string using individual Render Env vars
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    connectionString = $"Host={dbServer};Database={dbName};Username={dbUser};Password={dbPassword};Port=5432;SslMode=Require;TrustServerCertificate=True;";
}

// Get JWT configuration from environment variables with fallback to configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["JwtConfig:Key"];
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["JwtConfig:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["JwtConfig:Audience"];

if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT_SECRET_KEY is not configured.");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is not configured.");

// =========================================================================
// 2. IDENTITY AND SECURITY SERVICES
// =========================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();

// =========================================================================
// 3. DATABASE CONTEXT REGISTRATION 
// =========================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        npgsqlOptions.CommandTimeout(120);
    });
});

// =========================================================================
// 4. API CONTROLLERS & DOCUMENTATION
// =========================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Enter your JWT Access Token",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() },
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddHealthChecks();

// =========================================================================
// 5. APPLICATION SERVICES & APPLICATION REPOSITORIES
// =========================================================================
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<FeeService>();
builder.Services.AddScoped<IFinancialTransactionService, FinancialTransactionService>();
builder.Services.AddScoped<IFeeValidationService, FeeValidationService>();
builder.Services.AddScoped<IAcademicTermService, AcademicTermService>();
builder.Services.AddScoped<IEnhancedFeeService, EnhancedFeeService>();
builder.Services.AddScoped<FeeObligationService>();
builder.Services.AddScoped<IItemTransactionService, ItemTransactionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStudentGradeHistoryService, StudentGradeHistoryService>();
builder.Services.AddScoped<IHistoricalFeeStructureService, HistoricalFeeStructureService>();
builder.Services.AddScoped<IStudentPromotionService, StudentPromotionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();
builder.Services.AddScoped<IExcelReportGenerator, ExcelReportGenerator>();

var app = builder.Build();

// =========================================================================
// 6. ASYNC DATABASE SEEDER / INITIALIZATION
// =========================================================================
var maxRetries = builder.Environment.IsDevelopment() ? 3 : 5;
var delay = TimeSpan.FromSeconds(5);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Test Connection to PostgreSQL
        await dbContext.Database.CanConnectAsync();

        if (!dbContext.Users.Any())
        {
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin"
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }

        await GradeSeeder.UpdateGradeLevels(dbContext);
        break;
    }
    catch (Exception ex)
    {
        if (i == maxRetries - 1)
        {
            Console.WriteLine($"Failed to initialize database after {maxRetries} attempts: {ex.Message}");
            Console.WriteLine("Application will continue but database may not be initialized.");
        }
        else
        {
            Console.WriteLine($"Database connection attempt {i + 1} failed. Retrying in {delay.TotalSeconds} seconds...");
            await Task.Delay(delay);
        }
    }
}

// =========================================================================
// 7. REQUEST PIPELINE MIDDLEWARES & ENDPOINTS
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Management API v1");
        c.RoutePrefix = "swagger";
    });
}
else if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Management API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/status", () => Results.Ok(new
{
    Service = "Financial Management API",
    Status = "Running",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Endpoints = new
    {
        Swagger = "/swagger",
        Health = "/health",
        API = "/api"
    }
}));

app.MapHealthChecks("/health");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAllOrigins");
app.MapControllers();

app.Run();