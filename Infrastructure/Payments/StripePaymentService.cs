using System.Net;
using Application.Abstractions.Services;
using Application.DTOs.Payments;
using Application.Exceptions;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Payments;

public sealed class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public string DefaultCurrency => _settings.Currency;

    public StripePaymentService(StripeSettings settings, ILogger<StripePaymentService> logger)
    {
        _settings = settings;
        _logger = logger;
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<StripeException>(IsTransient),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Transient Stripe failure, retrying (attempt {Attempt}).",
                        args.AttemptNumber + 1);
                    return default;
                },
            })
            .Build();
    }

    public async Task<ConnectOnboardingResponse> CreateOnboardingLinkAsync(
        string? existingAccountId,
        string storeEmail,
        CancellationToken cancellationToken = default)
    {
        var client = new StripeClient(_settings.SecretKey);

        var accountId = existingAccountId;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            var accountService = new AccountService(client);
            var account = await _pipeline.ExecuteAsync(
                async token => await accountService.CreateAsync(
                    new AccountCreateOptions
                    {
                        Type = "express",
                        Country = _settings.DefaultCountry,
                        Email = storeEmail,
                        Capabilities = new AccountCapabilitiesOptions
                        {
                            CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                            Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                        },
                    },
                    cancellationToken: token),
                cancellationToken);

            accountId = account.Id;
        }

        var linkService = new AccountLinkService(client);
        var link = await _pipeline.ExecuteAsync(
            async token => await linkService.CreateAsync(
                new AccountLinkCreateOptions
                {
                    Account = accountId,
                    RefreshUrl = _settings.ConnectRefreshUrl,
                    ReturnUrl = _settings.ConnectReturnUrl,
                    Type = "account_onboarding",
                },
                cancellationToken: token),
            cancellationToken);

        return new ConnectOnboardingResponse(accountId, link.Url);
    }

    public async Task<ConnectStatusResponse> GetAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var client = new StripeClient(_settings.SecretKey);
        var accountService = new AccountService(client);

        var account = await _pipeline.ExecuteAsync(
            async token => await accountService.GetAsync(accountId, cancellationToken: token),
            cancellationToken);

        return new ConnectStatusResponse(account.Id, account.ChargesEnabled && account.PayoutsEnabled);
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string destinationAccountId,
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationAccountId))
        {
            throw new OwnerNotOnboardedException(
                "The owning store has not connected a Stripe payout account.");
        }

        var client = new StripeClient(_settings.SecretKey);
        var sessionService = new SessionService(client);

        var session = await _pipeline.ExecuteAsync(
            async token => await sessionService.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    ClientReferenceId = reservationId.ToString("N"),
                    SuccessUrl = _settings.SuccessUrl,
                    CancelUrl = _settings.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        ["reservation_id"] = reservationId.ToString("N"),
                    },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = currency,
                                UnitAmount = ToMinorUnits(amount),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"StockMesh reservation {reservationId:N}",
                                },
                            },
                        },
                    },
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        TransferData = new SessionPaymentIntentDataTransferDataOptions
                        {
                            Destination = destinationAccountId,
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["reservation_id"] = reservationId.ToString("N"),
                        },
                    },
                },
                cancellationToken: token),
            cancellationToken);

        return new CheckoutSessionResponse(session.Id, session.Url);
    }

    private static long ToMinorUnits(decimal amount)
    {
        return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }

    private static bool IsTransient(StripeException exception)
    {
        return exception.HttpStatusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}