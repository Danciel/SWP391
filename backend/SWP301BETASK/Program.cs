using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using SWP301BETASK.Data;
using SWP301BETASK.Models;
using SWP301BETASK.Features.Admin.Services;
using SWP301BETASK.Features.Admin.Controllers;
using SWP301BETASK.Features.Admin.Configuration;

// Asset Encryption
using SWP301BETASK.Features.AssetManagement.Contracts;
using SWP301BETASK.Features.AssetManagement.Services;
using SWP301BETASK.Features.AssetManagement.Verification;
using SWP301BETASK.Features.AssetManagement.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Options
// ============================================================

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<OtpOptions>(
    builder.Configuration.GetSection("Otp"));

var jwt = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? new JwtOptions();

if (jwt.Secret.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Secret must be at least 32 characters.");

if (!builder.Environment.IsDevelopment() &&
    jwt.Secret.StartsWith("CHANGE_ME"))
{
    throw new InvalidOperationException(
        "Set a real Jwt:Secret (env var Jwt__Secret or user-secrets).");
}


// ============================================================
// Database
// ============================================================
//
// Supabase uses PostgreSQL.
// Set:
//
// Database:Provider = Postgres
//
// and:
//
// ConnectionStrings:Postgres = <Supabase connection string>
//

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<LegacyVaultDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================
// Application Services
// ============================================================

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IPasswordService, BCryptPasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IOtpSender, ConsoleOtpSender>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddSingleton<IIdentityProviderClient, MockIdentityProviderClient>();

// ============================================================
// Asset Management Services (from DEV-Kiet)
// ============================================================

builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAssetVerificationService, MockAssetVerificationService>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAssetService, AssetService>();

// ============================================================
// Authentication - JWT
// ============================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Secret)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();


// ============================================================
// Rate Limiting
// ============================================================

var permitPerMinute =
    builder.Configuration.GetValue<int?>(
        "RateLimit:PermitPerMinute") ?? 30;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
});


// ============================================================
// Controllers
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));


// ============================================================
// Swagger / OpenAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "SWP301BETASK API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description =
                "Paste the access token from /api/auth/login"
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)]
                = new List<string>()
        });
});


// ============================================================
// CORS
// ============================================================

var frontendUrl =
    builder.Configuration["FrontendUrl"]
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ============================================================
// Build
// ============================================================

var app = builder.Build();


// ============================================================
// Database bootstrap + development admin seed
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<LegacyVaultDbContext>();

    db.Database.EnsureCreated();

    var seed =
        app.Configuration.GetSection("SeedAdmin");

    var seedEmail = seed["Email"];
    var seedPassword = seed["Password"];

    if (!string.IsNullOrWhiteSpace(seedEmail) &&
        !string.IsNullOrWhiteSpace(seedPassword) &&
        !db.Accounts.Any(a => a.Role == UserRole.ADMIN))
    {
        var passwords = scope.ServiceProvider
            .GetRequiredService<IPasswordService>();

        db.Accounts.Add(
            new Account
            {
                Username =
                    Normalize.Username(
                        seed["Username"] ?? "admin"),

                Email =
                    Normalize.Email(seedEmail),

                Role = UserRole.ADMIN,

                PasswordHash =
                    passwords.Hash(seedPassword),

                SecretOtp =
                    Convert.ToBase64String(
                        System.Security.Cryptography
                            .RandomNumberGenerator
                            .GetBytes(32)),

                EmailVerified = true
            });

        db.SaveChanges();
    }
}


// ============================================================
// Development CLI seeding
// ============================================================

var seedArg = Array.IndexOf(args, "--seed-users");

if (seedArg >= 0)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine(
            "--seed-users is only allowed when " +
            "ASPNETCORE_ENVIRONMENT=Development.");

        return;
    }

    var seedCount =
        seedArg + 1 < args.Length &&
        int.TryParse(args[seedArg + 1], out var parsed)
            ? Math.Clamp(parsed, 1, 500)
            : 20;

    using var seedScope =
        app.Services.CreateScope();

    var added =
        await DevDataSeeder.SeedAsync(
            seedScope.ServiceProvider
                .GetRequiredService<LegacyVaultDbContext>(),

            seedScope.ServiceProvider
                .GetRequiredService<IPasswordService>(),

            seedCount);

    Console.WriteLine(
        $"Done. {added} new user(s) inserted into the database.");

    return;
}


// ============================================================
// HTTP Pipeline
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();