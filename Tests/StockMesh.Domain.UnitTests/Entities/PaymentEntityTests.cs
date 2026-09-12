using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class PaymentEntityTests
{
    [Fact]
    public void ReservationPayment_created_pending_with_positive_amount()
    {
        var payment = new ReservationPayment(
            Guid.NewGuid(), "cs_test_123", "acct_test_123", 30m, "usd",
            "https://checkout.stripe.com/pay/test");

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Amount.Should().Be(30m);
        payment.CheckoutUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ReservationPayment_rejects_non_positive_amount()
    {
        var act = () => new ReservationPayment(
            Guid.NewGuid(), "cs_test_123", "acct_test_123", 0m, "usd");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReservationPayment_paid_then_failed_throws()
    {
        var payment = new ReservationPayment(
            Guid.NewGuid(), "cs_test_123", "acct_test_123", 30m, "usd");
        payment.MarkPaid();

        var act = () => payment.MarkFailed();

        act.Should().Throw<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void ReservationPayment_failed_can_reopen_for_retry()
    {
        var payment = new ReservationPayment(
            Guid.NewGuid(), "cs_old", "acct_test_123", 30m, "usd");
        payment.MarkFailed();

        payment.ReopenForRetry("cs_new", "https://checkout.stripe.com/pay/new");

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.StripeSessionId.Should().Be("cs_new");
    }

    [Fact]
    public void ReservationPayment_pending_cannot_reopen_for_retry()
    {
        var payment = new ReservationPayment(
            Guid.NewGuid(), "cs_test_123", "acct_test_123", 30m, "usd");

        var act = () => payment.ReopenForRetry("cs_new", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Store_connect_stripe_account_and_payout_status()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);

        store.StripeConnectAccountId.Should().BeNull();
        store.PayoutsEnabled.Should().BeFalse();

        store.ConnectStripeAccount("acct_test_123");
        store.SetPayoutStatus(true);

        store.StripeConnectAccountId.Should().Be("acct_test_123");
        store.PayoutsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Store_connect_stripe_account_rejects_empty_id()
    {
        var store = new Store("Owner", VerticalCategory.Pharmacy, 30.0, 31.0);

        var act = () => store.ConnectStripeAccount("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessedStripeEvent_rejects_empty_event_id()
    {
        var act = () => new ProcessedStripeEvent("  ");

        act.Should().Throw<ArgumentException>();
    }
}