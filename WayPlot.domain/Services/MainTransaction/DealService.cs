using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using System.Globalization;

namespace WayPlot.domain.Services.MainTransaction;

public sealed class DealService(
    ITenantUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher events,
    ICurrentUser user,
    IClock clock,
    CommissionService commissions)
{
    private const decimal MaximumPrice = 9999999999999999.99m;
    private const int ReceiptNumberAttempts = 3;

    public async Task<ServiceResult<IReadOnlyList<DealListItem>>> ListAsync()
    {
        if (user.Role == Roles.SuperAdmin)
            return ServiceResult<IReadOnlyList<DealListItem>>.Fail("Super Admin cannot access tenant deal or payment records.");

        Guid? agentStaffId = null;
        Guid? branchId = null;
        if (user.Role == Roles.Agent)
        {
            if (user.UserId is not { } currentAgentId)
                return ServiceResult<IReadOnlyList<DealListItem>>.Fail("Your staff account could not be identified.");
            agentStaffId = currentAgentId;
        }
        else if (user.Role == Roles.BranchManager)
        {
            if (user.BranchId is not { } currentBranchId)
                return ServiceResult<IReadOnlyList<DealListItem>>.Fail("Your staff account has no assigned branch. Ask your Tenant Admin.");
            branchId = currentBranchId;
        }
        else if (user.Role != Roles.TenantAdmin)
        {
            return ServiceResult<IReadOnlyList<DealListItem>>.Fail("Your role cannot access tenant deal or payment records.");
        }

        await using var uow = uowFactory.Create();
        var deals = await uow.Deals.ListAsync(agentStaffId, branchId);
        IReadOnlyList<DealListItem> items = deals.Select(ToListItem).ToList();
        return ServiceResult<IReadOnlyList<DealListItem>>.Success(items);
    }

    public async Task<ServiceResult<IReadOnlyList<DealCandidateItem>>> ListCandidatesAsync()
    {
        if (user.Role != Roles.Agent)
            return ServiceResult<IReadOnlyList<DealCandidateItem>>.Fail("Only an Agent can create a deal.");
        if (user.UserId is not { } agentStaffId)
            return ServiceResult<IReadOnlyList<DealCandidateItem>>.Fail("Your staff account could not be identified.");

        await using var uow = uowFactory.Create();
        var leads = await uow.Leads.ListAsync(LeadStage.Negotiation, null, agentStaffId);
        IReadOnlyList<DealCandidateItem> candidates = leads
            .Where(lead => !lead.CloseRequested
                && lead.Parcel.Status is ParcelStatus.Available or ParcelStatus.Reserved)
            .Select(lead => new DealCandidateItem(
                lead.Id,
                lead.Contact.FullName,
                lead.Parcel.Label,
                lead.OfferAmount,
                lead.Parcel.Price,
                lead.Parcel.ReservationFee))
            .ToList();
        return ServiceResult<IReadOnlyList<DealCandidateItem>>.Success(candidates);
    }

    public async Task<ServiceResult<DealListItem>> CreateAsync(CreateDealRequest request)
    {
        if (user.Role != Roles.Agent)
            return ServiceResult<DealListItem>.Fail("Only an Agent can create a deal.");
        if (user.UserId is not { } agentStaffId)
            return ServiceResult<DealListItem>.Fail("Your staff account could not be identified.");

        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(request.LeadId);
        if (lead is null)
            return ServiceResult<DealListItem>.Fail("That lead was not found.");
        if (lead.AssignedStaffId != agentStaffId)
            return ServiceResult<DealListItem>.Fail("You can only request a deal for a lead assigned to you.");
        if (lead.Stage != LeadStage.Negotiation)
            return ServiceResult<DealListItem>.Fail("A deal can only be created from a lead in Negotiation.");
        if (lead.CloseRequested)
            return ServiceResult<DealListItem>.Fail("This lead already has a pending close request.");
        if (lead.Parcel.Status is not (ParcelStatus.Available or ParcelStatus.Reserved))
            return ServiceResult<DealListItem>.Fail($"Parcel {lead.Parcel.Label} is {lead.Parcel.Status} and cannot be sold in a new deal.");
        if (await uow.Deals.ActiveForParcelExistsAsync(lead.ParcelId))
            return ServiceResult<DealListItem>.Fail($"Parcel {lead.Parcel.Label} already has an active deal.");

        var defaultPrice = lead.OfferAmount is > 0 ? lead.OfferAmount.Value : lead.Parcel.Price;
        var price = request.Price ?? defaultPrice;
        if (price <= 0 || price > MaximumPrice)
            return ServiceResult<DealListItem>.Fail("The deal price must be greater than zero and within the supported amount range.");
        if (request.DownPayment < 0 || request.DownPayment > price
            || decimal.Round(request.DownPayment, 2) != request.DownPayment)
            return ServiceResult<DealListItem>.Fail("The down payment must be between zero and the deal price, with at most two decimal places.");
        if (lead.Parcel.ReservationFee > price)
            return ServiceResult<DealListItem>.Fail("The parcel's reservation fee cannot be greater than the deal price.");

        var now = clock.UtcNow;
        var deal = new Deal
        {
            LeadId = lead.Id,
            Lead = lead,
            ParcelId = lead.ParcelId,
            Parcel = lead.Parcel,
            ContactId = lead.ContactId,
            Contact = lead.Contact,
            AgentStaffId = agentStaffId,
            Price = price,
            Discount = 0,
            ReservationFee = lead.Parcel.ReservationFee,
            DownPayment = request.DownPayment,
            IsInstallment = false,
            Status = DealStatus.Current,
            IsActive = true
        };
        lead.CloseRequested = true;
        lead.LastActivityAtUtc = now;
        uow.Deals.Add(deal);

        await uow.SaveAsync();
        return ServiceResult<DealListItem>.Success(ToListItem(deal), "Closed request submitted to the Tenant Admin for confirmation.");
    }

    public async Task<ServiceResult<DealListItem>> ConfirmAsync(Guid dealId)
    {
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<DealListItem>.Fail("Only a Tenant Admin can confirm a deal.");
        if (user.UserId is not { } adminStaffId)
            return ServiceResult<DealListItem>.Fail("Your staff account could not be identified.");

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealListItem>.Fail("That deal was not found.");
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is not null || !deal.IsActive)
            return ServiceResult<DealListItem>.Fail("This deal is no longer waiting for confirmation.");

        var rule = PipelineRules.CanConfirmDeal(deal.Lead);
        if (!rule.Ok)
            return ServiceResult<DealListItem>.Fail(rule.Message);

        var payoutResult = await commissions.AddForConfirmedDealAsync(uow, deal);
        if (!payoutResult.Ok)
            return ServiceResult<DealListItem>.Fail(payoutResult.Message);

        var from = deal.Lead.Stage;
        var now = clock.UtcNow;
        deal.ConfirmedAtUtc = now;
        deal.ConfirmedByStaffId = adminStaffId;
        deal.Lead.Stage = LeadStage.Closed;
        deal.Lead.CloseRequested = false;
        deal.Lead.LastActivityAtUtc = now;
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = deal.LeadId,
            FromStage = from,
            ToStage = LeadStage.Closed,
            ChangedAtUtc = now,
            ChangedByStaffId = adminStaffId,
            Note = "Deal confirmed"
        });

        await uow.SaveAsync();
        await events.DispatchAsync(new LeadStageChanged(deal.LeadId, from, LeadStage.Closed, now));
        return ServiceResult<DealListItem>.Success(ToListItem(deal), "Deal confirmed and lead closed.");
    }

    public async Task<ServiceResult<DealListItem>> CancelAsync(Guid dealId)
    {
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<DealListItem>.Fail("Only a Tenant Admin can cancel a deal.");

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealListItem>.Fail("That deal was not found.");
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is not null || !deal.IsActive)
            return ServiceResult<DealListItem>.Fail("Only an unconfirmed deal can be canceled.");
        if (deal.Payments.Any(payment => payment.Status != PaymentStatus.Returned))
            return ServiceResult<DealListItem>.Fail("This deal has payment activity. Payment returns or refunds must be handled before it can be canceled.");

        deal.Status = DealStatus.Cancelled;
        deal.IsActive = false;
        deal.Lead.CloseRequested = false;
        deal.Lead.LastActivityAtUtc = clock.UtcNow;
        await commissions.HandleDealEndedAsync(uow, deal.Id);
        await uow.SaveAsync();

        return ServiceResult<DealListItem>.Success(ToListItem(deal), "Deal canceled. The parcel status was not changed.");
    }

    public async Task<ServiceResult<IReadOnlyList<PaymentListItem>>> ListPaymentsAsync(Guid dealId)
    {
        if (user.Role == Roles.SuperAdmin)
            return ServiceResult<IReadOnlyList<PaymentListItem>>.Fail("Super Admin cannot access tenant deal or payment records.");

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<IReadOnlyList<PaymentListItem>>.Fail("That deal was not found.");

        var accessFailure = DealAccessFailure(deal);
        if (accessFailure is not null)
            return ServiceResult<IReadOnlyList<PaymentListItem>>.Fail(accessFailure);

        var payments = await uow.Payments.ListByDealAsync(dealId);
        IReadOnlyList<PaymentListItem> items = payments.Select(ToPaymentListItem).ToList();
        return ServiceResult<IReadOnlyList<PaymentListItem>>.Success(items);
    }

    public async Task<ServiceResult<PaymentListItem>> RecordPaymentAsync(RecordPaymentRequest request)
    {
        if (user.Role != Roles.Agent)
            return ServiceResult<PaymentListItem>.Fail("Only an Agent can record a payment.");
        if (user.UserId is not { } agentStaffId)
            return ServiceResult<PaymentListItem>.Fail("Your staff account could not be identified.");
        if (!Enum.IsDefined(typeof(PaymentMethod), request.Method))
            return ServiceResult<PaymentListItem>.Fail("Select a valid payment method.");
        if (request.Amount <= 0 || request.Amount > MaximumPrice)
            return ServiceResult<PaymentListItem>.Fail("The payment amount must be greater than zero and within the supported amount range.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            return ServiceResult<PaymentListItem>.Fail("The payment amount can have at most two decimal places.");

        for (var attempt = 0; attempt < ReceiptNumberAttempts; attempt++)
        {
            var now = clock.UtcNow;
            await using var uow = uowFactory.Create();
            var deal = await uow.Deals.GetAsync(request.DealId);
            if (deal is null)
                return ServiceResult<PaymentListItem>.Fail("That deal was not found.");
            if (deal.AgentStaffId != agentStaffId)
                return ServiceResult<PaymentListItem>.Fail("Agents can only record payments for their own customers.");
            if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is null || !deal.IsActive)
                return ServiceResult<PaymentListItem>.Fail("Payments can only be recorded for an active, confirmed deal.");
            if (deal.Parcel.Status is ParcelStatus.Sold or ParcelStatus.Leased)
                return ServiceResult<PaymentListItem>.Fail($"Parcel {deal.Parcel.Label} is {deal.Parcel.Status}; no further payment can be recorded.");

            var allocatedAmount = deal.Payments
                .Where(payment => payment.Status != PaymentStatus.Returned)
                .Sum(payment => payment.Amount);
            var remaining = Math.Max(0, deal.Price - allocatedAmount);
            if (request.Amount > remaining)
                return ServiceResult<PaymentListItem>.Fail(
                    $"The payment exceeds the remaining balance of ₱{remaining.ToString("N2", CultureInfo.InvariantCulture)}. Pending and confirmed payments both count toward the deal price.");

            var prefix = $"RCPT-{now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-";
            var lastSequence = await uow.Payments.MaxReceiptSequenceAsync(prefix);
            if (lastSequence == int.MaxValue)
                return ServiceResult<PaymentListItem>.Fail("The receipt sequence for today is full. Contact your Tenant Admin.");

            var payment = new Payment
            {
                DealId = deal.Id,
                Deal = deal,
                PaidAtUtc = now,
                Amount = request.Amount,
                Method = request.Method,
                ReceiptNo = prefix + (lastSequence + 1).ToString("D4", CultureInfo.InvariantCulture),
                RecordedByStaffId = agentStaffId,
                Status = PaymentStatus.AwaitingAdmin
            };
            uow.Payments.Add(payment);

            try
            {
                await uow.SaveAsync();
            }
            catch (ReceiptNumberConflictException) when (attempt + 1 < ReceiptNumberAttempts)
            {
                continue;
            }
            catch (ReceiptNumberConflictException)
            {
                return ServiceResult<PaymentListItem>.Fail("A unique receipt number could not be reserved after several attempts. Please try again.");
            }

            return ServiceResult<PaymentListItem>.Success(
                ToPaymentListItem(payment),
                $"Payment recorded and awaiting Tenant Admin review. Receipt: {payment.ReceiptNo}.");
        }

        return ServiceResult<PaymentListItem>.Fail("A unique receipt number could not be reserved. Please try again.");
    }

    public async Task<ServiceResult<PaymentListItem>> ConfirmPaymentAsync(Guid paymentId)
    {
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<PaymentListItem>.Fail("Only a Tenant Admin can confirm a payment.");
        if (user.UserId is not { } adminStaffId)
            return ServiceResult<PaymentListItem>.Fail("Your staff account could not be identified.");

        await using var uow = uowFactory.Create();
        var payment = await uow.Payments.GetAsync(paymentId);
        if (payment is null)
            return ServiceResult<PaymentListItem>.Fail("That payment was not found.");
        if (payment.Status != PaymentStatus.AwaitingAdmin)
            return ServiceResult<PaymentListItem>.Fail("Only a payment awaiting review can be confirmed.");

        var deal = payment.Deal;
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is null || !deal.IsActive)
            return ServiceResult<PaymentListItem>.Fail("The payment's deal is not active and confirmed.");
        if (deal.Parcel.Status is ParcelStatus.Sold or ParcelStatus.Leased)
            return ServiceResult<PaymentListItem>.Fail($"Parcel {deal.Parcel.Label} is {deal.Parcel.Status}; this payment cannot be confirmed.");

        var now = clock.UtcNow;
        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAtUtc = now;
        payment.ConfirmedByStaffId = adminStaffId;

        var paidTotal = deal.Payments
            .Where(item => item.Status == PaymentStatus.Confirmed)
            .Sum(item => item.Amount);
        if (paidTotal >= deal.Price)
        {
            deal.Status = DealStatus.FullyPaid;
            deal.Parcel.Status = ParcelStatus.Sold;
        }
        else if (deal.Parcel.Status == ParcelStatus.Available && paidTotal >= deal.ReservationFee)
        {
            deal.Parcel.Status = ParcelStatus.Reserved;
        }

        await uow.SaveAsync();
        await events.DispatchAsync(new PaymentConfirmed(deal.Id, payment.Id, now));

        var message = deal.Status == DealStatus.FullyPaid
            ? "Payment confirmed. The deal is fully paid and the parcel is Sold."
            : deal.Parcel.Status == ParcelStatus.Reserved
                ? "Payment confirmed. The parcel is Reserved."
                : "Payment confirmed.";
        return ServiceResult<PaymentListItem>.Success(ToPaymentListItem(payment), message);
    }

    public async Task<ServiceResult<PaymentListItem>> ReturnPaymentAsync(Guid paymentId, string? reviewNote)
    {
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<PaymentListItem>.Fail("Only a Tenant Admin can return a payment.");

        await using var uow = uowFactory.Create();
        var payment = await uow.Payments.GetAsync(paymentId);
        if (payment is null)
            return ServiceResult<PaymentListItem>.Fail("That payment was not found.");
        if (payment.Status != PaymentStatus.AwaitingAdmin)
            return ServiceResult<PaymentListItem>.Fail("Only a payment awaiting review can be returned. Refunds for confirmed payments are outside this phase.");

        payment.Status = PaymentStatus.Returned;
        payment.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        await uow.SaveAsync();

        return ServiceResult<PaymentListItem>.Success(
            ToPaymentListItem(payment),
            "Payment returned to the Agent. No parcel status was changed.");
    }

    private static DealListItem ToListItem(Deal deal)
    {
        var paidTowardReservation = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .Sum(payment => payment.Amount);
        var allocatedAmount = deal.Payments
            .Where(payment => payment.Status != PaymentStatus.Returned)
            .Sum(payment => payment.Amount);
        var reservationFeeDue = Math.Max(0, deal.ReservationFee - paidTowardReservation);
        var remainingBalance = Math.Max(0, deal.Price - allocatedAmount);
        return new DealListItem(
            deal.Id,
            deal.LeadId,
            deal.Lead.Contact.FullName,
            deal.Lead.Parcel.Label,
            deal.Lead.Stage,
            deal.Lead.CloseRequested,
            deal.Price,
            deal.DownPayment,
            deal.ReservationFee,
            reservationFeeDue,
            remainingBalance,
            deal.Status,
            deal.ConfirmedAtUtc is not null,
            deal.IsActive,
            deal.CreatedAtUtc,
            deal.Lead.Parcel.Status,
            deal.Payments.Any(payment => payment.Status != PaymentStatus.Returned));
    }

    private static PaymentListItem ToPaymentListItem(Payment payment) =>
        new(payment.Id, payment.DealId, payment.PaidAtUtc, payment.Amount, payment.Method,
            payment.ReceiptNo, payment.Status, payment.ReviewNote);

    private string? DealAccessFailure(Deal deal)
    {
        if (user.Role == Roles.TenantAdmin)
            return null;
        if (user.Role == Roles.Agent)
            return deal.AgentStaffId == user.UserId
                ? null
                : "Agents can only access deals and payments for their own customers.";
        if (user.Role == Roles.BranchManager)
            return user.BranchId is { } branchId && deal.BranchId == branchId
                ? null
                : user.BranchId is null
                    ? "Your staff account has no assigned branch. Ask your Tenant Admin."
                    : "This deal is outside your branch.";
        if (user.Role == Roles.SuperAdmin)
            return "Super Admin cannot access tenant deal or payment records.";
        return "Your role cannot access tenant deal or payment records.";
    }
}
