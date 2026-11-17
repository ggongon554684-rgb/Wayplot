using System.Net.Mail;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using CaptureResult = WayPlot.domain.Common.ServiceResult<WayPlot.domain.Contracts.CaptureContactResult>;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Contact capture: validation, consent, and the duplicate check (normalized phone OR email).</summary>
public sealed class ContactService(
    ITenantUnitOfWorkFactory uowFactory,
    IDomainEventDispatcher events,
    ICurrentUser user,
    IClock clock)
{
    public async Task<CaptureResult> CaptureAsync(CaptureContactRequest request)
    {
        var name = request.FullName?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return CaptureResult.Fail("Enter the contact's full name.");
        if (name.Length > 150)
            return CaptureResult.Fail("The name is too long (150 characters at most).");

        var phoneText = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var phone = PhoneNormalizer.Normalize(phoneText);
        if (phoneText is not null && phone is null)
            return CaptureResult.Fail("That is not a valid Philippine mobile number. Example: 0917 123 4567.");

        if (!TryNormalizeEmail(request.Email, out var email))
            return CaptureResult.Fail("That email address is not valid.");

        if (phone is null && email is null)
            return CaptureResult.Fail("Enter a phone number or an email address.");

        // No consent means no SMS or email, so consent for a channel needs that channel's address.
        if (request.ConsentSms && phone is null)
            return CaptureResult.Fail("SMS consent needs a mobile number.");
        if (request.ConsentEmail && email is null)
            return CaptureResult.Fail("Email consent needs an email address.");

        await using var uow = uowFactory.Create();

        var existing = await uow.Contacts.FindByPhoneOrEmailAsync(phone, email);
        if (existing is not null)
            return await RecordInquiryOnExistingAsync(uow, existing, request);

        var now = clock.UtcNow;
        var contact = new Contact
        {
            FullName = name,
            Phone = phoneText,
            PhoneNormalized = phone,
            Email = email,
            Source = request.Source,
            Type = request.Type,
            ConsentSms = request.ConsentSms,
            ConsentEmail = request.ConsentEmail,
            ConsentAtUtc = request.ConsentSms || request.ConsentEmail ? now : null
        };

        uow.Contacts.Add(contact);
        await uow.SaveAsync();

        return CaptureResult.Success(
            new CaptureContactResult(contact.Id, contact.FullName, MatchedExisting: false, InquiryRecorded: false),
            "Contact saved.");
    }
    // Paste this method inside ContactService, right after CaptureAsync (before RecordInquiryOnExistingAsync).

    /// <summary>The contacts for the list screen, ordered by name. A blank search returns everyone.</summary>
    public async Task<ServiceResult<IReadOnlyList<ContactListItem>>> ListAsync(string? search)
    {
        await using var uow = uowFactory.Create();
        var found = await uow.Contacts.SearchAsync(search);

        IReadOnlyList<ContactListItem> items = found
            .Select(c => new ContactListItem(
                c.Id, c.FullName, c.Phone, c.Email, c.Source, c.Type, c.ConsentSms, c.ConsentEmail))
            .ToList();

        return ServiceResult<IReadOnlyList<ContactListItem>>.Success(items, string.Empty);
    }



    private async Task<CaptureResult> RecordInquiryOnExistingAsync(
        ITenantUnitOfWork uow, Contact existing, CaptureContactRequest request)
    {
        // An Interaction belongs to a Lead, so the inquiry goes on the contact's latest active lead.
        var lead = await uow.Leads.GetLatestActiveForContactAsync(existing.Id);
        if (lead is null)
        {
            return CaptureResult.Success(
                new CaptureContactResult(existing.Id, existing.FullName, MatchedExisting: true, InquiryRecorded: false),
                $"Matched existing contact {existing.FullName}. No duplicate was created. " +
                "This contact has no lead yet, so the inquiry note was not stored.");
        }

        var now = clock.UtcNow;
        var channel = ChannelFor(request.Source);
        var note = string.IsNullOrWhiteSpace(request.InquiryNote)
            ? $"Inquiry received again (source: {request.Source})."
            : request.InquiryNote.Trim();

        uow.Interactions.Add(new Interaction
        {
            LeadId = lead.Id,
            Type = InteractionType.Inquiry,
            Channel = channel,
            OccurredAtUtc = now,
            Note = note,
            LoggedByStaffId = user.UserId
        });
        lead.LastActivityAtUtc = now;

        await uow.SaveAsync();
        await events.DispatchAsync(new InteractionLogged(lead.Id, InteractionType.Inquiry, channel, now));

        return CaptureResult.Success(
            new CaptureContactResult(existing.Id, existing.FullName, MatchedExisting: true, InquiryRecorded: true),
            $"Matched existing contact {existing.FullName}. Inquiry added to history.");
    }

    private static InteractionChannel ChannelFor(LeadSource source) => source switch
    {
        LeadSource.PhoneCall => InteractionChannel.Call,
        LeadSource.WalkIn => InteractionChannel.WalkIn,
        _ => InteractionChannel.Note
    };

    /// <summary>Blank is fine (returns true, email null). Otherwise it must be a plain address like name@host.com.</summary>
    private static bool TryNormalizeEmail(string? input, out string? email)
    {
        email = null;
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text)) return true;

        if (!MailAddress.TryCreate(text, out var address) || address.Address != text || !address.Host.Contains('.'))
            return false;

        email = text.ToLowerInvariant();
        return true;
    }
}