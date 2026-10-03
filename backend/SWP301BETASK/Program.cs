using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;

// Asset Encryption
using SWP301BETASK.Features.AssetManagement.Contracts;
using SWP301BETASK.Features.AssetManagement.Services;
using SWP301BETASK.Features.AssetManagement.Verification;
using SWP301BETASK.Features.AssetManagement.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LegacyVaultDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection was not found.")
    )
);


// =====================================================
// ASSET MANAGEMENT SERVICES
// =====================================================

builder.Services.AddScoped<IEncryptionService, EncryptionService>();

builder.Services.AddScoped<
    IAssetVerificationService,
    MockAssetVerificationService>();

builder.Services.AddScoped<
    IAssetRepository,
    AssetRepository>();

builder.Services.AddScoped<
    IAssetService,
    AssetService>();


// =====================================================
// CORS
// =====================================================

// Allow the Vite frontend to call this API during local development.
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


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();