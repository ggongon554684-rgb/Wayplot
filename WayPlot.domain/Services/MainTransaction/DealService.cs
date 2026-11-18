using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;
using System.Globalization;

namespace WayPlot.domain.Services.MainTransaction;

public sealed class DealService(
    ITenantUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher events,
    ICurrentUser user,
    IClock clock,
    CommissionService commissions,
    TenantAccessPolicy access,
    TimeProvider timeProvider)
{
    private const decimal MaximumPrice = 9999999999999999.99m;

    public async Task<ServiceResult<IReadOnlyList<DealListItem>>> ListAsync()
    {
        var scope = access.GetDataReadScope("deals");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<DealListItem>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var agentStaffId = scope.Value.Kind == TenantDataScopeKind.Agent ? scope.Value.StaffId : null;
        var branchId = scope.Value.Kind == TenantDataScopeKind.Branch ? scope.Value.BranchId : null;
        var deals = await uow.Deals.ListAsync(agentStaffId, branchId);
        IReadOnlyList<DealListItem> items = deals.Select(ToListItem).ToList();
        return ServiceResult<IReadOnlyList<DealListItem>>.Success(items);
    }

    public async Task<ServiceResult<DealSummaryDto>> GetDealSummaryAsync(Guid dealId)
    {
        var scope = access.GetDataReadScope("deal summaries");
        if (!scope.Ok)
            return ServiceResult<DealSummaryDto>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealSummaryDto>.Fail("That deal was not found.");
        if (DealAccessFailure(deal) is { } accessFailure)
            return ServiceResult<DealSummaryDto>.Fail(accessFailure);
        var agentName = await GetAgentNameAsync(uow, deal.AgentStaffId);
        return ServiceResult<DealSummaryDto>.Success(BuildDealSummary(deal, agentName));
    }

    public async Task<ServiceResult<IReadOnlyList<DealSummaryDto>>> GetDealSummariesForContactAsync(Guid contactId)
    {
        var scope = access.GetDataReadScope("deal summaries");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<DealSummaryDto>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var deals = await uow.Deals.ListByContactAsync(contactId, scope.Value);
        var agentIds = deals.Select(deal => deal.AgentStaffId).Distinct().ToArray();
        var agentNames = new Dictionary<Guid, string>();
        if (agentIds.Length > 0)
        {
            var staff = await uow.Staff.ListByIdsAsync(agentIds);
            agentNames = staff.ToDictionary(member => member.Id, member => member.FullName);
        }
        return ServiceResult<IReadOnlyList<DealSummaryDto>>.Success(
            deals.Select(deal => BuildDealSummary(deal,
                agentNames.TryGetValue(deal.AgentStaffId, out var name) ? name : null)).ToList());
    }

    public async Task<ServiceResult<bool>> RefreshDealStatusesAsync()
    {
        Guid[] dealIds;
        await using (var readUow = uowFactory.Create())
            dealIds = (await readUow.Deals.ListAsync(null, null)).Select(deal => deal.Id).ToArray();

        var conflicts = 0;
        foreach (var dealId in dealIds)
        {
            await using var uow = uowFactory.Create();
            var deal = await uow.Deals.GetAsync(dealId);
            if (deal is null)
                continue;
            if (deal.ConfirmedAtUtc is null || !deal.IsActive
                || deal.Status is DealStatus.FullyPaid or DealStatus.Cancelled or DealStatus.Defaulted)
                continue;

            var summary = BuildDealSummary(deal, null);
            var desired = summary.OverdueAmount > 0 ? DealStatus.Overdue : DealStatus.Current;
            if (deal.Status == desired)
                continue;
            deal.Status = desired;
            uow.Deals.Touch(deal);

            try
            {
                await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
            }
            catch (DealLifecycleConcurrencyConflictException)
            {
                conflicts++;
            }
        }
        return ServiceResult<bool>.Success(true, conflicts == 0
            ? "Deal statuses refreshed."
            : $"Deal statuses refreshed. {conflicts} conflicting row(s) were skipped.");
    }

    public async Task<ServiceResult<DealTerminationSummary>> GetTerminationSummaryAsync(Guid dealId)
    {
        var scope = access.GetDataReadScope("deals and payouts");
        if (!scope.Ok)
            return ServiceResult<DealTerminationSummary>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealTerminationSummary>.Fail("That deal was not found.");
        if (DealAccessFailure(deal) is { } accessFailure)
            return ServiceResult<DealTerminationSummary>.Fail(accessFailure);

        var commissionsForDeal = await uow.Commissions.ListByDealAsync(dealId);
        var bonusesForDeal = await uow.ReferralBonuses.ListByDealAsync(dealId);
        var statuses = commissionsForDeal.Select(commission => commission.Status)
            .Concat(bonusesForDeal.Select(bonus => bonus.Status))
            .ToArray();
        return ServiceResult<DealTerminationSummary>.Success(new DealTerminationSummary(
            deal.Payments.Where(payment => payment.Status == PaymentStatus.Confirmed).Sum(payment => payment.Amount),
            statuses.Count(status => status is CommissionStatus.Pending or CommissionStatus.Approved),
            statuses.Count(status => status == CommissionStatus.Paid)));
    }

    public async Task<ServiceResult<IReadOnlyList<DealCandidateItem>>> ListCandidatesAsync()
    {
        var permission = access.RequireDataMutation("request Closed");
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<DealCandidateItem>>.Fail(permission.Message);

        var scope = access.GetDataReadScope("leads");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<DealCandidateItem>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var agentStaffId = scope.Value.Kind == TenantDataScopeKind.Agent ? scope.Value.StaffId : null;
        var leads = await uow.Leads.ListAsync(LeadStage.Negotiation, null, agentStaffId, scope.Value);
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

    public async Task<ServiceResult<DealListItem>> RequestClosedAsync(CreateDealRequest request)
    {
        var permission = access.RequireDataMutation("request Closed");
        if (!permission.Ok)
            return ServiceResult<DealListItem>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(request.LeadId);
        if (lead is null)
            return ServiceResult<DealListItem>.Fail("That lead was not found.");
        var leadPermission = access.RequireLeadMutation(lead, "request Closed");
        if (!leadPermission.Ok)
            return ServiceResult<DealListItem>.Fail(leadPermission.Message);
        if (user.Role == Roles.TenantAdmin && lead.AssignedStaffId is null)
            return ServiceResult<DealListItem>.Fail("Assign this lead to an agent before requesting Closed.");
        if (lead.Stage != LeadStage.Negotiation)
            return ServiceResult<DealListItem>.Fail("Closed can only be requested for a lead in Negotiation.");
        if (lead.CloseRequested)
            return ServiceResult<DealListItem>.Fail("This lead already has a pending close request.");
        if (lead.Parcel.Status is ParcelStatus.Sold or ParcelStatus.Leased)
            return ServiceResult<DealListItem>.Fail($"Closed was refused: parcel {lead.Parcel.Label} is {lead.Parcel.Status}.");
        if (lead.Parcel.Status is not (ParcelStatus.Available or ParcelStatus.Reserved))
            return ServiceResult<DealListItem>.Fail($"Closed was refused: parcel {lead.Parcel.Label} is {lead.Parcel.Status}.");

        var activeDeal = await uow.Deals.GetActiveForParcelAsync(lead.ParcelId);
        if (activeDeal is not null)
        {
            var existingState = activeDeal.ConfirmedAtUtc is null
                ? "pending request"
                : activeDeal.Status + " deal";
            return ServiceResult<DealListItem>.Fail(
                $"Closed was refused: parcel {lead.Parcel.Label} already has a {existingState}.");
        }

        var defaultPrice = lead.OfferAmount is > 0 ? lead.OfferAmount.Value : lead.Parcel.Price;
        var price = request.Price ?? defaultPrice;
        if (price <= 0 || price > MaximumPrice || decimal.Round(price, 2) != price)
            return ServiceResult<DealListItem>.Fail("The deal price must be greater than zero and within the supported amount range.");
        if (request.Discount < 0 || request.Discount >= price
            || decimal.Round(request.Discount, 2) != request.Discount)
            return ServiceResult<DealListItem>.Fail("The discount must be non-negative, have at most two decimal places, and be less than the deal price.");
        var netPrice = price - request.Discount;
        var reservationFee = request.ReservationFee ?? lead.Parcel.ReservationFee;
        if (reservationFee < 0 || reservationFee > netPrice
            || decimal.Round(reservationFee, 2) != reservationFee)
            return ServiceResult<DealListItem>.Fail("The reservation fee must be between zero and the net deal price, with at most two decimal places.");
        if (request.DownPayment < 0 || request.DownPayment > netPrice
            || decimal.Round(request.DownPayment, 2) != request.DownPayment)
            return ServiceResult<DealListItem>.Fail("The down payment must be between zero and the net deal price, with at most two decimal places.");
        if (request.IsInstallment
            && (request.InstallmentCount is < 1 or > 120 || request.DueDay is < 1 or > 31
                || request.DownPayment >= netPrice))
            return ServiceResult<DealListItem>.Fail("An installment plan needs 1 to 120 installments, a due day from 1 to 31, and a balance after down payment.");
        if (!request.IsInstallment && request.InstallmentCount != 0)
            return ServiceResult<DealListItem>.Fail("A full-payment deal cannot have an installment count.");

        var now = clock.UtcNow;
        var deal = new Deal
        {
            LeadId = lead.Id,
            Lead = lead,
            ParcelId = lead.ParcelId,
            Parcel = lead.Parcel,
            ContactId = lead.ContactId,
            Contact = lead.Contact,
            AgentStaffId = lead.AssignedStaffId ?? access.CurrentStaffId!.Value,
            Price = price,
            Discount = request.Discount,
            ReservationFee = reservationFee,
            DownPayment = request.DownPayment,
            IsInstallment = request.IsInstallment,
            InstallmentCount = request.IsInstallment ? request.InstallmentCount : 0,
            InstallmentAmount = request.IsInstallment
                ? decimal.Round((netPrice - request.DownPayment) / request.InstallmentCount, 2, MidpointRounding.AwayFromZero)
                : 0,
            DueDay = request.IsInstallment ? request.DueDay : 1,
            Status = DealStatus.Current,
            IsActive = true
        };
        lead.CloseRequested = true;
        lead.LastActivityAtUtc = now;
        if (lead.Parcel.Status == ParcelStatus.Available)
            SetParcelStatus(deal, ParcelStatus.Reserved);
        uow.Deals.Add(deal);

        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("Closed was refused because the deal or parcel changed concurrently. Reload and try again.");
        }
        catch (ActiveDealConflictException)
        {
            var active = await uow.Deals.GetActiveForParcelAsync(lead.ParcelId);
            var state = active is null
                ? "an active deal"
                : active.ConfirmedAtUtc is null ? "a pending request" : $"{active.Status} deal";
            return ServiceResult<DealListItem>.Fail(
                $"Closed was refused: parcel {lead.Parcel.Label} already has {state}.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("Closed was refused because the parcel changed concurrently. Reload and try again.");
        }

        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, now));
        return ServiceResult<DealListItem>.Success(ToListItem(deal), "Closed request submitted to the Tenant Admin for confirmation.");
    }

    public async Task<ServiceResult<DealListItem>> ConfirmClosedAsync(Guid dealId, ParcelStatus closingStatus)
    {
        var permission = access.RequireDataMutation("confirm Closed");
        if (!permission.Ok)
            return ServiceResult<DealListItem>.Fail(permission.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<DealListItem>.Fail("Only a Tenant Admin can confirm a deal.");
        if (user.UserId is not { } adminStaffId)
            return ServiceResult<DealListItem>.Fail("Your staff account could not be identified.");
        if (closingStatus is not (ParcelStatus.Sold or ParcelStatus.Leased))
            return ServiceResult<DealListItem>.Fail("Choose Sold or Leased when confirming Closed.");

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealListItem>.Fail("That deal was not found.");
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is not null || !deal.IsActive)
            return ServiceResult<DealListItem>.Fail("This deal is no longer waiting for confirmation.");
        if (!deal.Lead.CloseRequested)
            return ServiceResult<DealListItem>.Fail("This deal has no pending Closed request.");
        if (deal.Parcel.Status != ParcelStatus.Reserved)
            return ServiceResult<DealListItem>.Fail(
                $"Closed was refused because parcel {deal.Parcel.Label} is {deal.Parcel.Status}, not Reserved for this deal.");

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
        deal.Status = DealStatus.Current;
        deal.Contact.Type = ContactType.Client;
        SetParcelStatus(deal, closingStatus);
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

        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("Closed was not confirmed because the deal changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("Closed was not confirmed because the parcel changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new LeadStageChanged(deal.LeadId, from, LeadStage.Closed, now));
        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, now));
        return ServiceResult<DealListItem>.Success(ToListItem(deal), $"Deal confirmed as {closingStatus}; lead closed.");
    }

    public async Task<ServiceResult<DealListItem>> ReturnRequestAsync(Guid dealId)
    {
        var permission = access.RequireDataMutation("return a Closed request");
        if (!permission.Ok)
            return ServiceResult<DealListItem>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealListItem>.Fail("That deal was not found.");
        var leadPermission = access.RequireLeadMutation(deal.Lead, "return a Closed request");
        if (!leadPermission.Ok)
            return ServiceResult<DealListItem>.Fail(leadPermission.Message);
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is not null || !deal.IsActive || !deal.Lead.CloseRequested)
            return ServiceResult<DealListItem>.Fail("Only a pending Closed request can be returned.");

        deal.Status = DealStatus.Cancelled;
        deal.IsActive = false;
        deal.Lead.CloseRequested = false;
        deal.Lead.LastActivityAtUtc = clock.UtcNow;
        SetParcelStatus(deal, ParcelStatus.Available);
        var payoutEvents = await commissions.HandleDealEndedAsync(uow, deal.Id, deal.Lead.LastActivityAtUtc);
        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("The request was not returned because the deal changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail("The request was not returned because the parcel changed concurrently. Reload and try again.");
        }

        foreach (var payoutEvent in payoutEvents)
            await events.DispatchAsync(payoutEvent);
        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, deal.Lead.LastActivityAtUtc));
        return ServiceResult<DealListItem>.Success(ToListItem(deal), "Closed request returned. The parcel is Available and the lead remains in Negotiation.");
    }

    public Task<ServiceResult<DealListItem>> CancelDealAsync(
        Guid dealId, string reason, DealMoneyOutcome? moneyOutcome, decimal? refundedAmount) =>
        TerminateDealAsync(dealId, reason, moneyOutcome, refundedAmount, defaulted: false);

    public Task<ServiceResult<DealListItem>> DefaultDealAsync(
        Guid dealId, string reason, DealMoneyOutcome? moneyOutcome, decimal? refundedAmount) =>
        TerminateDealAsync(dealId, reason, moneyOutcome, refundedAmount, defaulted: true);

    private async Task<ServiceResult<DealListItem>> TerminateDealAsync(
        Guid dealId,
        string reason,
        DealMoneyOutcome? moneyOutcome,
        decimal? requestedRefundAmount,
        bool defaulted)
    {
        var action = defaulted ? "default" : "cancel";
        var actionPast = defaulted ? "defaulted" : "cancelled";
        var permission = access.RequireDataMutation($"{action} a confirmed deal");
        if (!permission.Ok)
            return ServiceResult<DealListItem>.Fail(permission.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<DealListItem>.Fail($"Only a Tenant Admin can {action} a confirmed deal.");
        if (user.UserId is not { } adminStaffId)
            return ServiceResult<DealListItem>.Fail("Your staff account could not be identified.");

        var normalizedReason = reason?.Trim() ?? string.Empty;
        if (normalizedReason.Length == 0)
            return ServiceResult<DealListItem>.Fail("A termination reason is required.");
        if (normalizedReason.Length > 500)
            return ServiceResult<DealListItem>.Fail("The termination reason cannot exceed 500 characters.");
        if (moneyOutcome is null || !Enum.IsDefined(typeof(DealMoneyOutcome), moneyOutcome.Value))
            return ServiceResult<DealListItem>.Fail("Choose what should happen to money already paid.");

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<DealListItem>.Fail("That deal was not found.");
        if (DealAccessFailure(deal) is { } accessFailure)
            return ServiceResult<DealListItem>.Fail(accessFailure);
        if (deal.Status == DealStatus.FullyPaid)
            return ServiceResult<DealListItem>.Fail($"A FullyPaid deal cannot be {actionPast}.");
        if (deal.Status == DealStatus.Cancelled)
            return ServiceResult<DealListItem>.Fail("This deal is already Cancelled.");
        if (deal.Status == DealStatus.Defaulted)
            return ServiceResult<DealListItem>.Fail("This deal is already Defaulted.");
        if (deal.ConfirmedAtUtc is null || !deal.IsActive
            || deal.Status is not (DealStatus.Current or DealStatus.Overdue))
            return ServiceResult<DealListItem>.Fail($"Only an active, confirmed Current or Overdue deal can be {actionPast}.");
        if (deal.Payments.Any(payment => payment.Status == PaymentStatus.AwaitingAdmin))
            return ServiceResult<DealListItem>.Fail("Confirm or return the pending payments first.");

        var confirmedPaidTotal = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .Sum(payment => payment.Amount);
        decimal? savedRefundAmount = moneyOutcome.Value switch
        {
            DealMoneyOutcome.Refunded => confirmedPaidTotal,
            DealMoneyOutcome.Kept => null,
            DealMoneyOutcome.PartlyRefunded when requestedRefundAmount is > 0
                && requestedRefundAmount < confirmedPaidTotal => requestedRefundAmount,
            DealMoneyOutcome.PartlyRefunded => null,
            _ => null
        };
        if (moneyOutcome == DealMoneyOutcome.PartlyRefunded && savedRefundAmount is null)
            return ServiceResult<DealListItem>.Fail(
                $"A partial refund must be greater than zero and less than the confirmed payments total of ₱{confirmedPaidTotal.ToString("N2", CultureInfo.InvariantCulture)}.");

        var now = clock.UtcNow;
        deal.Status = defaulted ? DealStatus.Defaulted : DealStatus.Cancelled;
        deal.IsActive = false;
        deal.TerminationReason = normalizedReason;
        deal.TerminationMoneyOutcome = moneyOutcome;
        deal.RefundedAmount = savedRefundAmount;
        deal.TerminatedAtUtc = now;
        deal.TerminatedByStaffId = adminStaffId;
        if (deal.Parcel.Status is ParcelStatus.Sold or ParcelStatus.Leased)
            SetParcelStatus(deal, ParcelStatus.Available);
        if (!await uow.Deals.HasOtherLiveDealForContactAsync(deal.ContactId, deal.Id))
            deal.Contact.Type = ContactType.Customer;

        var payoutEvents = await commissions.HandleDealEndedAsync(uow, deal.Id, now);
        try
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await uow.SaveAsync();
            });
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail(
                $"The deal was not {action}ed because the deal, contact, or payout changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<DealListItem>.Fail(
                $"The deal was not {action}ed because the parcel changed concurrently. Reload and try again.");
        }

        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, now));
        foreach (var payoutEvent in payoutEvents)
            await events.DispatchAsync(payoutEvent);
        var status = defaulted ? "Defaulted" : "Cancelled";
        return ServiceResult<DealListItem>.Success(ToListItem(deal), $"Deal {status}; termination details and payout updates were saved.");
    }

    public async Task<ServiceResult<IReadOnlyList<DealPaymentDto>>> GetPaymentsAsync(Guid dealId)
    {
        var scope = access.GetDataReadScope("deal payments");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<DealPaymentDto>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(dealId);
        if (deal is null)
            return ServiceResult<IReadOnlyList<DealPaymentDto>>.Fail("That deal was not found.");
        if (DealAccessFailure(deal) is { } accessFailure)
            return ServiceResult<IReadOnlyList<DealPaymentDto>>.Fail(accessFailure);

        var payments = await uow.Payments.ListByDealAsync(dealId);
        var recorderIds = payments.Where(payment => payment.RecordedByStaffId is not null)
            .Select(payment => payment.RecordedByStaffId!.Value).Distinct().ToArray();
        var recorders = recorderIds.Length == 0
            ? []
            : await uow.Staff.ListByIdsAsync(recorderIds);
        var names = recorders.ToDictionary(member => member.Id, member => member.FullName);
        var items = payments.Select(payment => new DealPaymentDto(
            payment.Id,
            payment.DealId,
            payment.PaidAtUtc,
            payment.Amount,
            payment.Method,
            payment.ReceiptNo,
            payment.Status,
            payment.RecordedByStaffId is { } recorderId && names.TryGetValue(recorderId, out var name)
                ? name : null,
            payment.ReviewNote)).ToList();
        return ServiceResult<IReadOnlyList<DealPaymentDto>>.Success(items);
    }

    public async Task<ServiceResult<IReadOnlyList<PaymentListItem>>> ListPaymentsAsync(Guid dealId)
    {
        var scope = access.GetDataReadScope("payments");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<PaymentListItem>>.Fail(scope.Message);

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
        var permission = access.RequireDataMutation("record a payment");
        if (!permission.Ok)
            return ServiceResult<PaymentListItem>.Fail(permission.Message);
        if (!Enum.IsDefined(typeof(PaymentMethod), request.Method))
            return ServiceResult<PaymentListItem>.Fail("Select a valid payment method.");
        if (request.Amount <= 0 || request.Amount > MaximumPrice)
            return ServiceResult<PaymentListItem>.Fail("The payment amount must be greater than zero and within the supported amount range.");
        if (decimal.Round(request.Amount, 2) != request.Amount)
            return ServiceResult<PaymentListItem>.Fail("The payment amount can have at most two decimal places.");
        var receiptNo = request.ReceiptNo?.Trim() ?? string.Empty;
        if (receiptNo.Length == 0 || receiptNo.Length > 50)
            return ServiceResult<PaymentListItem>.Fail("Enter a receipt number no longer than 50 characters.");

        var paidAtUtc = request.PaidAtUtc.Kind == DateTimeKind.Utc
            ? request.PaidAtUtc
            : request.PaidAtUtc.ToUniversalTime();
        await using var uow = uowFactory.Create();
        var deal = await uow.Deals.GetAsync(request.DealId);
        if (deal is null)
            return ServiceResult<PaymentListItem>.Fail("That deal was not found.");
        var accessFailure = DealAccessFailure(deal);
        if (accessFailure is not null)
            return ServiceResult<PaymentListItem>.Fail(accessFailure);
        if (deal.Status != DealStatus.Current || deal.ConfirmedAtUtc is null || !deal.IsActive)
            return ServiceResult<PaymentListItem>.Fail("Payments can only be recorded for an active, confirmed deal.");

        var remaining = RemainingBalance(deal);
        if (request.Amount > remaining)
            return ServiceResult<PaymentListItem>.Fail(
                $"Payment was refused: it exceeds the remaining confirmed balance of ₱{remaining.ToString("N2", CultureInfo.InvariantCulture)}.");
        if (deal.Payments.Any(payment => string.Equals(payment.ReceiptNo, receiptNo, StringComparison.OrdinalIgnoreCase)))
            return ServiceResult<PaymentListItem>.Fail("Payment was refused: this receipt number is already recorded.");
        if (deal.Payments.Any(payment => payment.Amount == request.Amount && payment.PaidAtUtc.Date == paidAtUtc.Date))
            return ServiceResult<PaymentListItem>.Fail("Payment was refused: the same amount and date are already recorded for this deal.");
        if (await uow.Payments.ReceiptExistsAsync(receiptNo))
            return ServiceResult<PaymentListItem>.Fail("Payment was refused: this receipt number is already recorded.");

        var payment = new Payment
        {
            DealId = deal.Id,
            Deal = deal,
            PaidAtUtc = paidAtUtc,
            Amount = request.Amount,
            Method = request.Method,
            ReceiptNo = receiptNo,
            RecordedByStaffId = access.CurrentStaffId,
            Status = PaymentStatus.AwaitingAdmin
        };
        uow.Payments.Add(payment);
        uow.Deals.Touch(deal);

        try
        {
            await uow.SaveAsync();
        }
        catch (ReceiptNumberConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was refused: this receipt number was recorded concurrently. Reload and try again.");
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not recorded because the deal changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not recorded because the parcel changed concurrently. Reload and try again.");
        }

        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, clock.UtcNow));
        return ServiceResult<PaymentListItem>.Success(
            ToPaymentListItem(payment),
            $"Payment recorded and awaiting Tenant Admin review. Receipt: {payment.ReceiptNo}.");
    }

    public async Task<ServiceResult<PaymentListItem>> ConfirmPaymentAsync(Guid paymentId)
    {
        var permission = access.RequireDataMutation("confirm a payment");
        if (!permission.Ok)
            return ServiceResult<PaymentListItem>.Fail(permission.Message);
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
        var remaining = RemainingBalance(deal);
        if (payment.Amount > remaining)
            return ServiceResult<PaymentListItem>.Fail(
                $"Payment was refused: it exceeds the remaining confirmed balance of ₱{remaining.ToString("N2", CultureInfo.InvariantCulture)}.");

        var now = clock.UtcNow;
        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAtUtc = now;
        payment.ConfirmedByStaffId = adminStaffId;

        var paidTotal = deal.Payments
            .Where(item => item.Status == PaymentStatus.Confirmed)
            .Sum(item => item.Amount);
        if (paidTotal >= deal.Price - deal.Discount)
            deal.Status = DealStatus.FullyPaid;
        uow.Deals.Touch(deal);

        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not confirmed because the deal or payment changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not confirmed because the parcel changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new PaymentConfirmed(deal.Id, payment.Id, now));
        await events.DispatchAsync(new DealChanged(deal.Id, deal.ParcelId, deal.ContactId, now));
        await RefreshDealStatusesAsync();

        var message = deal.Status == DealStatus.FullyPaid
            ? "Payment confirmed. The deal is fully paid; parcel status was not changed."
            : "Payment confirmed; only confirmed payments reduce the deal balance.";
        return ServiceResult<PaymentListItem>.Success(ToPaymentListItem(payment), message);
    }

    public async Task<ServiceResult<PaymentListItem>> ReturnPaymentAsync(Guid paymentId, string? reviewNote)
    {
        var permission = access.RequireDataMutation("return a payment");
        if (!permission.Ok)
            return ServiceResult<PaymentListItem>.Fail(permission.Message);
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
        uow.Deals.Touch(payment.Deal);
        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not returned because it changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<PaymentListItem>.Fail("Payment was not returned because the parcel changed concurrently. Reload and try again.");
        }

        await events.DispatchAsync(new DealChanged(payment.DealId, payment.Deal.ParcelId, payment.Deal.ContactId, clock.UtcNow));
        return ServiceResult<PaymentListItem>.Success(
            ToPaymentListItem(payment),
            "Payment returned to the Agent. No parcel status was changed.");
    }

    private async Task<string?> GetAgentNameAsync(ITenantUnitOfWork uow, Guid staffId)
    {
        var staff = await uow.Staff.ListByIdsAsync([staffId]);
        return staff.FirstOrDefault()?.FullName;
    }

    private DealSummaryDto BuildDealSummary(Deal deal, string? agentName)
    {
        var netPrice = deal.Price - deal.Discount;
        if (deal.ConfirmedAtUtc is null)
        {
            var pendingConfirmationTotal = deal.Payments
                .Where(payment => payment.Status == PaymentStatus.AwaitingAdmin)
                .Sum(payment => payment.Amount);
            DealType? pendingDealType = deal.Parcel.Status switch
            {
                ParcelStatus.Sold => DealType.Sale,
                ParcelStatus.Leased => DealType.Lease,
                _ => null
            };
            return new DealSummaryDto(
                deal.Id, deal.ContactId, deal.ParcelId, deal.Parcel.Label,
                pendingDealType, deal.Status, netPrice, 0m, pendingConfirmationTotal, netPrice, 0m,
                null, null, 0m, 0, 0, 0, deal.LatePenaltyPercentPerMonth,
                true, agentName, deal.TerminationReason, deal.TerminationMoneyOutcome,
                deal.RefundedAmount, deal.TerminatedAtUtc);
        }
        var confirmedPayments = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .ToList();
        var confirmedPaid = confirmedPayments.Sum(payment => payment.Amount);
        var pendingTotal = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.AwaitingAdmin)
            .Sum(payment => payment.Amount);
        var balance = Math.Max(0, netPrice - confirmedPaid);
        var percentPaid = netPrice <= 0
            ? 100m
            : Math.Clamp(Math.Round(confirmedPaid * 100m / netPrice, 2), 0m, 100m);

        var confirmedLocalDate = (deal.ConfirmedAtUtc is { } confirmed
            ? confirmed.ToLocalTime().Date
            : timeProvider.GetLocalNow().Date);
        var obligations = DealScheduleBuilder.Build(deal, confirmedLocalDate);
        var remainingConfirmed = confirmedPaid;
        var covered = obligations.Select(obligation =>
        {
            var paid = Math.Min(obligation.Amount, Math.Max(0, remainingConfirmed));
            remainingConfirmed -= paid;
            return new
            {
                Obligation = obligation,
                Paid = paid,
                Remaining = obligation.Amount - paid
            };
        }).ToList();

        var today = timeProvider.GetLocalNow().Date;
        var overdueItems = covered
            .Where(item => item.Obligation.DueDate < today && item.Remaining > 0)
            .ToList();
        var overdueAmount = overdueItems.Sum(item => item.Remaining);
        var earliestOverdue = overdueItems
            .Select(item => item.Obligation.DueDate)
            .DefaultIfEmpty(today)
            .Min();
        var daysOverdue = overdueItems.Count == 0 ? 0 : Math.Max(0, (today - earliestOverdue).Days);

        var next = covered
            .Where(item => item.Remaining > 0)
            .OrderBy(item => item.Obligation.DueDate)
            .ThenBy(item => item.Obligation.Sequence)
            .FirstOrDefault();
        var installmentItems = covered
            .Where(item => item.Obligation.Kind.StartsWith("Installment ", StringComparison.Ordinal))
            .ToList();
        DealType? dealType = deal.Parcel.Status switch
        {
            ParcelStatus.Sold => DealType.Sale,
            ParcelStatus.Leased => DealType.Lease,
            _ => null
        };

        return new DealSummaryDto(
            deal.Id,
            deal.ContactId,
            deal.ParcelId,
            deal.Parcel.Label,
            dealType,
            deal.Status,
            netPrice,
            confirmedPaid,
            pendingTotal,
            balance,
            percentPaid,
            next is null ? null : next.Obligation.DueDate,
            next?.Remaining,
            overdueAmount,
            daysOverdue,
            installmentItems.Count(item => item.Remaining == 0),
            installmentItems.Count,
            deal.LatePenaltyPercentPerMonth,
            false,
            agentName,
            deal.TerminationReason,
            deal.TerminationMoneyOutcome,
            deal.RefundedAmount,
            deal.TerminatedAtUtc);
    }

    private static DealListItem ToListItem(Deal deal)
    {
        var paidTowardReservation = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .Sum(payment => payment.Amount);
        var allocatedAmount = deal.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .Sum(payment => payment.Amount);
        var reservationFeeDue = Math.Max(0, deal.ReservationFee - paidTowardReservation);
        var remainingBalance = Math.Max(0, deal.Price - deal.Discount - allocatedAmount);
        return new DealListItem(
            deal.Id,
            deal.LeadId,
            deal.Lead.Contact.FullName,
            deal.Lead.Parcel.Label,
            deal.Lead.Stage,
            deal.Lead.CloseRequested,
            deal.Price,
            deal.Discount,
            deal.DownPayment,
            deal.ReservationFee,
            reservationFeeDue,
            remainingBalance,
            deal.Status,
            deal.ConfirmedAtUtc is not null,
            deal.IsActive,
            deal.CreatedAtUtc,
            deal.Lead.Parcel.Status,
            deal.Payments.Any(payment => payment.Status != PaymentStatus.Returned),
            deal.Payments.Any(payment => payment.Status == PaymentStatus.AwaitingAdmin));
    }

    private static PaymentListItem ToPaymentListItem(Payment payment) =>
        new(payment.Id, payment.DealId, payment.PaidAtUtc, payment.Amount, payment.Method,
            payment.ReceiptNo, payment.Status, payment.ReviewNote);

    private static decimal RemainingBalance(Deal deal) =>
        Math.Max(0, deal.Price - deal.Discount
            - deal.Payments.Where(payment => payment.Status == PaymentStatus.Confirmed).Sum(payment => payment.Amount));

    private static void SetParcelStatus(Deal deal, ParcelStatus status) =>
        deal.Parcel.Status = status;

    private string? DealAccessFailure(Deal deal)
    {
        var scope = access.GetDataReadScope("deals and payments");
        if (!scope.Ok)
            return scope.Message;
        if (scope.Value.Kind == TenantDataScopeKind.Agent && deal.AgentStaffId != scope.Value.StaffId)
            return "Agents can only access deals and payments for their own leads.";
        if (scope.Value.Kind == TenantDataScopeKind.Branch && deal.BranchId != scope.Value.BranchId)
            return "This deal is outside your branch.";
        return null;
    }
}
