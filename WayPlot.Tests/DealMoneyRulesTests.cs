using System.Reflection;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Services.MainTransaction;
using Xunit;

namespace WayPlot.Tests;

public sealed class DealMoneyRulesTests
{
    private static readonly DateTime AsOfDate = new(2026, 11, 18);

    [Fact]
    public void Current_installment_deal_matches_the_contract_example()
    {
        var deal = CreateDeal(
            price: 300_000m,
            reservation: 10_000m,
            downPayment: 40_000m,
            isInstallment: true,
            installmentCount: 12,
            installmentAmount: 20_833.33m,
            dueDay: 31,
            confirmedLocalDate: new DateTime(2026, 11, 1),
            payments: [Payment(50_000m, PaymentStatus.Confirmed)]);

        var obligations = DealScheduleBuilder.Build(deal, deal.ConfirmedAtUtc!.Value.ToLocalTime().Date);
        var summary = BuildSummary(deal, AsOfDate);

        Assert.Equal(14, obligations.Count);
        Assert.Equal(20_833.33m, obligations[2].Amount);
        Assert.Equal(20_833.33m, obligations[12].Amount);
        Assert.Equal(20_833.37m, obligations[13].Amount);
        Assert.Equal(300_000m, obligations.Sum(item => item.Amount));
        Assert.Equal(new DateTime(2026, 11, 30), summary.NextDueDate);
        Assert.Equal(20_833.33m, summary.NextDueAmount);
        Assert.Equal(300_000m, summary.NetPrice);
        Assert.Equal(50_000m, summary.ConfirmedPaid);
        Assert.Equal(250_000m, summary.Balance);
        Assert.Equal(16.67m, summary.PercentPaid);
        Assert.Equal(0m, summary.OverdueAmount);
        Assert.Equal(0, summary.DaysOverdue);
        Assert.Equal(0, summary.InstallmentsPaid);
        Assert.Equal(12, summary.InstallmentsTotal);
    }

    [Fact]
    public void Overdue_installment_deal_matches_the_contract_example()
    {
        var deal = CreateDeal(
            price: 240_000m,
            reservation: 0m,
            downPayment: 20_000m,
            isInstallment: true,
            installmentCount: 10,
            installmentAmount: 22_000m,
            dueDay: 31,
            confirmedLocalDate: new DateTime(2026, 1, 1),
            payments: [Payment(20_000m, PaymentStatus.Confirmed)]);

        var obligations = DealScheduleBuilder.Build(deal, deal.ConfirmedAtUtc!.Value.ToLocalTime().Date);
        var installments = obligations.Where(item => item.Kind.StartsWith("Installment ", StringComparison.Ordinal)).ToList();
        var summary = BuildSummary(deal, AsOfDate);

        DateTime[] expectedDueDates =
        [
            new(2026, 1, 31), new(2026, 2, 28), new(2026, 3, 31), new(2026, 4, 30),
            new(2026, 5, 31), new(2026, 6, 30), new(2026, 7, 31), new(2026, 8, 31),
            new(2026, 9, 30), new(2026, 10, 31)
        ];

        Assert.Equal(expectedDueDates, installments.Select(item => item.DueDate));
        Assert.Equal(new DateTime(2026, 1, 31), summary.NextDueDate);
        Assert.Equal(22_000m, summary.NextDueAmount);
        Assert.Equal(240_000m, summary.NetPrice);
        Assert.Equal(20_000m, summary.ConfirmedPaid);
        Assert.Equal(220_000m, summary.Balance);
        Assert.Equal(8.33m, summary.PercentPaid);
        Assert.Equal(220_000m, summary.OverdueAmount);
        Assert.Equal(291, summary.DaysOverdue);
        Assert.Equal(0, summary.InstallmentsPaid);
        Assert.Equal(10, summary.InstallmentsTotal);
    }

    [Fact]
    public void Fully_paid_deal_matches_the_contract_example()
    {
        var deal = CreateDeal(
            price: 180_000m,
            reservation: 5_000m,
            downPayment: 25_000m,
            isInstallment: false,
            installmentCount: 0,
            installmentAmount: 0m,
            dueDay: 1,
            confirmedLocalDate: new DateTime(2026, 9, 1),
            payments: [Payment(180_000m, PaymentStatus.Confirmed)]);

        var obligations = DealScheduleBuilder.Build(deal, deal.ConfirmedAtUtc!.Value.ToLocalTime().Date);
        var summary = BuildSummary(deal, AsOfDate);

        Assert.Equal(new[] { 5_000m, 25_000m, 150_000m }, obligations.Select(item => item.Amount));
        Assert.Equal(180_000m, obligations.Sum(item => item.Amount));
        Assert.Null(summary.NextDueDate);
        Assert.Null(summary.NextDueAmount);
        Assert.Equal(180_000m, summary.NetPrice);
        Assert.Equal(180_000m, summary.ConfirmedPaid);
        Assert.Equal(0m, summary.Balance);
        Assert.Equal(100m, summary.PercentPaid);
        Assert.Equal(0m, summary.OverdueAmount);
        Assert.Equal(0, summary.DaysOverdue);
    }

