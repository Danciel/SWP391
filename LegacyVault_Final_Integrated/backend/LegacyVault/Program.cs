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
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Asset OAuth callback tickets and provider HTTP requests.
builder.Services.AddMemoryCache();


builder.Services.AddDataProtection();

builder.Services.AddSingleton<
    LegacyVault.Shared.Security.OwnerPinGrantService>();



builder.Services.AddHttpClient("asset-oauth");
builder.Services.AddOpenApi();

// Frontend (Vite) development CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5161",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174")
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

// Account & Auth Services
builder.Services.AddScoped<IAccountService, AccountsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Audit Management (admin audit-log API)
builder.Services.AddScoped<LegacyVault.Features.AuditManagement.Contracts.IAuditRepository, LegacyVault.Features.AuditManagement.Repositories.AuditRepository>();
builder.Services.AddScoped<LegacyVault.Features.AuditManagement.Contracts.IAuditService, LegacyVault.Features.AuditManagement.Services.AuditService>();

// Phone OTP & SMS Sender
builder.Services.AddScoped<IPhoneOtpService, PhoneOtpService>();
builder.Services.AddScoped<IPhoneOtpRepository, PhoneOtpRepository>();
builder.Services.AddScoped<ISmsSender, MockSmsSender>();

// IDP Integration 
builder.Services.AddHttpClient<IIdpIntegrationService, IdpIntegrationService>();


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
builder.Services.AddScoped<
    LegacyVault.Features.WillManagement.Contracts.IWalletService,
    LegacyVault.Features.WillManagement.Services.WalletService>();
builder.Services.AddScoped<
    LegacyVault.Features.WillManagement.Contracts.IExecutorProfileService,
    LegacyVault.Features.WillManagement.Services.ExecutorProfileService>();
builder.Services.AddScoped<
    LegacyVault.Features.WillManagement.Contracts.IExecutorPricingService,
    LegacyVault.Features.WillManagement.Services.ExecutorPricingService>();
builder.Services.AddScoped<
    LegacyVault.Features.WillManagement.Contracts.ILegalService,
    LegacyVault.Features.WillManagement.Services.LegalService>();
builder.Services.AddScoped<
    LegacyVault.Features.WillManagement.Contracts.IHandoverService,
    LegacyVault.Features.WillManagement.Services.HandoverService>();
builder.Services.AddScoped<IWillRepository, WillRepository>();
builder.Services.AddScoped<IWillService, WillService>();
// D05: LifeStatus comes from the separate MockIdpApi over HTTP
// (search by email -> query by id). Override with config "Idp:BaseUrl".
builder.Services.AddHttpClient<
    LegacyVault.Features.WillManagement.Contracts.ILifeStatusClient,
    LegacyVault.Features.WillManagement.Services.IdpLifeStatusClient>(client =>
{
    var baseUrl = builder.Configuration["Idp:BaseUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHostedService<
    LegacyVault.Features.WillManagement.Services.DmsMissedCycleWorker>();

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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LegacyVault API",
        Version = "v1",
        Description = "Identity & Access endpoints: authentication and accounts."
    });

    // JWT Bearer authentication button in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token only (no 'Bearer ' prefix)."
    });

    // Apply the Bearer requirement to endpoints marked [Authorize]
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LegacyVault API v1");
            options.RoutePrefix = "swagger"; // UI at /swagger
            options.DisplayRequestDuration();
        });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class AuthorizeCheckOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var hasAuthorize =
            context.MethodInfo.DeclaringType!
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any()
            || context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any();

        var allowAnonymous =
            context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || allowAnonymous)
        {
            return;
        }

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
        });
    }
}