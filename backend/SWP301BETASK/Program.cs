using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Features.WillManagement;
using SWP301BETASK.Shared.Exceptions;
using SWP301BETASK.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddStandardApiErrors();   // [WillManagement] shared { message, errors } validation shape
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LegacyVaultDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection was not found.")
    )
);

builder.Services.AddWillManagement();                        // [WillManagement]

// Allow the Vite frontend to call this API during local development.
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
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

app.UseMiddleware<ExceptionMiddleware>();                    // [WillManagement] shared error handling

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
