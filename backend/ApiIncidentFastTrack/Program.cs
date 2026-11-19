using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIncidentCommentService, IncidentCommentService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IResolutionTypeService, ResolutionTypeService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// var connectionString = builder.Configuration.GetConnectionString("Incidents")
//     ?? throw new InvalidOperationException("Configure ConnectionStrings:Campanhas com dornet user-sercrets.");
//     builder.Services.AddDbContext<AppDbContext>(options =>options.UseSqlServer(connectionString));
//     builder.Services.AddScoped<CampanhaService>();

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
