using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using AssignmentHub.Api.Configuration;
using AssignmentHub.Api.Contracts;
using AssignmentHub.Api.Middleware;
using AssignmentHub.Application;
using AssignmentHub.Infrastructure;
using AssignmentHub.Infrastructure.Persistence.Seed;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AssignmentHubFrontend";

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------
// Providers are declared explicitly rather than inherited so the set is obvious
// at a glance. Levels come from the "Logging" section of appsettings.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
// WebApplicationBuilder already layers appsettings.json, appsettings.{Env}.json,
// user-secrets (Development) and environment variables, in that order. Env vars
// use "__" as the separator, so ConnectionStrings__Default and Jwt__Secret
// override the files without any extra wiring.
var jwtOptions = builder.Configuration
                     .GetSection(JwtOptions.SectionName)
                     .Get<JwtOptions>()
                 ?? new JwtOptions();

// Fail fast and loudly: a missing or short signing key must never silently
// degrade into unverifiable tokens.
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. It is deliberately absent from every committed file. " +
        "Set it with `dotnet user-secrets set \"Jwt:Secret\" \"<32+ character key>\"` " +
        "or export Jwt__Secret before running the API (see .env.example and the README). " +
        "The same applies to `dotnet ef` commands, which boot this host.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret must be at least 32 bytes (256 bits) for HMAC-SHA256 signing.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// ---------------------------------------------------------------------------
// Application + Infrastructure composition
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------------------------
// MVC, validation and the consistent error contract
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

// Model-binding and validation failures are reshaped into ApiErrorResponse so a
// 400 looks exactly like a 500 to the client, minus the fields it does not need.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        var payload = new ApiErrorResponse
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            TraceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
            Errors = errors
        };

        return new BadRequestObjectResult(payload);
    };
});

// ---------------------------------------------------------------------------
// Swagger / OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assignment Hub API",
        Version = "v1",
        Description = "Assignment & submission management for Admin, Teacher and Student roles."
    });

    // Adds the "Authorize" button and sends `Authorization: Bearer <token>`.
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste the JWT returned by the login endpoint. The \"Bearer \" prefix is added for you.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });

    var xmlDocumentation = Path.Combine(AppContext.BaseDirectory, "AssignmentHub.Api.xml");
    if (File.Exists(xmlDocumentation))
    {
        options.IncludeXmlComments(xmlDocumentation);
    }
});

// ---------------------------------------------------------------------------
// Authentication and authorization
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            // Default is 5 minutes, which makes short-lived-token tests unreliable.
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
// Origins are configuration-driven; the default matches `npm run dev`.
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

// An empty array in configuration would otherwise block every origin silently,
// which is a painful thing to debug from the browser side.
string[] allowedOrigins = configuredOrigins is { Length: > 0 }
    ? configuredOrigins
    : new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
// First in, so it wraps everything that follows.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    // Demo data for local development only. Idempotent: a restart against an
    // already-seeded database is a no-op. Migrations are applied separately via
    // `dotnet ef database update` (see docs/database.md), so the seeder skips and
    // logs a warning if any are still pending.
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assignment Hub API v1");
        options.DocumentTitle = "Assignment Hub API";
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    // Skipped in Development: redirecting the frontend's http:// calls to https://
    // turns every CORS preflight into a failure.
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposed so the test project can host the real pipeline through
/// <c>WebApplicationFactory&lt;Program&gt;</c>. Top-level statements would
/// otherwise generate an internal Program class.
/// </summary>
public partial class Program
{
}
