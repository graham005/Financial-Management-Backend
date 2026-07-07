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

// Build connection string - prefer environment variables (production), fallback to appsettings (development)
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
var connectionString = string.IsNullOrEmpty(dbServer)
    ? builder.Configuration.GetConnectionString("DefaultConnection") // Development: use appsettings.json
    : $"Server={dbServer};Database={Environment.GetEnvironmentVariable("DB_NAME")};User Id={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};Encrypt={Environment.GetEnvironmentVariable("DB_ENCRYPT") ?? "True"};TrustServerCertificate={Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "False"};Connection Timeout={Environment.GetEnvironmentVariable("DB_CONNECTION_TIMEOUT") ?? "60"};MultipleActiveResultSets={Environment.GetEnvironmentVariable("DB_MULTIPLE_ACTIVE_RESULT_SETS") ?? "False"};ConnectRetryCount=3;ConnectRetryInterval=10"; // Production: use environment variables

// Get JWT configuration from environment variables with fallback to configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["JwtConfig:Key"];
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["JwtConfig:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["JwtConfig:Audience"];

if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT_SECRET_KEY is not configured.");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is not configured.");

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlServerOptions.CommandTimeout(120);
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// Add Health Checks
builder.Services.AddHealthChecks();

// Registering repository and background services 
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

// Retry logic for database initialization (only in development or when DB is available)
var maxRetries = builder.Environment.IsDevelopment() ? 3 : 5;
var delay = TimeSpan.FromSeconds(5);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Test connection
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

            GradeSeeder.UpdateGradeLevels(dbContext);
        }
        
        // Success - break the retry loop
        break;
    }
    catch (Exception ex)
    {
        if (i == maxRetries - 1)
        {
            // Last retry failed - log and continue (don't crash the app)
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Management API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}
else if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Management API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root for production
    });
}

// Add root endpoint for development
app.MapGet("/", () => Results.Redirect("/swagger"));

// Add API status endpoint
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

// Map health checks
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors("AllowAllOrigins");

app.MapControllers();

app.Run();
