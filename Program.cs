using Financial_management_backend.Data;
using Financial_management_backend.Models;
using Financial_management_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//var jwtConfig = builder.Configuration.GetSection("JwtConfig");
//var keySection = jwtConfig["Key"];
//if (string.IsNullOrEmpty(keySection))
//{
//    throw new InvalidOperationException("JWT Key is not configured in the application settings.");
//}
//var key = Encoding.UTF8.GetBytes(keySection);

//// Add services to the container.
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//}).AddJwtBearer(options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.SaveToken = true;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtConfig["Issuer"],
//        ValidAudience = jwtConfig["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//    };

//    options.Events = new JwtBearerEvents
//    {
//        OnMessageReceived = context =>
//        {
//            // Accept token without "Bearer " prefix
//            var token = context.Request.Headers["Authorization"].FirstOrDefault();
//            if (!string.IsNullOrEmpty(token))
//            {
//                context.Token = token; // Directly assign the token
//            }
//            return Task.CompletedTask;
//        },
//        OnAuthenticationFailed = context =>
//        {
//            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
//            if (context.Exception.GetType() == typeof(SecurityTokenInvalidSignatureException))
//            {
//                Console.WriteLine("The token signature is invalid.");
//            }
//            else if (context.Exception.GetType() == typeof(SecurityTokenInvalidIssuerException))
//            {
//                Console.WriteLine("The token issuer is invalid.");
//            }
//            else if (context.Exception.GetType() == typeof(SecurityTokenInvalidAudienceException))
//            {
//                Console.WriteLine("The token audience is invalid.");
//            }
//            return Task.CompletedTask;
//        },
//        OnTokenValidated = context =>
//        {
//            Console.WriteLine("Token validated successfully.");
//            return Task.CompletedTask;
//        }
//    };
//});
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
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"]!)),
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
