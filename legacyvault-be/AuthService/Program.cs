using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AuthService.Configuration;
using AuthService.Data;
using AuthService.Domain;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Options ---------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection("Otp"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Secret.Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
if (!builder.Environment.IsDevelopment() && jwt.Secret.StartsWith("CHANGE_ME"))
    throw new InvalidOperationException("Set a real Jwt:Secret (env var Jwt__Secret or user-secrets).");

// ---- Services --------------------------------------------------------------
// Database:Provider = Sqlite | Postgres | SqlServer  (connection string key = ConnectionStrings:<Provider>)
var dbProvider = (builder.Configuration["Database:Provider"] ?? "Sqlite").Trim();
var connectionString = builder.Configuration.GetConnectionString(dbProvider)
    ?? throw new InvalidOperationException($"Missing ConnectionStrings:{dbProvider}.");

builder.Services.AddDbContext<AppDbContext>(o =>
{
    switch (dbProvider.ToLowerInvariant())
    {
        case "postgres": o.UseNpgsql(connectionString); break;
        case "sqlserver": o.UseSqlServer(connectionString); break;
        case "sqlite": o.UseSqlite(connectionString); break;
        default:
            throw new InvalidOperationException(
                $"Unknown Database:Provider '{dbProvider}'. Use Sqlite, Postgres or SqlServer.");
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IPasswordService, BCryptPasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IOtpSender, ConsoleOtpSender>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddSingleton<IIdentityProviderClient, MockIdentityProviderClient>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var permitPerMinute = builder.Configuration.GetValue<int?>("RateLimit:PermitPerMinute") ?? 30;
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Paste the access token from /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---- DB bootstrap + optional dev admin seed --------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // swap for `dotnet ef migrations` in real deployments

    var seed = app.Configuration.GetSection("SeedAdmin");
    var seedEmail = seed["Email"];
    var seedPassword = seed["Password"];
    if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword)
        && !db.Accounts.Any(a => a.Role == UserRole.ADMIN))
    {
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        db.Accounts.Add(new Account
        {
            Username = Normalize.Username(seed["Username"] ?? "admin"),
            Email = Normalize.Email(seedEmail),
            Role = UserRole.ADMIN,
            PasswordHash = passwords.Hash(seedPassword),
            SecretOtp = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            EmailVerified = true
        });
        db.SaveChanges();
    }
}

// ---- CLI: dotnet run -- --seed-users [count] (Development only) ---------------
var seedArg = Array.IndexOf(args, "--seed-users");
if (seedArg >= 0)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("--seed-users is only allowed when ASPNETCORE_ENVIRONMENT=Development.");
        return;
    }

    var seedCount = seedArg + 1 < args.Length && int.TryParse(args[seedArg + 1], out var parsed)
        ? Math.Clamp(parsed, 1, 500)
        : 20;

    using var seedScope = app.Services.CreateScope();
    var added = await DevDataSeeder.SeedAsync(
        seedScope.ServiceProvider.GetRequiredService<AppDbContext>(),
        seedScope.ServiceProvider.GetRequiredService<IPasswordService>(),
        seedCount);

    Console.WriteLine($"Done. {added} new user(s) inserted into '{dbProvider}' database.");
    return;
}

// ---- Pipeline ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
