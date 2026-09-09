using Application.Abstractions.Locking;
using Application.Abstractions.Options;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Features.MarketSignals.Services;
using Application.Features.Metrics.Services;
using Application.Features.Reservations.Services;
using Infrastructure.BackgroundJobs;
using Infrastructure.ExternalServices;
using Infrastructure.Identity;
using Infrastructure.Locking;
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
        services.AddScoped<IDailyMarketSignalRepository, DailyMarketSignalRepository>();
        services.AddScoped<ReservationExpiryProcessor>();
        services.AddScoped<MarketSignalAggregator>();
        services.AddHostedService<ReservationExpirySweeper>();
        services.AddHostedService<MarketSignalSweeper>();

        services.AddSingleton<MarketSignalOptions>(_ =>
            configuration.GetSection(MarketSignalOptions.SectionName).Get<MarketSignalOptions>()
            ?? new MarketSignalOptions());

        return services;
    }
}