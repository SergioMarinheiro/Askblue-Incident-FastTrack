using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Locally the connection string comes from user secrets (never from appsettings.json).
var connectionString = builder.Configuration.GetConnectionString("IncidentFastTrack")
    ?? throw new InvalidOperationException(
        "Connection string 'IncidentFastTrack' is not configured. Set it with 'dotnet user-secrets set'.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIncidentCommentService, IncidentCommentService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IResolutionTypeService, ResolutionTypeService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