    [Fact]
    public void Installment_due_dates_clamp_to_february_and_thirty_day_months()
    {
        var deal = CreateDeal(
            price: 400m,
            reservation: 0m,
            downPayment: 0m,
            isInstallment: true,
            installmentCount: 4,
            installmentAmount: 100m,
            dueDay: 31,
            confirmedLocalDate: new DateTime(2026, 1, 1));

        var obligations = DealScheduleBuilder.Build(deal, new DateTime(2026, 1, 1));

        Assert.Equal(
            new[] { new DateTime(2026, 1, 31), new DateTime(2026, 2, 28), new DateTime(2026, 3, 31), new DateTime(2026, 4, 30) },
            obligations.Select(item => item.DueDate));
    }

    [Fact]
    public void Final_installment_adjusts_the_schedule_to_the_net_price()
    {
        var deal = CreateDeal(
            price: 101m,
            reservation: 0m,
            downPayment: 0m,
            isInstallment: true,
            installmentCount: 3,
            installmentAmount: 33.33m,
            dueDay: 15,
            confirmedLocalDate: new DateTime(2026, 1, 1));

        var obligations = DealScheduleBuilder.Build(deal, new DateTime(2026, 1, 1));

        Assert.Equal(34.34m, obligations[^1].Amount);
        Assert.Equal(101m, obligations.Sum(item => item.Amount));
    }

    [Fact]
    public void Confirmed_payments_cover_obligations_in_order_and_pending_payments_do_not_change_them()
    {
        var deal = CreateDeal(
            price: 1_000m,
            reservation: 100m,
            downPayment: 200m,
            isInstallment: true,
            installmentCount: 2,
            installmentAmount: 350m,
            dueDay: 31,
            confirmedLocalDate: new DateTime(2026, 1, 1),
            payments:
            [
                Payment(50m, PaymentStatus.Confirmed),
                Payment(100m, PaymentStatus.Confirmed),
                Payment(900m, PaymentStatus.AwaitingAdmin)
            ]);

        var summary = BuildSummary(deal, new DateTime(2026, 1, 15));

        Assert.Equal(150m, summary.ConfirmedPaid);
        Assert.Equal(900m, summary.PendingPaymentsTotal);
        Assert.Equal(850m, summary.Balance);
        Assert.Equal(new DateTime(2026, 1, 1), summary.NextDueDate);
        Assert.Equal(150m, summary.NextDueAmount);
        Assert.Equal(150m, summary.OverdueAmount);
    }

    private static DealSummaryDto BuildSummary(Deal deal, DateTime asOfDate)
    {
        var localNow = asOfDate.Date.AddHours(12);
        var timeZone = TimeZoneInfo.Local;
        var fixedTimeProvider = new FixedTimeProvider(
            new DateTimeOffset(localNow, timeZone.GetUtcOffset(localNow)));
        var service = new DealService(null!, null!, null!, null!, null!, null!, fixedTimeProvider);

        // Invoke the domain projection directly, bypassing DealService repository workflows.
        var builder = typeof(DealService).GetMethod("BuildDealSummary", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Deal summary builder was not found.");
        return (DealSummaryDto)(builder.Invoke(service, [deal, null])
            ?? throw new InvalidOperationException("Deal summary builder returned no summary."));
    }

    private static Deal CreateDeal(
        decimal price,
        decimal reservation,
        decimal downPayment,
        bool isInstallment,
        int installmentCount,
        decimal installmentAmount,
        int dueDay,
        DateTime confirmedLocalDate,
        List<Payment>? payments = null)
    {
        var confirmedLocalTime = confirmedLocalDate.Date.AddHours(12);
        var offset = TimeZoneInfo.Local.GetUtcOffset(confirmedLocalTime);
        var confirmedAtUtc = new DateTimeOffset(confirmedLocalTime, offset).UtcDateTime;
        var deal = new Deal
        {
            Price = price,
            ReservationFee = reservation,
            DownPayment = downPayment,
            IsInstallment = isInstallment,
            InstallmentCount = installmentCount,
            InstallmentAmount = installmentAmount,
            DueDay = dueDay,
            ConfirmedAtUtc = confirmedAtUtc,
            Status = DealStatus.Current,
            Parcel = new Parcel { Label = "Test parcel", Status = ParcelStatus.Sold },
            Payments = payments ?? []
        };
        foreach (var payment in deal.Payments)
            payment.Deal = deal;
        return deal;
    }

    private static Payment Payment(decimal amount, PaymentStatus status) => new()
    {
        Amount = amount,
        Status = status
    };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
    }
}
