using Application.Abstractions.Locking;
using Application.Abstractions.Options;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Features.MarketSignals.Services;
using Application.Features.Metrics.Services;
using Application.Features.Recommendations.Services;
using Application.Features.Reservations.Services;
using Infrastructure.Assistant;
using Infrastructure.BackgroundJobs;
using Infrastructure.ExternalServices;
using Infrastructure.Identity;
using Infrastructure.Locking;
using Infrastructure.Payments;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IApplicationDbContext, AppDbContext>(
            options => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddIdentityCore<StoreUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IInventoryBatchRepository, InventoryBatchRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IStoreUserRepository, StoreUserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IDailyStoreMetricRepository, DailyStoreMetricRepository>();
        services.AddScoped<IDailyProductMetricRepository, DailyProductMetricRepository>();
        services.AddScoped<IDailyMetricsMaterializer, DailyMetricsMaterializer>();

        services.AddSingleton<ReservationOptions>(_ =>
            configuration.GetSection(ReservationOptions.SectionName).Get<ReservationOptions>()
            ?? new ReservationOptions());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IReservationLockService, RedisReservationLockService>();

        var forecastingSettings = configuration.GetSection(ForecastingSettings.SectionName).Get<ForecastingSettings>()
            ?? throw new InvalidOperationException(
                "Missing 'Forecasting' configuration section. Provide the forecasting-service address (see docker-compose.yml).");
        forecastingSettings.Validate();
        services.AddSingleton(forecastingSettings);
        services.AddHttpClient<IForecastingClient, ForecastingServiceClient>(client =>
        {
            client.BaseAddress = new Uri(forecastingSettings.BaseUrl, UriKind.Absolute);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(forecastingSettings.TimeoutSeconds);
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        var stripeSettings = configuration.GetSection(StripeSettings.SectionName).Get<StripeSettings>()
            ?? throw new InvalidOperationException(
                "Missing 'Stripe' configuration section. Provide Stripe test-mode keys (see appsettings.json).");
        stripeSettings.Validate();
        services.AddSingleton(stripeSettings);
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.AddScoped<IStripeWebhookVerifier, StripeWebhookVerifier>();

        var assistantSettings = configuration.GetSection(AssistantSettings.SectionName).Get<AssistantSettings>()
            ?? throw new InvalidOperationException(
                "Missing 'Assistant' configuration section. Provide the agent-service address (see docker-compose.yml).");
        assistantSettings.Validate();
        services.AddSingleton(assistantSettings);
        services.AddHttpClient<IAssistantClient, AssistantServiceClient>(client =>
        {
            client.BaseAddress = new Uri(assistantSettings.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(assistantSettings.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        var agentLogSettings = configuration.GetSection(AgentLogSettings.SectionName).Get<AgentLogSettings>()
            ?? throw new InvalidOperationException(
                "Missing 'AgentLog' configuration section. Provide the agent-service callback secret.");
        agentLogSettings.Validate();
        services.AddSingleton(agentLogSettings); services.AddScoped<IDailyMarketSignalRepository, DailyMarketSignalRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IReservationPaymentRepository, ReservationPaymentRepository>();
        services.AddScoped<IProcessedStripeEventRepository, ProcessedStripeEventRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IAgentToolCallLogRepository, AgentToolCallLogRepository>();

        services.AddMemoryCache();
        services.AddSingleton<IAssistantRateLimiter, AssistantRateLimiter>();
        services.AddScoped<ReservationExpiryProcessor>();
        services.AddScoped<MarketSignalAggregator>();
        services.AddScoped<RecommendationGenerator>();
        services.AddHostedService<ReservationExpirySweeper>();
        services.AddHostedService<MarketSignalSweeper>();
        services.AddHostedService<RecommendationSweeper>();

        services.AddSingleton<MarketSignalOptions>(_ =>
            configuration.GetSection(MarketSignalOptions.SectionName).Get<MarketSignalOptions>()
            ?? new MarketSignalOptions());

        services.AddSingleton<RecommendationOptions>(_ =>
            configuration.GetSection(RecommendationOptions.SectionName).Get<RecommendationOptions>()
            ?? new RecommendationOptions());

        return services;
    }
}