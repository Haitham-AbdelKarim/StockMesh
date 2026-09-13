using Application.Common.Models;
using Application.DTOs.Payments;
using Application.Features.Payments.Commands.CreateConnectOnboarding;
using Application.Features.Payments.Queries.GetConnectStatus;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Payments;

public class ConnectOnboardingTests
{
    [Fact]
    public async Task Handle_OnboardWithoutAccount_CreatesLinkAndStoresAccountId()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);
        var paymentService = new FakePaymentService();
        var handler = new CreateConnectOnboardingCommandHandler(
            new FakeCurrentUser { StoreId = store.Id, Email = "owner@store.test" },
            new FakeStoreRepository(store),
            paymentService);

        var result = await handler.Handle(
            new CreateConnectOnboardingCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountId.Should().Be("acct_test_123");
        result.Value.OnboardingUrl.Should().NotBeNullOrEmpty();
        store.StripeConnectAccountId.Should().Be("acct_test_123");
    }

    [Fact]
    public async Task Handle_OnboardWithExistingAccount_RefreshesLinkForSameAccount()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);
        store.ConnectStripeAccount("acct_existing");
        string? seenExisting = null;
        var paymentService = new FakePaymentService
        {
            OnboardFactory = (existing, email) =>
            {
                seenExisting = existing;

                return new ConnectOnboardingResponse(
                    existing!, "https://connect.stripe.com/setup/refresh");
            },
        };
        var handler = new CreateConnectOnboardingCommandHandler(
            new FakeCurrentUser { StoreId = store.Id, Email = "owner@store.test" },
            new FakeStoreRepository(store),
            paymentService);

        var result = await handler.Handle(
            new CreateConnectOnboardingCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        seenExisting.Should().Be("acct_existing");
        store.StripeConnectAccountId.Should().Be("acct_existing");
    }

    [Fact]
    public async Task Handle_OnboardWhenStoreMissing_ReturnsNotFound()
    {
        var handler = new CreateConnectOnboardingCommandHandler(
            new FakeCurrentUser { StoreId = Guid.NewGuid() },
            new FakeStoreRepository(),
            new FakePaymentService());

        var result = await handler.Handle(
            new CreateConnectOnboardingCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_StatusWithoutAccount_ReturnsUnconnected()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);
        var handler = new GetConnectStatusQueryHandler(
            new FakeCurrentUser { StoreId = store.Id },
            new FakeStoreRepository(store),
            new FakePaymentService());

        var result = await handler.Handle(new GetConnectStatusQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountId.Should().BeNull();
        result.Value.PayoutsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_StatusWithAccount_RefreshesPayoutFlag()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);
        store.ConnectStripeAccount("acct_test_123");
        var paymentService = new FakePaymentService
        {
            StatusFactory = accountId => new ConnectStatusResponse(
                accountId, true),
        };
        var handler = new GetConnectStatusQueryHandler(
            new FakeCurrentUser { StoreId = store.Id },
            new FakeStoreRepository(store),
            paymentService);

        var result = await handler.Handle(new GetConnectStatusQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PayoutsEnabled.Should().BeTrue();
        store.PayoutsEnabled.Should().BeTrue();
    }
}