using Auth.Enums;
using Auth.Interfaces;
using Auth.Interfaces.Auth;
using Auth.Interfaces.RateLimiting;
using Auth.Models;
using Auth.RateLimiting;
using Auth.Repository;
using Auth.Services;
using Auth.Services.Auth;
using Auth.Services.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
                
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure enums to serialize as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWT Authentication API",
        Version = "v1",
        Description = "JWT Authentication API with refresh tokens and role-based authorization",
    });

    // Configure Swagger to use string enums in schema
    options.UseInlineDefinitionsForEnums();

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Auth")
    ));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Rate Limiting Services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IRateLimitRepo, RateLimitRepo>();

// Rate Limiting Middleware Dependencies
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitStore, MemoryRateLimitStore>();
builder.Services.AddSingleton(new RateLimitOptions
{
    Enabled = true,
    DefaultRequestLimit = 100,
    DefaultTimeWindow = TimeSpan.FromMinutes(1),
    DefaultAlgorithm = RateLimitAlgorithm.FixedWindow,
    DefaultStrategy = RateLimitStrategy.PerIp
});
builder.Services.AddSingleton<IRateLimitAlgorithmFactory, RateLimitAlgorithmFactory>();

builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<RateLimitLogCleanupService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AuthAPIClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JWT Authentication API v1");
        options.RoutePrefix = string.Empty; // Opens at root URL
        options.DocumentTitle = "JWT Authentication API - Swagger";
        options.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();

app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();