using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Services.MainTransaction;

public sealed class CommissionService(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    IClock clock,
    ITenantFeatures features,
    TenantAccessPolicy access,
    IDomainEventDispatcher events)
{
    private const string RulesSettingKey = "deals.commission-rules";
    private const string CompanyRecipientRole = "Company";
    private const decimal MaximumAmount = 9999999999999999.99m;

    public async Task<ServiceResult<CommissionRulesSnapshot>> GetRulesAsync()
    {
        var scope = access.GetDataReadScope("commission rules");
        if (!scope.Ok)
            return ServiceResult<CommissionRulesSnapshot>.Fail(scope.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<CommissionRulesSnapshot>.Fail("Only a Tenant Admin can view commission rules.");

        await using var uow = uowFactory.Create();
        if (!TryReadRules(await uow.Settings.GetForDisplayAsync(RulesSettingKey), out var snapshot))
            return ServiceResult<CommissionRulesSnapshot>.Fail("Commission rule history could not be read. No changes were made.");

        return ServiceResult<CommissionRulesSnapshot>.Success(snapshot);
    }

    public async Task<ServiceResult<CommissionRulesSnapshot>> SaveRulesAsync(CommissionRulesRequest request)
    {
        var permission = access.RequireDataMutation("change commission rules");
        if (!permission.Ok)
            return ServiceResult<CommissionRulesSnapshot>.Fail(permission.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<CommissionRulesSnapshot>.Fail("Only a Tenant Admin can change commission rules.");

        var validation = ValidateRules(request);
        if (validation is not null)
            return ServiceResult<CommissionRulesSnapshot>.Fail(validation);

        await using var uow = uowFactory.Create();
        var setting = await uow.Settings.GetByKeyAsync(RulesSettingKey);
        if (!TryReadRules(setting, out var current))
            return ServiceResult<CommissionRulesSnapshot>.Fail("Commission rule history could not be read. No changes were made.");

        var version = new CommissionRulesVersion(
            (current.Current?.Version ?? 0) + 1,
            clock.UtcNow,
            user.UserName,
            request.CommissionType,
            request.CommissionAmount,
            request.AgentSharePercent,
            request.CompanySharePercent,
            request.BranchManagerSharePercent,
            request.ReferralType,
            request.ReferralAmount,
            request.PayableTiming);
        var history = current.History.Append(version).ToList();
        var value = JsonSerializer.Serialize(new RulesHistoryData(history));

        if (setting is null)
        {
            uow.Settings.Add(new Setting { Key = RulesSettingKey, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await uow.SaveAsync();
        return ServiceResult<CommissionRulesSnapshot>.Success(
            new CommissionRulesSnapshot(version, history),
            $"Commission rules saved as version {version.Version}.");
    }

    public async Task<ServiceResult<IReadOnlyList<PayoutListItem>>> ListAsync()
    {
        var scope = access.GetDataReadScope("commissions and referral bonuses");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<PayoutListItem>>.Fail(scope.Message);
        var staffId = scope.Value.Kind == TenantDataScopeKind.Agent
            ? scope.Value.StaffId
            : scope.Value.Kind == TenantDataScopeKind.Branch ? access.CurrentStaffId : null;
        var branchId = scope.Value.Kind == TenantDataScopeKind.Branch ? scope.Value.BranchId : null;
        var includeCompanyShares = scope.Value.Kind == TenantDataScopeKind.Tenant;

        if (user.Role == Roles.TenantAdmin)
        {
            var backfill = await EnsureMissingFullyPaidAsync();
            if (!backfill.Ok)
                return ServiceResult<IReadOnlyList<PayoutListItem>>.Fail(backfill.Message);
        }

        await using var uow = uowFactory.Create();
        var payableTiming = await GetPayableTimingAsync(uow);
        var commissions = await uow.Commissions.ListAsync(staffId, branchId, includeCompanyShares);
        var bonuses = await uow.ReferralBonuses.ListAsync(staffId, branchId);
        var staffIds = commissions
            .Where(item => item.RecipientStaffId is not null)
            .Select(item => item.RecipientStaffId!.Value)
            .Distinct()
            .ToArray();
        var staff = await uow.Staff.ListByIdsAsync(staffIds);
        var staffNames = staff.ToDictionary(member => member.Id, member => member.FullName);

        var items = commissions.Select(item =>
        {
            var recipientName = item.RecipientRole == CompanyRecipientRole
                ? "Company"
                : item.RecipientStaffId is { } recipientId && staffNames.TryGetValue(recipientId, out var name)
                    ? name
                    : "Unassigned";
            return ToPayoutItem(
                item.Id, item.DealId, PayoutKind.Commission,
                item.Deal.Lead.Contact.FullName, item.Deal.Lead.Parcel.Label,
                recipientName, item.RecipientRole, item.Amount, item.Status,
                item.CreatedAtUtc, item.PaidAtUtc, item.BranchId, item.Deal, payableTiming);
        }).Concat(bonuses.Select(item =>
            ToPayoutItem(
                item.Id, item.DealId, PayoutKind.ReferralBonus,
                item.Deal.Lead.Contact.FullName, item.Deal.Lead.Parcel.Label,
                item.ReferrerName, "Referral", item.Amount, item.Status,
                item.CreatedAtUtc, item.PaidAtUtc, item.BranchId, item.Deal, payableTiming)))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .ToList();

        return ServiceResult<IReadOnlyList<PayoutListItem>>.Success(items);
    }

    public Task<ServiceResult<bool>> ApproveAsync(PayoutKind kind, Guid payoutId) =>
        ChangeStatusAsync(kind, payoutId, markPaid: false);

    public Task<ServiceResult<bool>> MarkPaidAsync(PayoutKind kind, Guid payoutId) =>
        ChangeStatusAsync(kind, payoutId, markPaid: true);

    internal async Task<ServiceResult<bool>> AddForConfirmedDealAsync(ITenantUnitOfWork uow, Deal deal)
    {
        var commissionExists = await uow.Commissions.ExistsForDealAsync(deal.Id);
        var hasReferrer = HasReferrer(deal.Lead.Contact);
        var bonusExists = !hasReferrer || await uow.ReferralBonuses.ExistsForDealAsync(deal.Id);
        if (commissionExists && bonusExists)
            return ServiceResult<bool>.Success(true);

        if (!TryReadRules(await uow.Settings.GetForDisplayAsync(RulesSettingKey), out var rules)
            || rules.Current is not { } current)
            return ServiceResult<bool>.Fail("Set valid commission and referral rules before confirming Closed.");

        return await AddMissingPayoutsAsync(uow, deal, current, commissionExists, bonusExists);
    }

    internal async Task<IReadOnlyList<PayoutChanged>> HandleDealEndedAsync(
        ITenantUnitOfWork uow, Guid dealId, DateTime occurredAtUtc)
    {
        var changed = new List<PayoutChanged>();
        foreach (var commission in await uow.Commissions.ListByDealAsync(dealId))
        {
            if (UpdateEndedStatus(commission))
                changed.Add(new PayoutChanged(dealId, commission.Id, PayoutKind.Commission, occurredAtUtc));
        }
        foreach (var bonus in await uow.ReferralBonuses.ListByDealAsync(dealId))
        {
            if (UpdateEndedStatus(bonus))
                changed.Add(new PayoutChanged(dealId, bonus.Id, PayoutKind.ReferralBonus, occurredAtUtc));
        }
        return changed;
    }

    private async Task<ServiceResult<bool>> EnsureMissingFullyPaidAsync()
    {
        await using var uow = uowFactory.Create();
        var deals = await uow.Deals.ListFullyPaidMissingCommissionAsync();
        if (deals.Count == 0)
            return ServiceResult<bool>.Success(true);

        if (!TryReadRules(await uow.Settings.GetForDisplayAsync(RulesSettingKey), out var rules)
            || rules.Current is not { } current)
            return ServiceResult<bool>.Fail("Configure commission rules before generating payouts for fully paid deals.");

        foreach (var deal in deals)
        {
            var result = await AddMissingPayoutsAsync(uow, deal, current,
                commissionExists: false,
                bonusExists: await uow.ReferralBonuses.ExistsForDealAsync(deal.Id));
            if (!result.Ok)
                return result;
        }

        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("Payout generation was interrupted by a concurrent change. Reload and try again.");
        }
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<bool>> AddMissingPayoutsAsync(
        ITenantUnitOfWork uow,
        Deal deal,
        CommissionRulesVersion rules,
        bool commissionExists,
        bool bonusExists)
    {
        Staff? branchManager = null;
        if (!commissionExists && rules.BranchManagerSharePercent > 0)
        {
            if (!features.Branching)
                return ServiceResult<bool>.Fail("Branch Manager commission shares are available only in Tier C.");
            if (deal.BranchId is not { } branchId)
                return ServiceResult<bool>.Fail("This deal has no branch for its configured Branch Manager share.");

            var managers = await uow.Staff.ListActiveBranchManagersAsync(branchId);
            if (managers.Count != 1)
                return ServiceResult<bool>.Fail(
                    managers.Count == 0
                        ? "Assign an active Branch Manager to this deal's branch before confirming Closed."
                        : "More than one active Branch Manager is assigned to this branch; ask your Tenant Admin to resolve it.");
            branchManager = managers[0];
        }

        if (!commissionExists)
        {
            var netPrice = deal.Price - deal.Discount;
            var total = CalculateAmount(netPrice, rules.CommissionType, rules.CommissionAmount);
            var agentAmount = decimal.Round(total * rules.AgentSharePercent / 100m, 2, MidpointRounding.AwayFromZero);
            var managerAmount = decimal.Round(total * rules.BranchManagerSharePercent / 100m, 2, MidpointRounding.AwayFromZero);
            if (agentAmount + managerAmount > total)
                managerAmount = total - agentAmount;
            var companyAmount = total - agentAmount - managerAmount;

            if (agentAmount > 0)
                uow.Commissions.Add(NewCommission(deal, deal.AgentStaffId, Roles.Agent, rules, agentAmount));
            if (managerAmount > 0 && branchManager is not null)
                uow.Commissions.Add(NewCommission(deal, branchManager.Id, Roles.BranchManager, rules, managerAmount));
            if (companyAmount > 0)
                uow.Commissions.Add(NewCommission(deal, null, CompanyRecipientRole, rules, companyAmount));
        }

        var contact = deal.Lead.Contact;
        if (!bonusExists && HasReferrer(contact))
        {
            var referrer = await ResolveReferrerAsync(uow, contact);
            if (referrer.Error is not null)
                return ServiceResult<bool>.Fail(referrer.Error);
            var amount = CalculateAmount(deal.Price - deal.Discount, rules.ReferralType, rules.ReferralAmount);
            if (amount <= 0)
                return ServiceResult<bool>.Fail("The referral rule produces no payable amount for this deal.");

            uow.ReferralBonuses.Add(new ReferralBonus
            {
                DealId = deal.Id,
                ReferrerContactId = contact.ReferrerContactId,
                ReferrerStaffId = contact.ReferrerStaffId,
                ReferrerName = referrer.Name!,
                Amount = amount,
                Status = CommissionStatus.Pending,
                BranchId = deal.BranchId
            });
        }

        return ServiceResult<bool>.Success(true);
    }

    private async Task<(string? Name, string? Error)> ResolveReferrerAsync(
        ITenantUnitOfWork uow, Contact contact)
    {
        if (contact.ReferrerContactId is { } contactId)
        {
            var referrer = await uow.Contacts.GetAsync(contactId);
            return referrer is null
                ? (null, "The lead's referrer contact is no longer available; correct the contact before confirming Closed.")
                : (referrer.FullName, null);
        }

        if (contact.ReferrerStaffId is { } staffId)
        {
            var staff = await uow.Staff.ListByIdsAsync([staffId]);
            var referrer = staff.SingleOrDefault(member => member.Id == staffId && member.IsActive);
            return referrer is null
                ? (null, "The lead's referrer staff member is inactive or unavailable; correct the contact before confirming Closed.")
                : (referrer.FullName, null);
        }

        return string.IsNullOrWhiteSpace(contact.ReferrerName)
            ? (null, "The lead's referrer has no name; correct the contact before confirming Closed.")
            : (contact.ReferrerName.Trim(), null);
    }

    private async Task<ServiceResult<bool>> ChangeStatusAsync(PayoutKind kind, Guid payoutId, bool markPaid)
    {
        var permission = access.RequireDataMutation(markPaid ? "pay a commission or referral bonus" : "approve a commission or referral bonus");
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<bool>.Fail("Only a Tenant Admin can approve or pay commissions and referral bonuses.");
        if (!Enum.IsDefined(kind))
            return ServiceResult<bool>.Fail("Select a valid payout type.");

        await using var uow = uowFactory.Create();
        var payableTiming = markPaid
            ? await GetPayableTimingAsync(uow)
            : CommissionPayableTiming.AfterDownPayment;
        Guid dealId;
        var now = clock.UtcNow;

        if (kind == PayoutKind.Commission)
        {
            var commission = await uow.Commissions.GetAsync(payoutId);
            if (commission is null)
                return ServiceResult<bool>.Fail("That commission was not found.");
            dealId = commission.DealId;
            var expected = markPaid ? CommissionStatus.Approved : CommissionStatus.Pending;
            if (commission.Status != expected)
                return ServiceResult<bool>.Fail(markPaid
                    ? "Only an approved commission can be marked Paid."
                    : "Only a pending commission can be approved.");
            if (markPaid && PaidBlockedReason(commission.Deal, payableTiming) is { } reason)
                return ServiceResult<bool>.Fail(reason);

            commission.Status = markPaid ? CommissionStatus.Paid : CommissionStatus.Approved;
            if (markPaid) commission.PaidAtUtc = now;
        }
        else
        {
            var bonus = await uow.ReferralBonuses.GetAsync(payoutId);
            if (bonus is null)
                return ServiceResult<bool>.Fail("That referral bonus was not found.");
            dealId = bonus.DealId;
            var expected = markPaid ? CommissionStatus.Approved : CommissionStatus.Pending;
            if (bonus.Status != expected)
                return ServiceResult<bool>.Fail(markPaid
                    ? "Only an approved referral bonus can be marked Paid."
                    : "Only a pending referral bonus can be approved.");
            if (markPaid && PaidBlockedReason(bonus.Deal, payableTiming) is { } reason)
                return ServiceResult<bool>.Fail(reason);

            bonus.Status = markPaid ? CommissionStatus.Paid : CommissionStatus.Approved;
            if (markPaid) bonus.PaidAtUtc = now;
        }

        try
        {
            await uow.SaveAsync();
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The payout was not changed because it changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The payout was not changed because the parcel changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new PayoutChanged(dealId, payoutId, kind, now));
        return ServiceResult<bool>.Success(true, markPaid ? "Payout marked Paid." : "Payout approved.");
    }

    private static PayoutListItem ToPayoutItem(
        Guid id,
        Guid dealId,
        PayoutKind kind,
        string contactName,
        string parcelLabel,
        string recipientName,
        string recipientRole,
        decimal amount,
        CommissionStatus status,
        DateTime createdAtUtc,
        DateTime? paidAtUtc,
        Guid? branchId,
        Deal deal,
        CommissionPayableTiming payableTiming)
    {
        var paidBlockedReason = PaidBlockedReason(deal, payableTiming);
        return new PayoutListItem(
            id, dealId, kind, contactName, parcelLabel, recipientName, recipientRole,
            amount, status, createdAtUtc, paidAtUtc,
            status == CommissionStatus.Pending,
            null,
            status == CommissionStatus.Approved && paidBlockedReason is null,
            paidBlockedReason,
            branchId);
    }

    private static string? PaidBlockedReason(Deal deal, CommissionPayableTiming payableTiming)
    {
        if (deal.Status is DealStatus.Cancelled or DealStatus.Defaulted)
            return "This deal is cancelled or defaulted.";
        if (deal.ConfirmedAtUtc is null)
            return "The deal must be confirmed before its payout can be Paid.";
        if (payableTiming == CommissionPayableTiming.AfterDownPayment && deal.DownPayment > 0)
        {
            var confirmed = deal.Payments
                .Where(payment => payment.Status == PaymentStatus.Confirmed)
                .Sum(payment => payment.Amount);
            var confirmedTowardDownPayment = Math.Max(0m, confirmed - deal.ReservationFee);
            if (confirmedTowardDownPayment < deal.DownPayment)
                return "Wait until confirmed payments reach the deal's down payment.";
        }

        return null;
    }

    private async Task<CommissionPayableTiming> GetPayableTimingAsync(ITenantUnitOfWork uow) =>
        ReadPayableTiming(await uow.Settings.GetForDisplayAsync(RulesSettingKey));

    private static CommissionPayableTiming ReadPayableTiming(Setting? setting)
    {
        const CommissionPayableTiming defaultTiming = CommissionPayableTiming.AfterDownPayment;
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
            return defaultTiming;

        try
        {
            using var document = JsonDocument.Parse(setting.Value);
            if (!document.RootElement.TryGetProperty("History", out var history)
                || history.ValueKind != JsonValueKind.Array
                || history.GetArrayLength() == 0)
                return defaultTiming;

            var latest = history.EnumerateArray().Last();
            if (!latest.TryGetProperty(nameof(CommissionRulesVersion.PayableTiming), out var value))
                return defaultTiming;
            if (value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var numeric)
                && Enum.IsDefined(typeof(CommissionPayableTiming), numeric))
                return (CommissionPayableTiming)numeric;
            if (value.ValueKind == JsonValueKind.String
                && Enum.TryParse<CommissionPayableTiming>(value.GetString(), out var parsed)
                && Enum.IsDefined(parsed))
                return parsed;
        }
        catch (JsonException)
        {
            return defaultTiming;
        }

        return defaultTiming;
    }

    private static Commission NewCommission(
        Deal deal, Guid? recipientStaffId, string recipientRole,
        CommissionRulesVersion rules, decimal amount) =>
        new()
        {
            DealId = deal.Id,
            RecipientStaffId = recipientStaffId,
            RecipientRole = recipientRole,
            Rate = rules.CommissionType == CommissionCalculationType.Percent ? rules.CommissionAmount : null,
            Amount = amount,
            Status = CommissionStatus.Pending,
            BranchId = deal.BranchId
        };

    private static decimal CalculateAmount(
        decimal dealPrice, CommissionCalculationType type, decimal amount) =>
        type == CommissionCalculationType.Percent
            ? decimal.Round(dealPrice * amount / 100m, 2, MidpointRounding.AwayFromZero)
            : amount;

    private static bool HasReferrer(Contact contact) =>
        contact.ReferrerContactId is not null || contact.ReferrerStaffId is not null
        || !string.IsNullOrWhiteSpace(contact.ReferrerName)
        || !string.IsNullOrWhiteSpace(contact.ReferrerPhone);

    private static bool UpdateEndedStatus(Commission commission)
    {
        if (commission.Status is CommissionStatus.Pending or CommissionStatus.Approved)
        {
            commission.Status = CommissionStatus.Cancelled;
            return true;
        }
        else if (commission.Status == CommissionStatus.Paid)
        {
            commission.Status = CommissionStatus.PaidNeedsReview;
            return true;
        }
        return false;
    }

    private static bool UpdateEndedStatus(ReferralBonus bonus)
    {
        if (bonus.Status is CommissionStatus.Pending or CommissionStatus.Approved)
        {
            bonus.Status = CommissionStatus.Cancelled;
            return true;
        }
        else if (bonus.Status == CommissionStatus.Paid)
        {
            bonus.Status = CommissionStatus.PaidNeedsReview;
            return true;
        }
        return false;
    }

    private string? ValidateRules(CommissionRulesRequest request)
    {
        if (!Enum.IsDefined(request.CommissionType) || !Enum.IsDefined(request.ReferralType))
            return "Choose a valid commission and referral calculation type.";
        if (!Enum.IsDefined(request.PayableTiming))
            return "Choose a valid payout timing.";
        if (!ValidRuleAmount(request.CommissionType, request.CommissionAmount))
            return "The commission amount must be greater than zero and within the supported range.";
        if (!ValidRuleAmount(request.ReferralType, request.ReferralAmount))
            return "The referral bonus amount must be greater than zero and within the supported range.";
        if (!ValidShare(request.AgentSharePercent) || !ValidShare(request.CompanySharePercent)
            || !ValidShare(request.BranchManagerSharePercent))
            return "Each recipient share must be between 0 and 100 with at most two decimal places.";
        if (!features.Branching && request.BranchManagerSharePercent != 0)
            return "Branch Manager shares can be configured only for Tier C.";
        if (request.AgentSharePercent + request.CompanySharePercent + request.BranchManagerSharePercent != 100m)
            return "Agent, company, and Branch Manager shares must total exactly 100%.";

        return null;
    }

    private static bool ValidRuleAmount(CommissionCalculationType type, decimal amount) =>
        amount > 0
        && (type == CommissionCalculationType.Percent ? amount <= 100m : amount <= MaximumAmount)
        && decimal.Round(amount, 2) == amount;

    private static bool ValidShare(decimal value) =>
        value >= 0 && value <= 100m && decimal.Round(value, 2) == value;

    private static bool TryReadRules(Setting? setting, out CommissionRulesSnapshot snapshot)
    {
        if (setting is null)
        {
            snapshot = new CommissionRulesSnapshot(null, Array.Empty<CommissionRulesVersion>());
            return true;
        }

        try
        {
            var data = JsonSerializer.Deserialize<RulesHistoryData>(setting.Value);
            if (data?.History is null)
            {
                snapshot = new CommissionRulesSnapshot(null, Array.Empty<CommissionRulesVersion>());
                return false;
            }

            var history = data.History.OrderBy(version => version.Version).ToList();
            snapshot = new CommissionRulesSnapshot(history.LastOrDefault(), history);
            return true;
        }
        catch (JsonException)
        {
            snapshot = new CommissionRulesSnapshot(null, Array.Empty<CommissionRulesVersion>());
            return false;
        }
    }

    private sealed record RulesHistoryData(List<CommissionRulesVersion> History);
}
