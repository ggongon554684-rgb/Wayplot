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
    IClock clock)
{
    public async Task<LeadResult> AttachContactToParcelAsync(AttachLeadRequest request)
    {
        await using var uow = uowFactory.Create();

        var contact = await uow.Contacts.GetAsync(request.ContactId);
        if (contact is null)
            return LeadResult.Fail("That contact was not found.");

        var parcel = await uow.Parcels.GetAsync(request.ParcelId);
        if (parcel is null)
            return LeadResult.Fail("That parcel was not found.");

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
            AssignedStaffId = user.UserId
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

    public async Task<IReadOnlyList<LeadDto>> ListByParcelAsync(Guid parcelId)
    {
        await using var uow = uowFactory.Create();
        return (await uow.Leads.ListByParcelAsync(parcelId)).Select(ToDto).ToList();
    }

    public async Task<ServiceResult<LeadFileDto>> GetFileAsync(Guid leadId)
    {
        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetForDisplayAsync(leadId);
        if (lead is null)
            return ServiceResult<LeadFileDto>.Fail("That lead was not found.");

        var history = await uow.StageHistories.ListByLeadAsync(leadId);
        var interactions = await uow.Interactions.ListByLeadAsync(leadId);
        return ServiceResult<LeadFileDto>.Success(new LeadFileDto(
            lead.Id,
            lead.Contact.FullName,
            lead.Contact.Phone,
            lead.Contact.Email,
            lead.Parcel.Label,
            lead.Stage,
            lead.OfferAmount,
            history.Select(h => new LeadStageHistoryRow(h.FromStage, h.ToStage, h.ChangedAtUtc, h.Note)).ToList(),
            interactions.Select(i => new LeadInteractionRow(i.Type, i.Channel, i.OccurredAtUtc, i.Note)).ToList()));
    }

    public async Task<LeadResult> MoveStageAsync(MoveStageRequest request)
    {
        await using var uow = uowFactory.Create();

        var lead = await uow.Leads.GetAsync(request.LeadId);
        if (lead is null)
            return LeadResult.Fail("That lead was not found.");

        // Check first so the agent gets a friendly message. The database index is only the safety net.
        var anotherHoldsParcel = request.ToStage == LeadStage.Negotiation
            && await uow.Leads.AnotherLeadInNegotiationAsync(lead.ParcelId, lead.Id);

        var rule = PipelineRules.CanMove(lead, request.ToStage, anotherHoldsParcel);
        if (!rule.Ok)
            return LeadResult.Fail(rule.Message);

        if (request.OfferAmount is <= 0)
            return LeadResult.Fail("The offer must be more than zero.");

        var from = lead.Stage;
        var now = clock.UtcNow;

        lead.Stage = request.ToStage;
        lead.LastActivityAtUtc = now;
        if (request.OfferAmount is { } offer)
            lead.OfferAmount = offer;
        if (request.ToStage == LeadStage.Lost)
            lead.LostReason = string.IsNullOrWhiteSpace(request.LostReason) ? null : request.LostReason.Trim();

        uow.StageHistories.Add(new StageHistory
        {
            LeadId = lead.Id,
            FromStage = from,
            ToStage = request.ToStage,
            ChangedAtUtc = now,
            ChangedByStaffId = user.UserId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        });

        await uow.SaveAsync();
        await events.DispatchAsync(new LeadStageChanged(lead.Id, from, request.ToStage, now));

        return LeadResult.Success(ToDto(lead), $"Moved to {request.ToStage}.");
    }

    private static LeadDto ToDto(Lead l) => new(
        l.Id, l.ContactId, l.Contact.FullName, l.Contact.Phone, l.Contact.Email, l.Contact.Source,
        l.Contact.ConsentSms, l.Contact.ConsentEmail, l.ParcelId, l.Parcel.Label, l.Stage,
        l.CloseRequested, l.OfferAmount, l.LostReason, l.LastActivityAtUtc);
}