using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Auth;
using Api.Diagnostics;
using Api.Swagger;
using Application;
using Asp.Versioning;
using Domain.Enums;
using Infrastructure;
using Infrastructure.ExternalServices;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Title = "Validation failed",
                            Status = StatusCodes.Status422UnprocessableEntity
                        };
                        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddMvc();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme. Obtain a token from the auth endpoints."
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });

            builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            RegisterHealthChecks(builder.Services, builder.Configuration);

            var jwtSettings = builder.Configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    if (jwtSettings is null)
                    {
                        return;
                    }

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.OwnerOnly,
                    policy => policy.RequireClaim(ClaimTypes.Role, StoreUserRole.Owner.ToString()));
            });

            var app = builder.Build();

            app.UseExceptionHandler();

            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestTraceId", httpContext.TraceIdentifier);

                    var storeId = httpContext.User.FindFirstValue("store_id");

                    if (storeId is not null)
                    {
                        diagnosticContext.Set("StoreId", storeId);
                    }

                    var userId = httpContext.User.FindFirstValue("sub");

                    if (userId is not null)
                    {
                        diagnosticContext.Set("UserId", userId);
                    }
                };
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/", () => Results.Redirect("/swagger"));

                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    foreach (var description in app.DescribeApiVersions())
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                });
            }

            SeedRoles(app.Services);

            if (app.Environment.IsDevelopment())
            {
                SeedCatalog(app.Services);
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestLogContextMiddleware>();

            app.MapControllers();
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthReportAsync,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });

            app.Run();
        }
        catch (Exception ex)
        {
            if (ex is not HostAbortedException)
            {
                Log.Fatal(ex, "StockMesh terminated unexpectedly.");
            }

            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void SeedRoles(IServiceProvider services)
    {
        try
        {
            RoleSeeder.EnsureRolesAsync(services).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to seed identity roles.");
        }
    }

    private static void SeedCatalog(IServiceProvider services)
    {
        try
        {
            CatalogSeeder.EnsureCatalogAsync(services).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to seed catalog data.");
        }
    }

    private static async Task WriteHealthReportAsync(
        HttpContext httpContext,
        HealthReport report)
    {
        httpContext.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    data = entry.Value.Data
                })
        };

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            payload,
            (JsonSerializerOptions?)null,
            httpContext.RequestAborted);
    }

    private static void RegisterHealthChecks(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var forecastingBaseUrl = configuration.GetSection(ForecastingSettings.SectionName)
            .GetValue<string>("BaseUrl");
        var assistantBaseUrl = configuration.GetSection(AssistantSettings.SectionName)
            .GetValue<string>("BaseUrl");
        var stripeKey = configuration.GetValue<string>($"{StripeSettings.SectionName}:SecretKey");
        var redisConfiguration = configuration.GetValue<string>("Redis:Configuration")
            ?? "localhost:6379,abortConnect=false";

        services.AddHealthChecks()
            .AddCheck<DbAvailabilityHealthCheck>(
                "database",
                failureStatus: HealthStatus.Degraded)
            .AddCheck(
                "redis",
                new RedisHealthCheck(redisConfiguration, TimeSpan.FromSeconds(2)),
                failureStatus: HealthStatus.Degraded)
            .AddCheck(
                "forecasting-service",
                new ExternalApiHealthCheck(
                    new HttpClient
                    {
                        BaseAddress = new Uri(
                            forecastingBaseUrl ?? throw new InvalidOperationException(
                                "Missing 'Forecasting:BaseUrl' configuration section.")),
                        Timeout = TimeSpan.FromSeconds(3)
                    },
                    "health"),
                failureStatus: HealthStatus.Degraded)
            .AddCheck(
                "agent-service",
                new ExternalApiHealthCheck(
                    new HttpClient
                    {
                        BaseAddress = new Uri(
                            assistantBaseUrl ?? throw new InvalidOperationException(
                                "Missing 'Assistant:BaseUrl' configuration section.")),
                        Timeout = TimeSpan.FromSeconds(3)
                    },
                    "health"),
                failureStatus: HealthStatus.Degraded)
            .AddCheck(
                "stripe",
                new StripeHealthCheck(StripeHealthCheck.CreateClient(
                    stripeKey ?? throw new InvalidOperationException(
                        "Missing 'Stripe:SecretKey' configuration section."))),
                failureStatus: HealthStatus.Degraded);
    }
}