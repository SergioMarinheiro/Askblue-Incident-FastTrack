using System.Text;
using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using ApiIncidentFastTrack.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Locally the connection string comes from user secrets (never from appsettings.json).
var connectionString = builder.Configuration.GetConnectionString("IncidentFastTrack")
    ?? throw new InvalidOperationException(
        "Connection string 'IncidentFastTrack' is not configured. Set it with 'dotnet user-secrets set'.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// JWT settings: Issuer/Audience/ExpiryMinutes from appsettings.json, SigningKey from user secrets.
// Fail at startup, not at the first login, if the key is missing or too short.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < JwtOptions.MinimumSigningKeyBytes)
{
    throw new InvalidOperationException(
        $"'Jwt:SigningKey' is missing or shorter than {JwtOptions.MinimumSigningKeyBytes} bytes. Set it with 'dotnet user-secrets set'.");
}

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as issued ("sub", "role", "company_id"),
        // instead of remapping them to long Microsoft claim-type URIs.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            // The default is 5 minutes; one server issues and checks the token, so little skew is needed.
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.UserId,
            // Makes [Authorize(Roles = "Manager")] and User.IsInRole(...) read the "role" claim.
            RoleClaimType = AppClaimTypes.Role
        };
    });

builder.Services.AddAuthorizationBuilder()
    // Every endpoint requires a valid token unless it is marked [AllowAnonymous].
    // Forgetting an attribute leaves an endpoint locked, not open.
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(AuthPolicies.ManagerOrAbove, policy =>
        policy.RequireRole(nameof(UserRole.Manager), nameof(UserRole.Admin), nameof(UserRole.SuperAdmin)))
    .AddPolicy(AuthPolicies.AdminOrAbove, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.SuperAdmin)))
    .AddPolicy(AuthPolicies.SuperAdminOnly, policy =>
        policy.RequireRole(nameof(UserRole.SuperAdmin)));

// The caller of the current request, read from the JWT claims (used by services and the tenant filter).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ICompanyResolver, CompanyResolver>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIncidentCommentService, IncidentCommentService>();
builder.Services.AddScoped<IIncidentAttachmentService, IncidentAttachmentService>();

// Attachment files: a folder on disk for now (decided 2026-11-23). A relative RootPath is
// resolved against the content root, so it doesn't depend on the folder the API is started from.
var attachmentOptions = builder.Configuration.GetSection(AttachmentStorageOptions.SectionName).Get<AttachmentStorageOptions>()
    ?? new AttachmentStorageOptions();
var attachmentRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, attachmentOptions.RootPath));
builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(attachmentRoot));
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IResolutionTypeService, ResolutionTypeService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

// Errors as ProblemDetails (RFC 9457): exceptions thrown by services (GlobalExceptionHandler)
// and empty-body status codes such as 401/403 from authorization (UseStatusCodePages below).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize" button: paste the token from /api/auth/login (without "Bearer ").
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT from POST /api/auth/login"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// First in the pipeline, so it catches exceptions from everything after it.
app.UseExceptionHandler();

// Gives a ProblemDetails body to responses that have a status code but no body
// (401 without a token, 403 for a wrong role, 404 for an unknown route).
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Creates the first SuperAdmin on an empty database (credentials from user secrets).
    await DevelopmentSeeder.SeedSuperAdminAsync(app.Services);
}

app.UseHttpsRedirection();

// Order matters: first find out who the caller is, then check what they may do.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
