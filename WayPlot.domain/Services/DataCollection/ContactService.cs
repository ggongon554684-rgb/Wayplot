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
    IClock clock,
    TenantAccessPolicy access)
{
    public async Task<CaptureResult> CaptureAsync(CaptureContactRequest request)
    {
        var permission = access.RequireDataMutation("capture contacts");
        if (!permission.Ok)
            return CaptureResult.Fail(permission.Message);

        if (access.IsAgent && request.AssignedStaffId is { } assignedStaffId && assignedStaffId != access.CurrentStaffId)
            return CaptureResult.Fail("Agents can only capture contacts assigned to themselves.");

        var effectiveRequest = access.IsAgent
            ? request with { AssignedStaffId = access.CurrentStaffId }
            : request;

        var error = ValidateRequest(effectiveRequest, out var name, out var phoneText, out var phone, out var email);
        if (error is not null)
            return CaptureResult.Fail(error);

        await using var uow = uowFactory.Create();

        var referrerError = await ValidateReferrerAsync(uow, effectiveRequest);
        if (referrerError is not null)
            return CaptureResult.Fail(referrerError);

        if (effectiveRequest.AssignedStaffId is { } effectiveAssignedStaffId
            && effectiveAssignedStaffId != user.UserId
            && !await uow.Staff.IsActiveAgentAsync(effectiveAssignedStaffId))
            return CaptureResult.Fail("The assigned agent was not found or is inactive.");

        var existing = await uow.Contacts.FindByPhoneOrEmailAsync(phone, email);
        if (existing is not null)
            return await RecordInquiryOnExistingAsync(uow, existing, effectiveRequest);

        var now = clock.UtcNow;
        var contact = new Contact
        {
            FullName = name,
            Phone = phoneText,
            PhoneNormalized = phone,
            Email = email,
            Source = effectiveRequest.Source,
            Type = effectiveRequest.Type,
            ConsentSms = effectiveRequest.ConsentSms,
            ConsentEmail = effectiveRequest.ConsentEmail,
            ConsentAtUtc = effectiveRequest.ConsentSms || effectiveRequest.ConsentEmail ? now : null,
            ReferrerContactId = effectiveRequest.ReferrerContactId,
            ReferrerStaffId = effectiveRequest.ReferrerStaffId,
            ReferrerName = Clean(effectiveRequest.ReferrerName),
            ReferrerPhone = Clean(effectiveRequest.ReferrerPhone),
            AssignedStaffId = effectiveRequest.AssignedStaffId
        };

        uow.Contacts.Add(contact);
        await uow.SaveAsync();

        return CaptureResult.Success(
            new CaptureContactResult(contact.Id, contact.FullName, MatchedExisting: false, InquiryRecorded: false),
            "Contact saved.");
    }

    internal static string? ValidateRequest(
        CaptureContactRequest request,
        out string name,
        out string? phoneText,
        out string? phone,
        out string? email)
    {
        name = request.FullName?.Trim() ?? string.Empty;
        phoneText = null;
        phone = null;
        email = null;

        if (name.Length == 0)
            return "Enter the contact's full name.";
        if (name.Length > 150)
            return "The name is too long (150 characters at most).";
        if (!Enum.IsDefined(request.Source))
            return "Choose a valid source.";
        if (!Enum.IsDefined(request.Type))
            return "Choose a valid contact type.";
        if (request.Type == ContactType.Client)
            return "Client status is set automatically when a deal is confirmed.";

        phoneText = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        phone = PhoneNormalizer.Normalize(phoneText);
        if (phoneText is not null && phone is null)
            return "That is not a valid Philippine mobile number. Example: 0917 123 4567.";

        if (!TryNormalizeEmail(request.Email, out email))
            return "That email address is not valid.";
        if (phone is null && email is null)
            return "Enter a phone number or an email address.";
        if (request.ConsentSms && phone is null)
            return "SMS consent needs a mobile number.";
        if (request.ConsentEmail && email is null)
            return "Email consent needs an email address.";

        return null;
    }
    // Paste this method inside ContactService, right after CaptureAsync (before RecordInquiryOnExistingAsync).

    /// <summary>The contacts for the list screen, ordered by name. A blank search returns everyone.</summary>
    public async Task<ServiceResult<ContactListItem>> GetAsync(Guid contactId)
    {
        var scope = access.GetDataReadScope("contacts");
        if (!scope.Ok)
            return ServiceResult<ContactListItem>.Fail(scope.Message);
        await using var uow = uowFactory.Create();
        var contact = await uow.Contacts.GetAsync(contactId, scope.Value);
        if (contact is null)
            return ServiceResult<ContactListItem>.Fail("That contact was not found or is outside your access scope.");
        return ServiceResult<ContactListItem>.Success(new ContactListItem(
            contact.Id, contact.FullName, contact.Phone, contact.Email, contact.Source,
            contact.Type, contact.ConsentSms, contact.ConsentEmail));
    }

    public async Task<ServiceResult<IReadOnlyList<ContactListItem>>> ListAsync(string? search)
    {
        var scope = access.GetDataReadScope("contacts");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<ContactListItem>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var found = await uow.Contacts.SearchAsync(search, scope.Value);

        IReadOnlyList<ContactListItem> items = found
            .Select(c => new ContactListItem(
                c.Id, c.FullName, c.Phone, c.Email, c.Source, c.Type, c.ConsentSms, c.ConsentEmail))
            .ToList();

        return ServiceResult<IReadOnlyList<ContactListItem>>.Success(items, string.Empty);
    }

    public async Task<ServiceResult<IReadOnlyList<PeopleListItemDto>>> ListPeopleAsync(PeopleFilter filter)
    {
        var scope = access.GetDataReadScope("people");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<PeopleListItemDto>>.Fail(scope.Message);
        var kinds = filter.Kinds ?? new HashSet<PersonKind>();
        var limit = filter.Limit is > 0 and <= 1000 ? filter.Limit : 200;

        await using var uow = uowFactory.Create();
        var contacts = await uow.Contacts.SearchAsync(filter.SearchText, scope.Value);
        if (user.Role != Roles.TenantAdmin)
            filter = filter with { AssignedStaffId = null };
        else if (filter.AssignedStaffId is { } assignedId)
            contacts = contacts.Where(contact => contact.AssignedStaffId == assignedId).ToList();

        var allLeads = await uow.Leads.ListAsync(null, filter.SearchText, scope: scope.Value);
        var allDeals = await uow.Deals.ListAsync(
            scope.Value.Kind == TenantDataScopeKind.Agent ? scope.Value.StaffId : null,
            scope.Value.Kind == TenantDataScopeKind.Branch ? scope.Value.BranchId : null);
        var parcels = await uow.Parcels.ListAsync();
        if (scope.Value.Kind == TenantDataScopeKind.Branch)
            parcels = parcels.Where(parcel => parcel.BranchId == scope.Value.BranchId).ToList();

        var staffIds = contacts.Where(contact => contact.AssignedStaffId is not null)
            .Select(contact => contact.AssignedStaffId!.Value)
            .Concat(allLeads.Where(lead => lead.AssignedStaffId is not null).Select(lead => lead.AssignedStaffId!.Value))
            .Distinct().ToArray();
        var staff = await uow.Staff.ListByIdsAsync(staffIds);
        var staffNames = staff.ToDictionary(member => member.Id, member => member.FullName);

        var ownerParcelCounts = parcels
            .GroupBy(parcel => parcel.OwnerId)
            .ToDictionary(group => group.Key, group => group.Count());
        var owners = await uow.Owners.ListAsync();
        if (scope.Value.Kind == TenantDataScopeKind.Branch)
        {
            var ownerIds = parcels.Select(parcel => parcel.OwnerId).ToHashSet();
            owners = owners.Where(owner => ownerIds.Contains(owner.Id)).ToList();
        }

        var result = new List<PeopleListItemDto>();
        if (kinds.Count == 0 || kinds.Contains(PersonKind.Customer)
            || kinds.Contains(PersonKind.Client) || kinds.Contains(PersonKind.Landowner))
        {
            foreach (var contact in contacts)
            {
                var kind = contact.Type switch
                {
                    ContactType.Client => PersonKind.Client,
                    ContactType.Landowner => PersonKind.Landowner,
                    _ => PersonKind.Customer
                };
                if (kinds.Count > 0 && !kinds.Contains(kind))
                    continue;

                var matchingOwner = kind == PersonKind.Landowner
                    ? owners.FirstOrDefault(owner => NormalizedDigits(owner.Phone) is { } ownerPhone
                        && ownerPhone.Length > 0
                        && ownerPhone == NormalizedDigits(contact.PhoneNormalized))
                    : null;
                var openLeadCount = allLeads.Count(lead =>
                    lead.ContactId == contact.Id && lead.Stage is not LeadStage.Lost and not LeadStage.Closed);
                var contactDeals = allDeals.Where(deal => deal.ContactId == contact.Id).ToList();
                var balance = kind == PersonKind.Client
                    ? contactDeals.Where(deal => deal.Status is DealStatus.Current or DealStatus.Overdue or DealStatus.FullyPaid)
                        .Sum(deal => Math.Max(0, deal.Price - deal.Discount
                            - deal.Payments.Where(payment => payment.Status == PaymentStatus.Confirmed).Sum(payment => payment.Amount)))
                    : (decimal?)null;
                var parcelCount = matchingOwner is null
                    ? 0
                    : ownerParcelCounts.TryGetValue(matchingOwner.Id, out var count) ? count : 0;
                var hasOverdueDeal = contactDeals.Any(deal => deal.Status == DealStatus.Overdue);
                result.Add(new PeopleListItemDto(
                    kind, contact.Id, contact.FullName, contact.Phone, contact.Email,
                    contact.AssignedStaffId is { } staffId && staffNames.TryGetValue(staffId, out var agentName)
                        ? agentName : null,
                    openLeadCount, contactDeals.Count, balance, parcelCount,
                    kind == PersonKind.Landowner ? matchingOwner?.Id : null,
                    hasOverdueDeal));
            }
        }

        if (kinds.Count == 0 || kinds.Contains(PersonKind.Landowner))
        {
            foreach (var owner in owners)
            {
                var ownerPhone = NormalizedDigits(owner.Phone);
                var alreadyMerged = ownerPhone.Length > 0 && contacts.Any(contact =>
                    contact.Type == ContactType.Landowner
                    && NormalizedDigits(contact.PhoneNormalized) == ownerPhone);
                if (alreadyMerged)
                    continue;
                result.Add(new PeopleListItemDto(
                    PersonKind.Landowner, owner.Id, owner.Name, owner.Phone, owner.Email, null,
                    0, 0, null, ownerParcelCounts.TryGetValue(owner.Id, out var count) ? count : 0, owner.Id));
            }
        }

        var search = filter.SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var normalizedSearch = NormalizedDigits(search);
            result = result.Where(person =>
                person.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(person.Phone)
                    && (!string.IsNullOrEmpty(normalizedSearch)
                        ? NormalizedDigits(person.Phone).Contains(normalizedSearch, StringComparison.Ordinal)
                        : person.Phone!.Contains(search, StringComparison.OrdinalIgnoreCase)))
                || (!string.IsNullOrEmpty(person.Email) && person.Email!.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return ServiceResult<IReadOnlyList<PeopleListItemDto>>.Success(
            result.OrderBy(person => person.Name).Take(limit).ToList());
    }

    private static string NormalizedDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("63")) digits = "0" + digits[2..];
        else if (digits.Length == 10 && digits[0] == '9') digits = "0" + digits;
        return digits;
    }

    public async Task<ServiceResult<ContactReferralOptions>> ListReferralOptionsAsync()
    {
        var scope = access.GetDataReadScope("contacts");
        if (!scope.Ok)
            return ServiceResult<ContactReferralOptions>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var contacts = await uow.Contacts.ListReferralContactsAsync(scope.Value);
        var staff = await uow.Staff.ListActiveReferralStaffAsync();
        return ServiceResult<ContactReferralOptions>.Success(new ContactReferralOptions(
            contacts.Select(contact => new ReferralChoice(
                contact.Id,
                string.IsNullOrWhiteSpace(contact.Phone)
                    ? contact.FullName
                    : $"{contact.FullName} — {contact.Phone}")).ToList(),
            staff.Select(member => new ReferralChoice(
                member.Id,
                $"{member.FullName} ({member.Role})")).ToList()));
    }



    private async Task<CaptureResult> RecordInquiryOnExistingAsync(
        ITenantUnitOfWork uow, Contact existing, CaptureContactRequest request)
    {
        var protectedOtherAgent = access.IsAgentAssignedToAnotherStaff(existing.AssignedStaffId);
        var scope = access.GetDataReadScope("contacts");
        var isVisible = scope.Ok && await uow.Contacts.GetAsync(existing.Id, scope.Value) is not null;
        var protectExisting = !isVisible || protectedOtherAgent;
        if (!protectExisting)
        {
            var referrerResult = ApplyReferrer(existing, request);
            if (referrerResult is not null)
                return CaptureResult.Fail(referrerResult);
        }

        var now = clock.UtcNow;
        var channel = ChannelFor(request.Source);
        var note = string.IsNullOrWhiteSpace(request.InquiryNote)
            ? $"Inquiry received again (source: {request.Source})."
            : request.InquiryNote.Trim();
        var parcelId = request.ParcelId;
        if (parcelId is { } selectedParcelId && await uow.Parcels.GetAsync(selectedParcelId) is null)
            parcelId = null;

        uow.DuplicateInquiries.Add(new DuplicateInquiry
        {
            ContactId = existing.Id,
            ParcelId = parcelId,
            Source = request.Source,
            OccurredAtUtc = now,
            RecordedByStaffId = user.UserId,
            Details = note
        });

        var lead = await uow.Leads.GetLatestActiveForContactAsync(existing.Id);
        var interactionAdded = false;
        if (lead is not null && !protectExisting && access.RequireLeadMutation(lead, "log an interaction").Ok)
        {
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
            interactionAdded = true;
        }

        await uow.SaveAsync();
        if (interactionAdded && lead is not null)
            await events.DispatchAsync(new InteractionLogged(lead.Id, InteractionType.Inquiry, channel, now));

        return CaptureResult.Success(
            new CaptureContactResult(protectExisting ? Guid.Empty : existing.Id, protectExisting ? string.Empty : existing.FullName, MatchedExisting: true, InquiryRecorded: true),
            protectedOtherAgent
                ? "A contact already exists and is assigned to another agent."
                : protectExisting
                    ? "A contact already exists."
                    : "Matched an existing contact. Inquiry added to history.");
    }

    private static InteractionChannel ChannelFor(LeadSource source) => source switch
    {
        LeadSource.PhoneCall => InteractionChannel.Call,
        LeadSource.WalkIn => InteractionChannel.WalkIn,
        _ => InteractionChannel.Note
    };

    private async Task<string?> ValidateReferrerAsync(ITenantUnitOfWork uow, CaptureContactRequest request)
    {
        var fieldError = ValidateReferrerFields(request);
        if (fieldError is not null)
            return fieldError;

        var scope = access.GetDataReadScope("contacts");
        if (!scope.Ok)
            return scope.Message;
        if (request.ReferrerContactId is { } contactId && await uow.Contacts.GetAsync(contactId, scope.Value) is null)
            return "The selected referrer contact was not found.";
        if (request.ReferrerStaffId is { } staffId && !await uow.Staff.IsActiveReferralStaffAsync(staffId))
            return "The selected referrer staff member was not found or is inactive.";

        return null;
    }

    private static string? ValidateReferrerFields(CaptureContactRequest request)
    {
        var hasContact = request.ReferrerContactId is not null;
        var hasStaff = request.ReferrerStaffId is not null;
        var hasOther = !string.IsNullOrWhiteSpace(request.ReferrerName)
            || !string.IsNullOrWhiteSpace(request.ReferrerPhone);

        if ((hasContact ? 1 : 0) + (hasStaff ? 1 : 0) + (hasOther ? 1 : 0) > 1)
            return "Choose one referrer: an existing contact, a staff member, or another person.";

        if (request.ReferrerName?.Trim().Length > 150)
            return "The referrer's name is too long (150 characters at most).";
        if (!hasOther)
            return null;
        if (string.IsNullOrWhiteSpace(request.ReferrerName) || string.IsNullOrWhiteSpace(request.ReferrerPhone))
            return "Enter both the outside referrer's name and mobile number.";
        if (PhoneNormalizer.Normalize(request.ReferrerPhone) is null)
            return "The referrer's mobile number is invalid. Example: 0917 123 4567.";

        return null;
    }

    private static string? ApplyReferrer(Contact contact, CaptureContactRequest request)
    {
        if (request.ReferrerContactId == contact.Id)
            return "A contact cannot refer themselves.";

        if (request.ReferrerContactId is null && request.ReferrerStaffId is null
            && string.IsNullOrWhiteSpace(request.ReferrerName) && string.IsNullOrWhiteSpace(request.ReferrerPhone))
            return null;

        var hasExisting = contact.ReferrerContactId is not null || contact.ReferrerStaffId is not null
            || !string.IsNullOrWhiteSpace(contact.ReferrerName) || !string.IsNullOrWhiteSpace(contact.ReferrerPhone);
        if (hasExisting)
        {
            var same = contact.ReferrerContactId == request.ReferrerContactId
                && contact.ReferrerStaffId == request.ReferrerStaffId
                && string.Equals(contact.ReferrerName, Clean(request.ReferrerName), StringComparison.Ordinal)
                && string.Equals(contact.ReferrerPhone, Clean(request.ReferrerPhone), StringComparison.Ordinal);
            return same ? null : "This contact already has a different referrer recorded.";
        }

        contact.ReferrerContactId = request.ReferrerContactId;
        contact.ReferrerStaffId = request.ReferrerStaffId;
        contact.ReferrerName = Clean(request.ReferrerName);
        contact.ReferrerPhone = Clean(request.ReferrerPhone);
        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Blank is fine (returns true, email null). Otherwise it must be a plain address like name@host.com.</summary>
    internal static bool TryNormalizeEmail(string? input, out string? email)
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
