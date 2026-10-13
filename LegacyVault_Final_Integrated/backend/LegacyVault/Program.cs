using System.Text;
using LegacyVault.Data;
using LegacyVault.Features.AssetManagement.Contracts;
using LegacyVault.Features.AssetManagement.Repositories;
using LegacyVault.Features.AssetManagement.Services;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.Repositories;
using LegacyVault.Features.IdentityAccess.Services;
using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.Repositories;
using LegacyVault.Features.WillManagement.Services;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Frontend (Vite) development CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured.");
}

builder.Services.AddDbContext<LegacyVaultDbContext>(options =>
    options.UseNpgsql(connectionString));

// =========================
// EXISTING IDENTITY / AUTH
// =========================
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// =========================
// SHARED CURRENT USER
// =========================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// =========================
// KIET - ASSET / ENCRYPTION
// =========================
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAssetEncryptionService, AssetEncryptionService>();
builder.Services.AddScoped<IAssetService, AssetService>();

// =========================
// CUONG - WILL FLOW
// =========================
builder.Services.AddScoped<IWillRepository, WillRepository>();
builder.Services.AddScoped<IWillService, WillService>();

// =========================
// JWT
// =========================
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
