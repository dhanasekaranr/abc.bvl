using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Services;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using abc.bvl.AdminTool.Infrastructure.Replication.Extensions;
using abc.bvl.AdminTool.Api.Configuration;
using abc.bvl.AdminTool.Api.Services;
using abc.bvl.AdminTool.Api.Middleware;
using abc.bvl.AdminTool.Api.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for secure logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Validate and bind configuration settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

var securitySettings = new SecuritySettings();
builder.Configuration.GetSection(SecuritySettings.SectionName).Bind(securitySettings);

// Validate configuration
var configurationErrors = new List<string>();
if (string.IsNullOrEmpty(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 64)
{
    configurationErrors.Add("JWT SecretKey must be at least 64 characters");
}
if (string.IsNullOrEmpty(jwtSettings.Issuer))
{
    configurationErrors.Add("JWT Issuer is required");
}
if (string.IsNullOrEmpty(jwtSettings.Audience))
{
    configurationErrors.Add("JWT Audience is required");
}

if (configurationErrors.Any())
{
    throw new InvalidOperationException($"Configuration errors: {string.Join(", ", configurationErrors)}");
}

// Register configuration settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection(SecuritySettings.SectionName));

// Add services to the container with security
builder.Services.AddControllers(options =>
{
    options.ModelValidatorProviders.Clear(); // Use FluentValidation instead
    
    // Add global filter to enrich all responses with UserInfo and AccessInfo
    options.Filters.Add<EnrichResponseFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "AdminTool API", 
        Version = "v1",
        Description = "Secure AdminTool API with comprehensive validation and authentication"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("JWT Token validated for user: {UserId}", 
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ScreenManager", policy => policy.RequireRole("Admin", "ScreenManager"));
});

// Add CORS with security
if (securitySettings.EnableCors)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(securitySettings.AllowedOrigins)
                  .WithHeaders("Authorization", "Content-Type", "X-DB-Route", "X-Enable-Dual-Write", "X-Database")
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    });
}

// Add HttpContextAccessor for database routing
builder.Services.AddHttpContextAccessor();

// Add configurable database
builder.Services.AddConfigurableDatabase(builder.Configuration);

// Get database settings for conditional services
var databaseSettings = builder.Configuration
    .GetSection(DatabaseSettings.SectionName)
    .Get<DatabaseSettings>() ?? new DatabaseSettings();

// Add MediatR with pipeline behaviors
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(abc.bvl.AdminTool.Application.PilotEnablement.Queries.GetPilotEnablementsQuery).Assembly);
    
    // Add authorization pipeline behavior (runs before handlers)
    cfg.AddOpenBehavior(typeof(abc.bvl.AdminTool.Application.Common.Behaviors.AuthorizationBehavior<,>));
});

// Add application services
builder.Services.AddScoped<IRequestContext, RequestContextAccessor>();

// Register UnitOfWork with DbContextResolver factory
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
{
    var resolver = serviceProvider.GetRequiredService<IDbContextResolver>();
    return new UnitOfWork(() => resolver.GetDbContext());
});

builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddScoped<IScreenDefinitionRepository, ScreenDefinitionRepository>();
builder.Services.AddScoped<IScreenPilotRepository, ScreenPilotRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

// Add Outbox pattern for dual-database replication
builder.Services.AddOutboxPattern(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline with security middleware
// Order is crucial for security!

// 1. Exception handling 
if (app.Environment.IsDevelopment())
{
    // Use built-in developer exception page in development
    app.UseDeveloperExceptionPage();
}
else
{
    // Use our custom exception handling in production
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseExceptionHandler("/Error");
}

// 2. Security headers (only in production to avoid conflicts with dev tools)
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<SecurityHeadersMiddleware>();
    
    // 3. Request size limiting
    app.UseMiddleware<RequestSizeLimitMiddleware>();

    // 4. Rate limiting
    app.UseMiddleware<RateLimitingMiddleware>();
}

// 5. HTTPS redirection
if (securitySettings.RequireHttps)
{
    app.UseHttpsRedirection();
}

// 6. HSTS (HTTP Strict Transport Security)
if (securitySettings.EnableHsts && !app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 7. CORS (if enabled)
if (securitySettings.EnableCors)
{
    app.UseCors();
}

// 8. Swagger (development only or secure production setup)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminTool API V1");
        c.OAuthClientId("swagger");
        c.OAuthAppName("AdminTool API");
    });
}

// 9. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 10. Controllers
app.MapControllers();

// 11. Health checks (optional)
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

// Log startup completion
Log.Information("AdminTool API started successfully in {Environment} mode", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
