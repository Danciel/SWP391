using Microsoft.EntityFrameworkCore;
using MockIdpApi.Data;
using MockIdpApi.Features.IdentityAccess.Contracts;
using MockIdpApi.Features.IdentityAccess.Services;
using MockIdpApi.Features.IdentityAccess.Repositories;
using MockIdpApi.Features.FaceRecognition.Contracts;
using MockIdpApi.Features.FaceRecognition.Services;
using MockIdpApi.Features.FaceRecognition.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. CORS (Allow LegacyVault and Frontend to call this)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// 2. Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MockIdpDbContext>(options => options.UseNpgsql(connectionString));

// 3. Services
builder.Services.AddScoped<IIdpUserRepository, IdpUserRepository>();
builder.Services.AddScoped<IIdpUserService, IdpUsersService>();
builder.Services.AddScoped<IFaceRepository, FaceRepository>();
builder.Services.AddScoped<IFaceService, FaceService>();
builder.Services.AddSingleton<IFaceRecognitionService, FaceRecognitionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
// app.UseAuthentication(); // Add JWT config here if you copy it from LegacyVault
// app.UseAuthorization();

app.MapControllers();
app.Run();