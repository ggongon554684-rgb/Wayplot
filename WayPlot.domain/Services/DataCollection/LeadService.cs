using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.MainTransaction;
using LeadResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.LeadDto>;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Attaching contacts to parcels as leads, and moving leads through the pipeline.
/// The audit fields on Lead are stamped by TenantDbContext; this service writes StageHistory and raises events.</summary>
public sealed class LeadService(
    ITenantUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher events,
    ICurrentUser user,
    IClock clock,
    TenantAccessPolicy access,
    IPipelineStageProvider pipelineStages,
    TimeProvider timeProvider,
    ITenantFeatures features)
{
    public async Task<LeadResult> AttachContactToParcelAsync(AttachLeadRequest request)
    {
        var permission = access.RequireDataMutation("attach contacts to parcels as leads");
        if (!permission.Ok)
            return LeadResult.Fail(permission.Message);
        if (access.IsAgent && request.AssignedStaffId is { } assignedStaffId && assignedStaffId != access.CurrentStaffId)
            return LeadResult.Fail("Agents can only attach leads assigned to themselves.");

        await pipelineStages.EnsureLoadedAsync();
        var scope = access.GetDataReadScope("contacts and leads");
        if (!scope.Ok)
            return LeadResult.Fail(scope.Message);

        await using var uow = uowFactory.Create();

        var contact = await uow.Contacts.GetAsync(request.ContactId, scope.Value);
        if (contact is null)
            return LeadResult.Fail("Cannot attach this contact: it was not found or is outside your access scope.");

        var parcel = await uow.Parcels.GetAsync(request.ParcelId);
        if (parcel is null)
            return LeadResult.Fail("That parcel was not found.");

        if (request.AssignedStaffId is { } requestedStaffId
            && requestedStaffId != user.UserId
            && !await uow.Staff.IsActiveAgentAsync(requestedStaffId))
            return LeadResult.Fail("The assigned agent was not found or is inactive.");

        if (await uow.Leads.ActiveLeadExistsAsync(contact.Id, parcel.Id))
            return LeadResult.Fail($"{contact.FullName} already has an active lead on parcel {parcel.Label}.");

        var now = clock.UtcNow;
        var lead = new Lead
        {
            ContactId = contact.Id,
            Contact = contact,
            ParcelId = parcel.Id,
            Parcel = parcel,
            Stage = LeadStage.New,
            LastActivityAtUtc = now,
            AssignedStaffId = access.IsAgent ? access.CurrentStaffId : request.AssignedStaffId ?? user.UserId
        };

        uow.Leads.Add(lead);
        // The first row of the lead's history, so funnel reports can start from New.
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = lead.Id,
            FromStage = null,
            ToStage = LeadStage.New,
            ChangedAtUtc = now,
            ChangedByStaffId = user.UserId,
            Note = "Lead created"
        });

        await uow.SaveAsync();
        await events.DispatchAsync(new LeadCaptured(lead.Id, now));

        return LeadResult.Success(ToDto(lead), $"{contact.FullName} is now a lead on parcel {parcel.Label}.");
    }

    public async Task<ServiceResult<IReadOnlyList<LeadDto>>> ListByParcelAsync(Guid parcelId)
    {
        var scope = access.GetDataReadScope("leads");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<LeadDto>>.Fail(scope.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        if (await uow.Parcels.GetAsync(parcelId) is null)
            return ServiceResult<IReadOnlyList<LeadDto>>.Fail("That parcel was not found or has been archived.");

        return ServiceResult<IReadOnlyList<LeadDto>>.Success(
            (await uow.Leads.ListByParcelAsync(parcelId, scope.Value)).Select(ToDto).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<ContactLeadCardDto>>> ListByContactAsync(Guid contactId)
    {
        var scope = access.GetDataReadScope("leads");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<ContactLeadCardDto>>.Fail(scope.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var leads = await uow.Leads.ListByContactAsync(contactId, scope.Value);
        var staffIds = leads.Where(lead => lead.AssignedStaffId is not null)
            .Select(lead => lead.AssignedStaffId!.Value).Distinct().ToArray();
        var staff = await uow.Staff.ListByIdsAsync(staffIds);
        var names = staff.ToDictionary(member => member.Id, member => member.FullName);

        var cards = leads.Select(lead => new ContactLeadCardDto(
            lead.Id,
            lead.ParcelId,
            lead.Parcel.Label,
            lead.Stage,
            pipelineStages.GetDisplayName(lead.Stage),
            lead.AssignedStaffId is { } staffId && names.TryGetValue(staffId, out var name)
                ? name : "Unassigned",
            lead.LastActivityAtUtc)).ToList();
        return ServiceResult<IReadOnlyList<ContactLeadCardDto>>.Success(cards);
    }

    public async Task<ServiceResult<IReadOnlyList<LeadListItem>>> ListAsync(LeadStage? stage, string? search)
    {
        var scope = access.GetDataReadScope("leads");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<LeadListItem>>.Fail(scope.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var leads = await uow.Leads.ListAsync(stage, search, scope: scope.Value);
        IReadOnlyList<LeadListItem> items = leads
            .Select(l => new LeadListItem(
                l.Id, l.Contact.FullName, l.Parcel.Label, l.Stage,
                pipelineStages.GetDisplayName(l.Stage), l.OfferAmount, l.LastActivityAtUtc))
            .ToList();
        return ServiceResult<IReadOnlyList<LeadListItem>>.Success(items);
    }

    public async Task<ServiceResult<LeadFileDto>> GetFileAsync(Guid leadId)
    {
        var scope = access.GetDataReadScope("lead files");
        if (!scope.Ok)
            return ServiceResult<LeadFileDto>.Fail(scope.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetForDisplayAsync(leadId, scope.Value);
        if (lead is null)
            return ServiceResult<LeadFileDto>.Fail("That lead was not found or is outside your access scope.");

        var history = await uow.StageHistories.ListByLeadAsync(leadId);
        var interactions = await uow.Interactions.ListByLeadAsync(leadId, scope.Value);
        return ServiceResult<LeadFileDto>.Success(new LeadFileDto(
            lead.Id,
            lead.Contact.FullName,
            lead.Contact.Phone,
            lead.Contact.Email,
            lead.Parcel.Label,
            lead.Stage,
            pipelineStages.GetDisplayName(lead.Stage),
            lead.OfferAmount,
            pipelineStages.GetStages(),
            history.Select(h => new LeadStageHistoryRow(
                h.FromStage,
                h.ToStage,
                h.ChangedAtUtc,
                h.Note,
                h.FromStage is { } fromStage ? pipelineStages.GetDisplayName(fromStage) : null,
                pipelineStages.GetDisplayName(h.ToStage))).ToList(),
            interactions.Select(i => new LeadInteractionRow(i.Type, i.Channel, i.OccurredAtUtc, i.Note)).ToList()));
    }

    public async Task<ServiceResult<Guid>> AddLeadForContactAsync(
        Guid contactId, Guid parcelId, string? note = null, Guid? assignToStaffId = null)
    {
        var permission = access.RequireDataMutation("add a lead for a contact");
        if (!permission.Ok)
            return ServiceResult<Guid>.Fail(permission.Message);
        var scope = access.GetDataReadScope("contacts and leads");
        if (!scope.Ok)
            return ServiceResult<Guid>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var contact = await uow.Contacts.GetAsync(contactId, scope.Value);
        if (contact is null)
            return ServiceResult<Guid>.Fail("Cannot add this lead: the contact was not found or is outside your access scope.");
        var parcel = await uow.Parcels.GetAsync(parcelId);
        if (parcel is null)
            return ServiceResult<Guid>.Fail("That parcel was not found.");
        if (parcel.Status != ParcelStatus.Available)
            return ServiceResult<Guid>.Fail($"Cannot add a lead: parcel {parcel.Label} is {parcel.Status}, not Available.");

        var existing = await uow.Leads.ListByContactAsync(contactId, scope.Value);
        foreach (var existingLead in existing.Where(item => item.ParcelId == parcelId))
        {
            if (existingLead.Stage != LeadStage.Lost)
            {
                if (existingLead.Stage != LeadStage.Closed)
                    return ServiceResult<Guid>.Fail($"{contact.FullName} already has an open lead on parcel {parcel.Label}.");
                var deals = await uow.Deals.ListByLeadAsync(existingLead.Id);
                if (!deals.Any(deal => deal.Status is DealStatus.Cancelled or DealStatus.Defaulted))
                    return ServiceResult<Guid>.Fail($"{contact.FullName} already has an open lead on parcel {parcel.Label}.");
            }
        }

        Guid assignedStaffId;
        if (access.IsAgent)
            assignedStaffId = access.CurrentStaffId!.Value;
        else
        {
            if (assignToStaffId is { } selected)
                assignedStaffId = selected;
            else if (contact.AssignedStaffId is { } contactAgent)
                assignedStaffId = contactAgent;
            else
                return ServiceResult<Guid>.Fail("Assign this lead to an agent.");
        }
        if (!await uow.Staff.IsActiveAgentAsync(assignedStaffId))
            return ServiceResult<Guid>.Fail("The assigned agent was not found or is inactive.");

        var now = clock.UtcNow;
        var lead = new Lead
        {
            ContactId = contact.Id,
            Contact = contact,
            ParcelId = parcel.Id,
            Parcel = parcel,
            Stage = LeadStage.New,
            AssignedStaffId = assignedStaffId,
            LastActivityAtUtc = now
        };
        uow.Leads.Add(lead);
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = lead.Id,
            FromStage = null,
            ToStage = LeadStage.New,
            ChangedAtUtc = now,
            ChangedByStaffId = user.UserId,
            Note = string.IsNullOrWhiteSpace(note) ? "Lead created" : note.Trim()
        });

        await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        await events.DispatchAsync(new LeadCaptured(lead.Id, now));
        return ServiceResult<Guid>.Success(lead.Id, $"Lead created on parcel {parcel.Label}.");
    }

    public async Task<ServiceResult<Guid>> SwitchLotAsync(Guid leadId, Guid newParcelId, string? reason = null)
    {
        var permission = access.RequireDataMutation("switch a lead to another lot");
        if (!permission.Ok)
            return ServiceResult<Guid>.Fail(permission.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var oldLead = await uow.Leads.GetAsync(leadId);
        if (oldLead is null)
            return ServiceResult<Guid>.Fail("That lead was not found.");
        var leadPermission = access.RequireLeadMutation(oldLead, "switch this lead to another lot");
        if (!leadPermission.Ok)
            return ServiceResult<Guid>.Fail(leadPermission.Message);
        if (oldLead.Stage is LeadStage.Closed or LeadStage.Lost)
            return ServiceResult<Guid>.Fail(
                $"Cannot switch lot: this lead is already {pipelineStages.GetDisplayName(oldLead.Stage)}.");
        if (oldLead.ParcelId == newParcelId)
            return ServiceResult<Guid>.Fail("Choose a different parcel.");

        var parcel = await uow.Parcels.GetAsync(newParcelId);
        if (parcel is null)
            return ServiceResult<Guid>.Fail("That parcel was not found.");
        if (parcel.Status != ParcelStatus.Available)
            return ServiceResult<Guid>.Fail($"Cannot switch lots: parcel {parcel.Label} is {parcel.Status}, not Available.");
        var assignedStaffId = oldLead.AssignedStaffId;
        if (assignedStaffId is null)
            return ServiceResult<Guid>.Fail("Assign this lead to an agent before switching lots.");

        var existing = await uow.Leads.ListByContactAsync(oldLead.ContactId);
        foreach (var existingLead in existing.Where(lead => lead.ParcelId == newParcelId))
        {
            if (existingLead.Stage != LeadStage.Lost)
            {
                if (existingLead.Stage != LeadStage.Closed)
                    return ServiceResult<Guid>.Fail($"{oldLead.Contact.FullName} already has a lead on parcel {parcel.Label}.");
                var deals = await uow.Deals.ListByLeadAsync(existingLead.Id);
                if (!deals.Any(deal => deal.Status is DealStatus.Cancelled or DealStatus.Defaulted))
                    return ServiceResult<Guid>.Fail($"{oldLead.Contact.FullName} already has a lead on parcel {parcel.Label}.");
            }
        }

        var now = clock.UtcNow;
        var newLead = new Lead
        {
            ContactId = oldLead.ContactId,
            Contact = oldLead.Contact,
            ParcelId = parcel.Id,
            Parcel = parcel,
            Stage = LeadStage.New,
            AssignedStaffId = assignedStaffId,
            LastActivityAtUtc = now
        };
        uow.Leads.Add(newLead);
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = newLead.Id,
            FromStage = null,
            ToStage = LeadStage.New,
            ChangedAtUtc = now,
            ChangedByStaffId = user.UserId,
            Note = "Lead created"
        });

        var from = oldLead.Stage;
        var lostNote = $"Switched to lot {parcel.Label}" +
            (string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason.Trim()}");
        oldLead.Stage = LeadStage.Lost;
        oldLead.LostReason = lostNote;
        oldLead.LastActivityAtUtc = now;
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = oldLead.Id,
            FromStage = from,
            ToStage = LeadStage.Lost,
            ChangedAtUtc = now,
            ChangedByStaffId = user.UserId,
            Note = lostNote
        });

        await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        await events.DispatchAsync(new LeadCaptured(newLead.Id, now));
        await events.DispatchAsync(new LeadStageChanged(oldLead.Id, from, LeadStage.Lost, now));
        return ServiceResult<Guid>.Success(newLead.Id, $"Lead switched to parcel {parcel.Label}. The old lead was lost.");
    }

    public async Task<ServiceResult<PipelineBoardDto>> GetPipelineBoardAsync(PipelineFilter filter)
    {
        var scope = access.GetDataReadScope("pipeline");
        if (!scope.Ok)
            return ServiceResult<PipelineBoardDto>.Fail(scope.Message);

        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var assignedId = scope.Value.Kind == TenantDataScopeKind.Agent
            ? scope.Value.StaffId
            : user.Role == Roles.TenantAdmin ? filter.AssignedStaffId : null;
        var leads = await uow.Leads.ListAsync(null, filter.SearchText, assignedId, scope.Value);
        if (filter.ParcelId is { } parcelId)
            leads = leads.Where(lead => lead.ParcelId == parcelId).ToList();

        var cutoff = timeProvider.GetLocalNow().UtcDateTime.AddDays(-30);
        if (!filter.IncludeAllClosedAndLost)
            leads = leads.Where(lead => lead.Stage is not LeadStage.Closed and not LeadStage.Lost
                || lead.LastActivityAtUtc >= cutoff).ToList();

        var staffIds = leads.Where(lead => lead.AssignedStaffId is not null)
            .Select(lead => lead.AssignedStaffId!.Value).Distinct().ToArray();
        var staff = await uow.Staff.ListByIdsAsync(staffIds);
        var staffNames = staff.ToDictionary(item => item.Id, item => item.FullName);
        var columns = new List<PipelineColumnDto>();
        var configuration = pipelineStages.GetConfiguration();
        var activeStages = configuration.Where(stage => stage.IsActive)
            .Select(stage => stage.Stage).ToHashSet();
        var inactiveStagesWithLeads = leads
            .Where(lead => lead.Stage is not (LeadStage.Closed or LeadStage.Lost)
                && !activeStages.Contains(lead.Stage))
            .Select(lead => lead.Stage)
            .ToHashSet();
        foreach (var stageInfo in PipelineStageDefinition.GetStagesForBoard(configuration, inactiveStagesWithLeads))
        {
            if (!Enum.TryParse<LeadStage>(stageInfo.Key, out var stage))
                continue;
            var inStage = leads.Where(lead => lead.Stage == stage)
                .OrderByDescending(lead => lead.LastActivityAtUtc).Take(100).ToList();
            var cards = new List<LeadCardDto>();
            foreach (var lead in inStage)
            {
                var history = await uow.StageHistories.ListByLeadAsync(lead.Id);
                var lastChange = history.Where(item => item.FromStage != item.ToStage && item.ToStage == stage)
                    .OrderByDescending(item => item.ChangedAtUtc).FirstOrDefault();
                var days = Math.Max(0, (timeProvider.GetLocalNow().UtcDateTime - (lastChange?.ChangedAtUtc ?? lead.LastActivityAtUtc)).Days);
                var deals = await uow.Deals.ListByLeadAsync(lead.Id);
                var latestDeal = deals.FirstOrDefault();
                cards.Add(new LeadCardDto(
                    lead.Id, lead.ContactId, lead.Contact.FullName, lead.ParcelId, lead.Parcel.Label,
                    lead.AssignedStaffId is { } id && staffNames.TryGetValue(id, out var name) ? name : null,
                    lead.Stage, pipelineStages.GetDisplayName(lead.Stage), days,
                    lead.LastActivityAtUtc, lead.CloseRequested,
                    latestDeal?.Status));
            }
            columns.Add(new PipelineColumnDto(stageInfo.Key, stageInfo.Name,
                leads.Count(lead => lead.Stage == stage), cards));
        }
        return ServiceResult<PipelineBoardDto>.Success(new PipelineBoardDto(columns));
    }

    public async Task<ServiceResult<IReadOnlyList<LeadMoveOptionDto>>> GetAllowedMovesAsync(Guid leadId)
    {
        var scope = access.GetDataReadScope("pipeline moves");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<LeadMoveOptionDto>>.Fail(scope.Message);
        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(leadId, scope.Value);
        if (lead is null)
            return ServiceResult<IReadOnlyList<LeadMoveOptionDto>>.Fail("That lead was not found or is outside your access scope.");

        var list = new List<LeadMoveOptionDto>();
        foreach (var stageInfo in pipelineStages.GetStages())
        {
            if (!Enum.TryParse<LeadStage>(stageInfo.Key, out var target))
                continue;
            var anotherHoldsParcel = target == LeadStage.Negotiation
                && await uow.Leads.AnotherLeadInNegotiationAsync(lead.ParcelId, lead.Id);
            var nextStage = PipelineRules.Next(lead.Stage, pipelineStages.GetConfiguration());
            var closingStage = pipelineStages.ClosingStage;
            var rule = target == LeadStage.Closed
                ? lead.Stage == closingStage
                    ? RuleResult.Success()
                    : RuleResult.Fail(
                        $"Closed can only be requested from {pipelineStages.GetDisplayName(closingStage)}.")
                : PipelineRules.CanMove(
                    lead,
                    target,
                    nextStage,
                    anotherHoldsParcel,
                    stageInfo.Name,
                    pipelineStages.GetDisplayName(LeadStage.Negotiation));
            if (user.Role is Roles.BranchManager or Roles.SuperAdmin)
                rule = RuleResult.Fail(access.MutationUnavailableReason);
            else if (access.IsAgent && lead.AssignedStaffId != access.CurrentStaffId)
                rule = RuleResult.Fail("Agents may only move leads assigned to them.");
            list.Add(new LeadMoveOptionDto(
                target, rule.Ok, rule.Ok ? null : rule.Message,
                target == LeadStage.Lost, target == LeadStage.Closed));
        }
        return ServiceResult<IReadOnlyList<LeadMoveOptionDto>>.Success(list);
    }

    public async Task<ServiceResult<IReadOnlyList<TimelineItemDto>>> GetLeadTimelineAsync(Guid leadId)
    {
        var scope = access.GetDataReadScope("lead timeline");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<TimelineItemDto>>.Fail(scope.Message);
        await pipelineStages.EnsureLoadedAsync();
        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(leadId, scope.Value);
        if (lead is null)
            return ServiceResult<IReadOnlyList<TimelineItemDto>>.Fail("That lead was not found or is outside your access scope.");

        var items = new List<TimelineItemDto>();
        var history = await uow.StageHistories.ListByLeadAsync(leadId);
        var staffIds = history.Where(row => row.ChangedByStaffId is not null)
            .Select(row => row.ChangedByStaffId!.Value).Distinct().ToArray();
        var staff = await uow.Staff.ListByIdsAsync(staffIds);
        var names = staff.ToDictionary(member => member.Id, member => member.FullName);
        foreach (var row in history)
        {
            var name = row.ChangedByStaffId is { } id && names.TryGetValue(id, out var staffName) ? staffName : null;
            if (row.FromStage is not null && row.FromStage == row.ToStage)
            {
                items.Add(new TimelineItemDto(row.ChangedAtUtc.ToLocalTime(), DealTimelineKind.Reassignment,
                    "Reassigned", row.Note, name));
                continue;
            }
            items.Add(new TimelineItemDto(row.ChangedAtUtc.ToLocalTime(), DealTimelineKind.StageChange,
                row.FromStage is null
                    ? $"Stage: {pipelineStages.GetDisplayName(row.ToStage)}"
                    : $"{pipelineStages.GetDisplayName(row.FromStage.Value)} -> {pipelineStages.GetDisplayName(row.ToStage)}",
                row.Note, name));
        }
        var interactions = await uow.Interactions.ListByLeadAsync(leadId, scope.Value);
        foreach (var row in interactions)
        {
            var name = row.LoggedByStaffId is { } id && names.TryGetValue(id, out var staffName) ? staffName : null;
            items.Add(new TimelineItemDto(row.OccurredAtUtc.ToLocalTime(), DealTimelineKind.Interaction,
                row.Type.ToString(), row.Note, name));
        }
        var deals = await uow.Deals.ListByLeadAsync(leadId);
        foreach (var deal in deals)
        {
            items.Add(new TimelineItemDto(deal.CreatedAtUtc.ToLocalTime(), DealTimelineKind.DealEvent,
                "Deal request created", null, null));
            if (deal.ConfirmedAtUtc is { } confirmed)
            {
                items.Add(new TimelineItemDto(confirmed.ToLocalTime(), DealTimelineKind.DealEvent,
                    "Deal confirmed", $"Deal was confirmed as {deal.Parcel.Status}.", null));
            }
            if (deal.TerminatedAtUtc is { } terminated)
            {
                var detail = deal.TerminationReason;
                if (deal.TerminationMoneyOutcome is { } outcome)
                    detail = string.IsNullOrWhiteSpace(detail)
                        ? $"{outcome}"
                        : $"{detail}; {outcome}" +
                            (deal.RefundedAmount is { } refunded ? $"; {refunded:C}" : string.Empty);
                items.Add(new TimelineItemDto(terminated.ToLocalTime(), DealTimelineKind.DealEvent,
                    deal.Status == DealStatus.Defaulted ? "Deal defaulted" : "Deal cancelled",
                    detail, null));
            }
            if (deal.Status == DealStatus.Cancelled && deal.ConfirmedAtUtc is null)
            {
                var detail = string.IsNullOrWhiteSpace(deal.TerminationReason) ? null : deal.TerminationReason;
                items.Insert(0, new TimelineItemDto(deal.CreatedAtUtc.ToLocalTime(), DealTimelineKind.DealEvent,
                    "Deal request returned", detail, null));
            }
        }
        return ServiceResult<IReadOnlyList<TimelineItemDto>>.Success(
            items.OrderByDescending(item => item.When).ToList());
    }

    public async Task<ServiceResult<bool>> ReassignLeadAsync(Guid leadId, Guid toStaffId, string? note = null)
    {
        var permission = RequireTenantAdmin("reassign leads");
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);
        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(leadId);
        if (lead is null)
            return ServiceResult<bool>.Fail("That lead was not found.");
        if (!await uow.Staff.IsActiveAgentAsync(toStaffId))
            return ServiceResult<bool>.Fail("The destination staff member must be an active Agent.");
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return ServiceResult<bool>.Fail("Closed and Lost leads are never reassigned.");
        var target = (await uow.Staff.ListByIdsAsync([toStaffId])).FirstOrDefault();
        var source = lead.AssignedStaffId is { } sourceId
            ? (await uow.Staff.ListByIdsAsync([sourceId])).FirstOrDefault()
            : null;
        if (target is null || !target.IsActive || target.Role != Roles.Agent)
            return ServiceResult<bool>.Fail("The destination staff member must be an active Agent.");
        if (features.Branching && source?.BranchId != target.BranchId)
            return ServiceResult<bool>.Fail("Tier C does not allow reassignment between branches.");

        lead.AssignedStaffId = toStaffId;
        lead.LastActivityAtUtc = clock.UtcNow;
        uow.StageHistories.Add(new StageHistory
        {
            LeadId = lead.Id,
            FromStage = lead.Stage,
            ToStage = lead.Stage,
            ChangedAtUtc = lead.LastActivityAtUtc,
            ChangedByStaffId = user.UserId,
            Note = string.IsNullOrWhiteSpace(note)
                ? $"Reassigned to {target?.FullName ?? "another agent"}"
                : note.Trim()
        });
        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The lead changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new LeadStageChanged(lead.Id, lead.Stage, lead.Stage, lead.LastActivityAtUtc));
        return ServiceResult<bool>.Success(true, "Lead reassigned. History was preserved.");
    }

    public async Task<ServiceResult<bool>> ReassignContactAsync(
        Guid contactId, Guid toStaffId, bool includeOpenLeads)
    {
        var permission = RequireTenantAdmin("reassign contacts");
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);
        await using var uow = uowFactory.Create();
        var contact = await uow.Contacts.GetAsync(contactId);
        if (contact is null)
            return ServiceResult<bool>.Fail("That contact was not found.");
        if (!await uow.Staff.IsActiveAgentAsync(toStaffId))
            return ServiceResult<bool>.Fail("The destination staff member must be an active Agent.");

        var target = (await uow.Staff.ListByIdsAsync([toStaffId])).FirstOrDefault();
        var source = contact.AssignedStaffId is { } sourceId
            ? (await uow.Staff.ListByIdsAsync([sourceId])).FirstOrDefault()
            : null;
        if (target is null || !target.IsActive || target.Role != Roles.Agent)
            return ServiceResult<bool>.Fail("The destination staff member must be an active Agent.");
        if (features.Branching && source?.BranchId != target.BranchId)
            return ServiceResult<bool>.Fail("Tier C does not allow reassignment between branches.");

        contact.AssignedStaffId = toStaffId;
        var changedLeads = new List<Lead>();
        if (includeOpenLeads)
        {
            var leads = await uow.Leads.ListByContactAsync(contactId);
            foreach (var lead in leads.Where(item => item.Stage is not (LeadStage.Closed or LeadStage.Lost)))
            {
                var tracked = await uow.Leads.GetAsync(lead.Id);
                if (tracked is null)
                    continue;
                tracked.AssignedStaffId = toStaffId;
                tracked.LastActivityAtUtc = clock.UtcNow;
                changedLeads.Add(tracked);
                uow.StageHistories.Add(new StageHistory
                {
                    LeadId = tracked.Id,
                    FromStage = tracked.Stage,
                    ToStage = tracked.Stage,
                    ChangedAtUtc = tracked.LastActivityAtUtc,
                    ChangedByStaffId = user.UserId,
                    Note = $"Contact reassigned; lead reassigned to {target?.FullName ?? "another agent"}"
                });
            }
        }
        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The contact or its leads changed concurrently. Reload and try again.");
        }
        var occurredAtUtc = clock.UtcNow;
        await events.DispatchAsync(new ContactChanged(contact.Id, occurredAtUtc));
        foreach (var lead in changedLeads)
            await events.DispatchAsync(new LeadStageChanged(lead.Id, lead.Stage, lead.Stage, occurredAtUtc));
        return ServiceResult<bool>.Success(true, "Contact reassigned.");
    }

    public async Task<ServiceResult<ReassignmentSummary>> ReassignAllFromAgentAsync(Guid fromStaffId, Guid toStaffId)
    {
        var permission = RequireTenantAdmin("reassign records");
        if (!permission.Ok)
            return ServiceResult<ReassignmentSummary>.Fail(permission.Message);
        if (fromStaffId == toStaffId)
            return ServiceResult<ReassignmentSummary>.Fail("Choose different source and destination Agents.");
        await using var uow = uowFactory.Create();
        if (!await uow.Staff.IsActiveAgentAsync(fromStaffId)
            || !await uow.Staff.IsActiveAgentAsync(toStaffId))
            return ServiceResult<ReassignmentSummary>.Fail("Both source and destination must be active Agents.");
        var staff = await uow.Staff.ListByIdsAsync([fromStaffId, toStaffId]);
        var source = staff.FirstOrDefault(item => item.Id == fromStaffId);
        var target = staff.FirstOrDefault(item => item.Id == toStaffId);
        if (source is null || target is null || !source.IsActive || !target.IsActive
            || source.Role != Roles.Agent || target.Role != Roles.Agent)
            return ServiceResult<ReassignmentSummary>.Fail("Both source and destination must be active Agents.");
        if (features.Branching && source.BranchId != target.BranchId)
            return ServiceResult<ReassignmentSummary>.Fail("Tier C does not allow reassignment between branches.");

        var contacts = await uow.Contacts.SearchAsync(null);
        var contactsReassigned = 0;
        var changedContacts = new List<Guid>();
        foreach (var contact in contacts.Where(item => item.AssignedStaffId == fromStaffId))
        {
            var tracked = await uow.Contacts.GetAsync(contact.Id);
            if (tracked is null)
                continue;
            tracked.AssignedStaffId = toStaffId;
            contactsReassigned++;
            changedContacts.Add(tracked.Id);
        }

        var leads = await uow.Leads.ListAsync(null, null, fromStaffId);
        var leadsReassigned = 0;
        var changedLeads = new List<Lead>();
        foreach (var lead in leads.Where(item => item.Stage is not (LeadStage.Closed or LeadStage.Lost)))
        {
            var tracked = await uow.Leads.GetAsync(lead.Id);
            if (tracked is null)
                continue;
            tracked.AssignedStaffId = toStaffId;
            tracked.LastActivityAtUtc = clock.UtcNow;
            changedLeads.Add(tracked);
            uow.StageHistories.Add(new StageHistory
            {
                LeadId = tracked.Id,
                FromStage = tracked.Stage,
                ToStage = tracked.Stage,
                ChangedAtUtc = tracked.LastActivityAtUtc,
                ChangedByStaffId = user.UserId,
                Note = "Reassigned in bulk with the agent"
            });
            leadsReassigned++;
        }

        var parcels = await uow.Parcels.ListAsync();
        var parcelsReassigned = 0;
        var changedParcels = new List<Guid>();
        foreach (var parcel in parcels.Where(item => item.AssignedStaffId == fromStaffId))
        {
            var tracked = await uow.Parcels.GetAsync(parcel.Id);
            if (tracked is null)
                continue;
            tracked.AssignedStaffId = toStaffId;
            parcelsReassigned++;
            changedParcels.Add(tracked.Id);
        }

        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return ServiceResult<ReassignmentSummary>.Fail("Some assigned records changed concurrently. Reload and try again.");
        }
        catch (ParcelConcurrencyConflictException)
        {
            return ServiceResult<ReassignmentSummary>.Fail("A parcel changed concurrently. Reload and try again.");
        }
        var occurredAtUtc = clock.UtcNow;
        foreach (var contactId in changedContacts)
            await events.DispatchAsync(new ContactChanged(contactId, occurredAtUtc));
        foreach (var lead in changedLeads)
            await events.DispatchAsync(new LeadStageChanged(lead.Id, lead.Stage, lead.Stage, occurredAtUtc));
        foreach (var parcelId in changedParcels)
            await events.DispatchAsync(new ParcelChanged(parcelId, occurredAtUtc));
        return ServiceResult<ReassignmentSummary>.Success(
            new ReassignmentSummary(contactsReassigned, leadsReassigned, parcelsReassigned),
            $"Reassigned {contactsReassigned} contacts, {leadsReassigned} open leads, and {parcelsReassigned} parcels.");
    }

    public async Task<ServiceResult<ReassignmentSummary>> PreviewReassignAllFromAgentAsync(
        Guid fromStaffId, Guid toStaffId)
    {
        var permission = RequireTenantAdmin("preview reassignment");
        if (!permission.Ok)
            return ServiceResult<ReassignmentSummary>.Fail(permission.Message);
        if (fromStaffId == toStaffId)
            return ServiceResult<ReassignmentSummary>.Fail("Choose different source and destination Agents.");

        await using var uow = uowFactory.Create();
        if (!await uow.Staff.IsActiveAgentAsync(fromStaffId)
            || !await uow.Staff.IsActiveAgentAsync(toStaffId))
            return ServiceResult<ReassignmentSummary>.Fail("Both source and destination must be active Agents.");
        var staff = await uow.Staff.ListByIdsAsync([fromStaffId, toStaffId]);
        var source = staff.FirstOrDefault(item => item.Id == fromStaffId);
        var target = staff.FirstOrDefault(item => item.Id == toStaffId);
        if (source is null || target is null || !source.IsActive || !target.IsActive
            || source.Role != Roles.Agent || target.Role != Roles.Agent)
            return ServiceResult<ReassignmentSummary>.Fail("Both source and destination must be active Agents.");
        if (features.Branching && source.BranchId != target.BranchId)
            return ServiceResult<ReassignmentSummary>.Fail("Tier C does not allow reassignment between branches.");

        var contacts = (await uow.Contacts.SearchAsync(null))
            .Count(contact => contact.AssignedStaffId == fromStaffId);
        var leads = (await uow.Leads.ListAsync(null, null, fromStaffId))
            .Count(lead => lead.Stage is not (LeadStage.Closed or LeadStage.Lost));
        var parcels = (await uow.Parcels.ListAsync())
            .Count(parcel => parcel.AssignedStaffId == fromStaffId);
        return ServiceResult<ReassignmentSummary>.Success(
            new ReassignmentSummary(contacts, leads, parcels),
            $"Ready to reassign {contacts} contacts, {leads} open leads, and {parcels} parcels.");
    }

    private RuleResult RequireTenantAdmin(string action)
    {
        var permission = access.RequireDataMutation(action);
        if (!permission.Ok)
            return permission;
        return user.Role == Roles.TenantAdmin
            ? RuleResult.Success()
            : RuleResult.Fail("Only a Tenant Admin can reassign records.");
    }

    public async Task<LeadResult> MoveStageAsync(MoveStageRequest request)
    {
        var permission = access.RequireDataMutation("move lead stages");
        if (!permission.Ok)
            return LeadResult.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        LeadResult? result = null;
        LeadStage? movedFrom = null;
        Guid movedLeadId = Guid.Empty;
        var movedAtUtc = default(DateTime);

        try
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
                if (!await uow.AcquirePipelineStagesLockAsync())
                {
                    result = LeadResult.Fail("The pipeline stages were just changed. Refresh and try again.");
                    return;
                }

                var setting = await uow.Settings.GetByKeyAsync(PipelineStageConfigJson.SettingKey);
                if (!PipelineStageConfigJson.TryReadCurrentConfiguration(setting?.Value, out var configuration))
                {
                    result = LeadResult.Fail("The pipeline stages were just changed. Refresh and try again.");
                    return;
                }

                var lead = await uow.Leads.GetAsync(request.LeadId);
                if (lead is null)
                {
                    result = LeadResult.Fail("That lead was not found.");
                    return;
                }

                var leadPermission = access.RequireLeadMutation(lead, "move this lead's stage");
                if (!leadPermission.Ok)
                {
                    result = LeadResult.Fail(leadPermission.Message);
                    return;
                }

                var nextStage = PipelineStageDefinition.Next(lead.Stage, configuration);
                var stageRuleFailed = lead.Stage is not (LeadStage.Closed or LeadStage.Lost)
                    && request.ToStage is not (LeadStage.Lost or LeadStage.Closed)
                    && nextStage != request.ToStage;
                if (stageRuleFailed)
                {
                    result = LeadResult.Fail("The pipeline stages were just changed. Refresh and try again.");
                    return;
                }

                var anotherHoldsParcel = request.ToStage == LeadStage.Negotiation
                    && await uow.Leads.AnotherLeadInNegotiationAsync(lead.ParcelId, lead.Id);
                var targetName = PipelineStageDefinition.GetDisplayName(request.ToStage, configuration);
                var rule = PipelineRules.CanMove(
                    lead,
                    request.ToStage,
                    nextStage,
                    anotherHoldsParcel,
                    targetName,
                    PipelineStageDefinition.GetDisplayName(LeadStage.Negotiation, configuration));
                if (!rule.Ok)
                {
                    result = LeadResult.Fail(rule.Message);
                    return;
                }

                if (request.OfferAmount is <= 0)
                {
                    result = LeadResult.Fail("The offer must be more than zero.");
                    return;
                }

                var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
                if (request.ToStage == LeadStage.Lost && note is null)
                {
                    result = LeadResult.Fail("Enter a note describing why the lead was lost.");
                    return;
                }

                movedFrom = lead.Stage;
                movedAtUtc = clock.UtcNow;
                movedLeadId = lead.Id;
                lead.Stage = request.ToStage;
                lead.LastActivityAtUtc = movedAtUtc;
                if (request.OfferAmount is { } offer)
                    lead.OfferAmount = offer;
                if (request.ToStage == LeadStage.Lost)
                    lead.LostReason = string.IsNullOrWhiteSpace(request.LostReason)
                        ? note
                        : request.LostReason.Trim();

                uow.StageHistories.Add(new StageHistory
                {
                    LeadId = lead.Id,
                    FromStage = movedFrom.Value,
                    ToStage = request.ToStage,
                    ChangedAtUtc = movedAtUtc,
                    ChangedByStaffId = user.UserId,
                    Note = note
                });

                await uow.SaveAsync();
                result = LeadResult.Success(
                    ToDto(lead, configuration),
                    $"Moved to {targetName}.");
            });
        }
        catch (DealLifecycleConcurrencyConflictException)
        {
            return LeadResult.Fail("This lead changed concurrently. Refresh and try again.");
        }

        if (movedFrom is { } from)
            await events.DispatchAsync(new LeadStageChanged(movedLeadId, from, request.ToStage, movedAtUtc));
        return result ?? LeadResult.Fail("The lead stage could not be changed. Refresh and try again.");
    }

    private LeadDto ToDto(Lead l) => ToDto(l, pipelineStages.GetConfiguration());

    private static LeadDto ToDto(Lead l, IReadOnlyList<PipelineStageSetting> configuration) => new(
        l.Id, l.ContactId, l.Contact.FullName, l.Contact.Phone, l.Contact.Email, l.Contact.Source,
        l.Contact.ConsentSms, l.Contact.ConsentEmail, l.ParcelId, l.Parcel.Label, l.Stage,
        PipelineStageDefinition.GetDisplayName(l.Stage, configuration),
        l.CloseRequested, l.OfferAmount, l.LostReason, l.LastActivityAtUtc);
}
