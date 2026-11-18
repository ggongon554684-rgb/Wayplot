using WayPlot.domain.Entities;

namespace WayPlot.domain.Services.MainTransaction;

public sealed record DealObligation(
    int Sequence,
    string Kind,
    DateTime DueDate,
    decimal Amount);

public sealed record AppliedDealObligation(
    DealObligation Obligation,
    decimal PaidAmount,
    decimal RemainingAmount);

public static class DealScheduleBuilder
{
    public static IReadOnlyList<DealObligation> Build(Deal deal, DateTime confirmedLocalDate)
    {
        var netPrice = deal.Price - deal.Discount;
        var obligations = new List<DealObligation>();
        var sequence = 1;

        if (deal.ReservationFee > 0)
            obligations.Add(new DealObligation(sequence++, "Reservation fee", confirmedLocalDate, deal.ReservationFee));
        if (deal.DownPayment > 0)
            obligations.Add(new DealObligation(sequence++, "Down payment", confirmedLocalDate, deal.DownPayment));

        if (!deal.IsInstallment)
        {
            var remaining = netPrice - deal.ReservationFee - deal.DownPayment;
            if (remaining > 0)
                obligations.Add(new DealObligation(sequence, "Full payment", confirmedLocalDate, remaining));
            return obligations;
        }

        if (deal.InstallmentCount <= 0)
            return obligations;

        var fixedBeforeInstallments = deal.ReservationFee + deal.DownPayment;
        var installmentTotal = netPrice - fixedBeforeInstallments;
        var dueDay = Math.Clamp(deal.DueDay, 1, 31);
        var firstDue = FirstDueDayAfter(confirmedLocalDate, dueDay);

        var totalAmount = 0m;
        for (var index = 0; index < deal.InstallmentCount; index++)
        {
            var isLast = index == deal.InstallmentCount - 1;
            var amount = isLast
                ? installmentTotal - totalAmount
                : decimal.Round(deal.InstallmentAmount, 2, MidpointRounding.AwayFromZero);
            amount = Math.Max(0, amount);
            var due = AddMonthsClamped(firstDue, index, dueDay);
            obligations.Add(new DealObligation(sequence++, $"Installment {index + 1}", due, amount));
            totalAmount += amount;
        }

        return obligations;
    }

    private static DateTime FirstDueDayAfter(DateTime confirmationDate, int dueDay)
    {
        var firstCandidate = MonthDay(confirmationDate.Year, confirmationDate.Month, dueDay);
        if (firstCandidate > confirmationDate)
            return firstCandidate;
        var next = confirmationDate.AddMonths(1);
        return MonthDay(next.Year, next.Month, dueDay);
    }

    private static DateTime AddMonthsClamped(DateTime firstDue, int months, int dueDay)
    {
        var yearMonth = firstDue.AddMonths(months);
        return MonthDay(yearMonth.Year, yearMonth.Month, dueDay);
    }

    private static DateTime MonthDay(int year, int month, int dueDay)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateTime(year, month, Math.Min(dueDay, lastDay), 0, 0, 0, DateTimeKind.Unspecified);
    }
}
