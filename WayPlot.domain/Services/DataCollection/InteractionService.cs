using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Logs and lists lead interactions. Validation and persistence rules stay here.</summary>
public sealed class InteractionService(
    ITenantUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher events,
    ICurrentUser user,
    IClock clock,
    TenantAccessPolicy access)
{
    private const int MaxNoteLength = 2000;

    public async Task<ServiceResult<InteractionListItem>> LogAsync(LogInteractionRequest request)
    {
        var permission = access.RequireDataMutation("log interactions");
        if (!permission.Ok)
            return ServiceResult<InteractionListItem>.Fail(permission.Message);

        var note = request.Note?.Trim() ?? string.Empty;
        if (note.Length == 0)
            return ServiceResult<InteractionListItem>.Fail("Enter a note for the interaction.");
        if (note.Length > MaxNoteLength)
            return ServiceResult<InteractionListItem>.Fail($"The note must be {MaxNoteLength} characters or fewer.");
        if (!TryMapKind(request.Kind, request.Channel, out var type, out var channel))
            return ServiceResult<InteractionListItem>.Fail(
                "Choose a valid interaction type and channel.");

        if (request.Kind == InteractionLogKinds.Feedback)
        {
            if (request.Rating is null)
                return ServiceResult<InteractionListItem>.Fail("Feedback needs a rating from 1 to 5.");
            if (request.Rating is < 1 or > 5)
                return ServiceResult<InteractionListItem>.Fail("A feedback rating must be from 1 to 5.");
        }
        else if (request.Rating is not null)
        {
            return ServiceResult<InteractionListItem>.Fail("Only feedback can have a rating.");
        }

        var occurredAtUtc = request.OccurredAtUtc.Kind == DateTimeKind.Utc
            ? request.OccurredAtUtc
            : request.OccurredAtUtc.ToUniversalTime();
        if (occurredAtUtc > clock.UtcNow)
            return ServiceResult<InteractionListItem>.Fail("The interaction time cannot be in the future.");

        await using var uow = uowFactory.Create();
        var lead = await uow.Leads.GetAsync(request.LeadId);
        if (lead is null)
            return ServiceResult<InteractionListItem>.Fail("That lead was not found.");
        var leadPermission = access.RequireLeadMutation(lead, "log an interaction on this lead");
        if (!leadPermission.Ok)
            return ServiceResult<InteractionListItem>.Fail(leadPermission.Message);
        if (lead.Stage is LeadStage.Closed or LeadStage.Lost)
            return ServiceResult<InteractionListItem>.Fail("This lead is already finished.");

        var interaction = new Interaction
        {
            LeadId = lead.Id,
            Lead = lead,
            Type = type,
            Channel = channel,
            OccurredAtUtc = occurredAtUtc,
            Note = note,
            Rating = request.Rating,
            ComplaintStatus = request.Kind == InteractionLogKinds.Complaint
                ? WayPlot.domain.Common.ComplaintStatus.Open
                : null,
            ResolvedAtUtc = null,
            LoggedByStaffId = user.UserId
        };

        uow.Interactions.Add(interaction);
        if (occurredAtUtc > lead.LastActivityAtUtc)
            lead.LastActivityAtUtc = occurredAtUtc;

        await uow.SaveAsync();
        await events.DispatchAsync(new InteractionLogged(lead.Id, type, channel, occurredAtUtc));

        return ServiceResult<InteractionListItem>.Success(ToListItem(interaction), "Interaction logged.");
    }

    public async Task<ServiceResult<IReadOnlyList<InteractionListItem>>> ListAsync(
        Guid? leadId, InteractionType? type)
    {
        var scope = access.GetDataReadScope("interactions");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<InteractionListItem>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var found = await uow.Interactions.ListAsync(leadId, type, scope.Value);
        IReadOnlyList<InteractionListItem> items = found.Select(ToListItem).ToList();
        return ServiceResult<IReadOnlyList<InteractionListItem>>.Success(items);
    }

    private static bool TryMapKind(
        string? kind,
        InteractionChannel selectedChannel,
        out InteractionType type,
        out InteractionChannel channel)
    {
        type = default;
        channel = default;
        switch (kind)
        {
            case InteractionLogKinds.Call:
                type = InteractionType.Inquiry;
                channel = InteractionChannel.Call;
                break;
            case InteractionLogKinds.Text:
                type = InteractionType.Inquiry;
                channel = InteractionChannel.Sms;
                break;
            case InteractionLogKinds.Viewing:
                type = InteractionType.Inquiry;
                channel = InteractionChannel.Viewing;
                break;
            case InteractionLogKinds.Note:
                type = InteractionType.Inquiry;
                channel = InteractionChannel.Note;
                break;
            case InteractionLogKinds.Complaint:
                type = InteractionType.Complaint;
                if (!Enum.IsDefined(typeof(InteractionChannel), selectedChannel)) return false;
                channel = selectedChannel;
                break;
            case InteractionLogKinds.Feedback:
                type = InteractionType.Feedback;
                if (!Enum.IsDefined(typeof(InteractionChannel), selectedChannel)) return false;
                channel = selectedChannel;
                break;
            default:
                return false;
        }

        return kind is InteractionLogKinds.Complaint or InteractionLogKinds.Feedback ||
               selectedChannel == channel;
    }

    private static InteractionListItem ToListItem(Interaction interaction) => new(
        interaction.Id,
        interaction.LeadId,
        interaction.OccurredAtUtc,
        interaction.Lead.Contact.FullName,
        interaction.Lead.Parcel.Label,
        interaction.Type,
        interaction.Channel,
        interaction.Note,
        interaction.Rating,
        interaction.ComplaintStatus);
}
