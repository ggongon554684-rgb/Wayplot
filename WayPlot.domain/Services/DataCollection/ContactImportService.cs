using Microsoft.VisualBasic.FileIO;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;

namespace WayPlot.domain.Services.DataCollection;

public sealed class ContactImportService(
    ITenantUnitOfWorkFactory uowFactory,
    ContactService contacts,
    LeadService leads,
    ICurrentUser user,
    TenantAccessPolicy access)
{
    public ServiceResult<IReadOnlyList<ImportCsvColumn>> ReadColumns(string csvContent)
    {
        var permission = access.RequireDataMutation("import contacts");
        if (!permission.Ok)
            return ServiceResult<IReadOnlyList<ImportCsvColumn>>.Fail(permission.Message);

        var parsed = ParseCsv(csvContent);
        return parsed.Ok
            ? ServiceResult<IReadOnlyList<ImportCsvColumn>>.Success(parsed.Value!.Columns)
            : ServiceResult<IReadOnlyList<ImportCsvColumn>>.Fail(parsed.Message);
    }

    public async Task<ServiceResult<ContactImportPreview>> PreviewAsync(
        string csvContent,
        ContactImportMapping mapping)
    {
        var prepared = await PrepareAsync(csvContent, mapping);
        if (!prepared.Ok)
            return ServiceResult<ContactImportPreview>.Fail(prepared.Message);

        return ServiceResult<ContactImportPreview>.Success(new ContactImportPreview(
            prepared.Value!.Columns,
            prepared.Value.Rows.Select(row => row.Display).ToList()));
    }

    public async Task<ServiceResult<ContactImportSummary>> ImportAsync(
        string csvContent,
        ContactImportMapping mapping)
    {
        var preparedResult = await PrepareAsync(csvContent, mapping);
        if (!preparedResult.Ok)
            return ServiceResult<ContactImportSummary>.Fail(preparedResult.Message);

        var prepared = preparedResult.Value!;
        var contactsCreated = 0;
        var leadsCreated = 0;
        var duplicates = 0;
        var rejected = prepared.Rows.Count(row => row.Display.Status == ContactImportRowStatus.Invalid);

        foreach (var row in prepared.Rows.Where(row => row.Display.Status != ContactImportRowStatus.Invalid))
        {
            var result = await contacts.CaptureAsync(row.Request!);
            if (!result.Ok || result.Value is null)
            {
                row.Display = row.Display with
                {
                    Status = ContactImportRowStatus.Invalid,
                    Reason = result.Message,
                    WillCreateLead = false
                };
                rejected++;
                continue;
            }

            if (result.Value.MatchedExisting)
            {
                duplicates++;
                row.Display = row.Display with
                {
                    Status = ContactImportRowStatus.Duplicate,
                    Reason = result.Message,
                    WillCreateLead = false
                };
                continue;
            }

            contactsCreated++;
            if (row.Display.Status == ContactImportRowStatus.Valid && row.Display.WillCreateLead && row.ParcelId is { } parcelId)
            {
                var request = row.Request!;
                var leadResult = await leads.AttachContactToParcelAsync(
                    new AttachLeadRequest(result.Value.ContactId, parcelId, request.AssignedStaffId));
                if (leadResult.Ok)
                {
                    leadsCreated++;
                    row.Display = row.Display with { LeadCreated = true };
                }
                else
                {
                    row.Display = row.Display with
                    {
                        WillCreateLead = false,
                        Warning = AppendWarning(row.Display.Warning, $"Contact saved, but lead not created: {leadResult.Message}")
                    };
                }
            }
            else if (row.Display.Status == ContactImportRowStatus.Duplicate)
            {
                row.Display = row.Display with
                {
                    Status = ContactImportRowStatus.Valid,
                    Reason = "The duplicate changed after preview; contact saved without creating a lead.",
                    WillCreateLead = false,
                    Warning = AppendWarning(row.Display.Warning, "The duplicate changed after preview.")
                };
            }
        }

        var rows = prepared.Rows.Select(row => row.Display).OrderBy(row => row.RowNumber).ToList();
        return ServiceResult<ContactImportSummary>.Success(new ContactImportSummary(
            contactsCreated,
            leadsCreated,
            duplicates,
            rejected,
            rows.Count(row => !string.IsNullOrWhiteSpace(row.Warning)),
            prepared.Columns,
            rows));
    }

    private async Task<ServiceResult<PreparedImport>> PrepareAsync(
        string csvContent,
        ContactImportMapping mapping)
    {
        var permission = access.RequireDataMutation("import contacts");
        if (!permission.Ok)
            return ServiceResult<PreparedImport>.Fail(permission.Message);
        if (user.UserId is not { } importingStaffId)
            return ServiceResult<PreparedImport>.Fail("Sign in again before importing contacts.");

        var parsedResult = ParseCsv(csvContent);
        if (!parsedResult.Ok)
            return ServiceResult<PreparedImport>.Fail(parsedResult.Message);

        var parsed = parsedResult.Value!;
        var mappingError = ValidateMapping(mapping, parsed.Columns.Count, ignoreAgent: user.Role == Roles.Agent);
        if (mappingError is not null)
            return ServiceResult<PreparedImport>.Fail(mappingError);

        await using var uow = uowFactory.Create();
        var isAgent = user.Role == Roles.Agent;
        var agents = !isAgent && mapping.AssignedAgentColumn is not null
            ? await uow.Staff.ListActiveAgentsAsync()
            : Array.Empty<Staff>();
        var parcels = mapping.ParcelColumn is not null
            ? await uow.Parcels.ListAsync()
            : Array.Empty<Parcel>();
        var parcelsByLabel = parcels.ToDictionary(parcel => parcel.Label, StringComparer.OrdinalIgnoreCase);
        var typeIndex = parsed.Columns.ToList().FindIndex(column =>
            column.Header.Equals("Type", StringComparison.OrdinalIgnoreCase)
            || column.Header.Equals("ContactType", StringComparison.OrdinalIgnoreCase)
            || column.Header.Equals("Kind", StringComparison.OrdinalIgnoreCase));

        var rows = new List<PreparedRow>(parsed.Rows.Count);
        for (var index = 0; index < parsed.Rows.Count; index++)
        {
            var fields = parsed.Rows[index];
            var rowNumber = index + 2;
            var rawName = Column(fields, mapping.NameColumn);
            var rawPhone = OptionalColumn(fields, mapping.PhoneColumn);
            var rawEmail = OptionalColumn(fields, mapping.EmailColumn);
            var rawSource = mapping.SourceColumn < 0 ? string.Empty : Column(fields, mapping.SourceColumn);
            var source = ParseSource(rawSource);
            var rawParcel = OptionalColumn(fields, mapping.ParcelColumn);
            var rawAgent = isAgent ? null : OptionalColumn(fields, mapping.AssignedAgentColumn);

            var display = new ContactImportRow(
                rowNumber,
                ContactImportRowStatus.Valid,
                rawName.Trim(),
                EmptyToNull(rawPhone),
                EmptyToNull(rawEmail),
                source?.ToString() ?? rawSource,
                EmptyToNull(rawParcel),
                null,
                null,
                false,
                false,
                null,
                fields.ToArray());
            if (typeIndex >= 0)
            {
                var rawType = OptionalColumn(fields, typeIndex);
                if (string.Equals(rawType?.Trim(), "Client", StringComparison.OrdinalIgnoreCase))
                {
                    rows.Add(InvalidPrepared(display, "Client status is set automatically when a deal is confirmed."));
                    continue;
                }
            }

            if (fields.Length > parsed.Columns.Count)
            {
                rows.Add(InvalidPrepared(display, "The row has more columns than the CSV header."));
                continue;
            }
            if (source is null)
            {
                rows.Add(InvalidPrepared(display, $"Unknown source '{rawSource}'."));
                continue;
            }

            Guid? parcelId = null;
            var warning = (string?)null;
            if (!string.IsNullOrWhiteSpace(rawParcel))
            {
                if (parcelsByLabel.TryGetValue(rawParcel.Trim(), out var parcel))
                    parcelId = parcel.Id;
                else
                    warning = $"Parcel '{rawParcel.Trim()}' was not found; the contact will be imported without a lead.";
            }

            Guid? assignedStaffId = importingStaffId;
            if (!isAgent && !string.IsNullOrWhiteSpace(rawAgent))
            {
                var matches = agents.Where(agent =>
                    string.Equals(agent.Username, rawAgent.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count != 1)
                {
                    rows.Add(InvalidPrepared(display, matches.Count == 0
                        ? $"Agent username '{rawAgent.Trim()}' was not found or is inactive."
                        : $"Agent username '{rawAgent.Trim()}' is ambiguous."));
                    continue;
                }
                assignedStaffId = matches[0].Id;
            }

            var request = new CaptureContactRequest(
                rawName,
                rawPhone,
                rawEmail,
                source.Value,
                ContactType.Customer,
                ConsentGiven(fields, mapping.ConsentColumn) && !string.IsNullOrWhiteSpace(rawPhone),
                ConsentGiven(fields, mapping.ConsentColumn) && !string.IsNullOrWhiteSpace(rawEmail),
                $"CSV import row {rowNumber}: {rawName.Trim()}; phone={rawPhone?.Trim() ?? "not provided"}; " +
                $"email={rawEmail?.Trim() ?? "not provided"}; parcel={rawParcel?.Trim() ?? "not provided"}.",
                parcelId,
                assignedStaffId);
            var validation = ContactService.ValidateRequest(request, out var name, out var phoneText, out var phone, out var email);
            if (validation is not null)
            {
                rows.Add(InvalidPrepared(display, validation));
                continue;
            }

            display = display with
            {
                Name = name,
                Phone = phoneText,
                Email = email,
                Source = source.Value.ToString(),
                WillCreateLead = parcelId is not null,
                Warning = warning
            };
            rows.Add(new PreparedRow(display, request, parcelId, phone, email));
        }

        await MarkDuplicatesAsync(uow, rows);
        return ServiceResult<PreparedImport>.Success(new PreparedImport(parsed.Columns, rows));
    }

    private async Task MarkDuplicatesAsync(ITenantUnitOfWork uow, List<PreparedRow> rows)
    {
        var candidates = rows.Where(row => row.Request is not null).ToList();
        var phoneValues = candidates.Where(row => row.Phone is not null).Select(row => row.Phone!).Distinct().ToArray();
        var emailValues = candidates.Where(row => row.Email is not null).Select(row => row.Email!).Distinct().ToArray();
        var existing = new List<Contact>();
        for (var offset = 0; offset < Math.Max(phoneValues.Length, emailValues.Length); offset += 700)
        {
            var phones = phoneValues.Skip(offset).Take(700).ToArray();
            var emails = emailValues.Skip(offset).Take(700).ToArray();
            existing.AddRange(await uow.Contacts.FindMatchesAsync(phones, emails));
        }

        var readScope = access.GetDataReadScope("contacts");
        if (!readScope.Ok)
            return;
        var visibleIds = new HashSet<Guid>();
        if (readScope.Value.Kind == TenantDataScopeKind.Tenant)
        {
            foreach (var contact in existing)
                visibleIds.Add(contact.Id);
        }
        else
        {
            for (var offset = 0; offset < Math.Max(phoneValues.Length, emailValues.Length); offset += 700)
            {
                var phones = phoneValues.Skip(offset).Take(700).ToArray();
                var emails = emailValues.Skip(offset).Take(700).ToArray();
                foreach (var contact in await uow.Contacts.FindMatchesAsync(phones, emails, readScope.Value))
                    visibleIds.Add(contact.Id);
            }
        }

        var byPhone = existing.Where(contact => contact.PhoneNormalized is not null)
            .GroupBy(contact => contact.PhoneNormalized!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var byEmail = existing.Where(contact => contact.Email is not null)
            .GroupBy(contact => contact.Email!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var identityTargets = new Dictionary<string, DuplicateTarget>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in candidates)
        {
            var targets = new Dictionary<string, DuplicateTarget>(StringComparer.Ordinal);
            if (row.Phone is not null)
            {
                if (byPhone.TryGetValue(row.Phone, out var matches))
                    foreach (var match in matches)
                        AddTarget(targets, new DuplicateTarget(
                            match.Id, null, CanReveal(match, visibleIds) ? match.FullName : null,
                            access.IsAgentAssignedToAnotherStaff(match.AssignedStaffId)));
                if (identityTargets.TryGetValue("phone:" + row.Phone, out var priorPhone))
                    targets[priorPhone.Key] = priorPhone;
            }
            if (row.Email is not null)
            {
                if (byEmail.TryGetValue(row.Email, out var matches))
                    foreach (var match in matches)
                        AddTarget(targets, new DuplicateTarget(
                            match.Id, null, CanReveal(match, visibleIds) ? match.FullName : null,
                            access.IsAgentAssignedToAnotherStaff(match.AssignedStaffId)));
                if (identityTargets.TryGetValue("email:" + row.Email, out var priorEmail))
                    targets[priorEmail.Key] = priorEmail;
            }

            if (targets.Count > 1)
            {
                var reason = targets.Values.Any(target => target.AssignedToAnotherAgent)
                    ? "A contact already exists and is assigned to another agent."
                    : "The phone and email match different contacts.";
                row.Display = Invalid(row.Display, reason);
                row.Request = null;
                continue;
            }

            var target = targets.Values.FirstOrDefault();
            if (target is null)
            {
                target = new DuplicateTarget(null, row.Display.RowNumber, row.Display.Name, false);
                row.Display = row.Display with { WillCreateLead = row.ParcelId is not null };
            }
            else
            {
                row.Display = row.Display with
                {
                    Status = ContactImportRowStatus.Duplicate,
                    Reason = target.RowNumber is { } duplicateOf
                        ? $"Duplicate of CSV row {duplicateOf}."
                        : target.Name is null
                            ? target.AssignedToAnotherAgent
                                ? "A contact already exists and is assigned to another agent."
                                : "A contact already exists."
                            : $"Matches existing contact {target.Name}.",
                    DuplicateOfRow = target.RowNumber,
                    WillCreateLead = false
                };
            }

            if (row.Phone is not null)
                identityTargets["phone:" + row.Phone] = target;
            if (row.Email is not null)
                identityTargets["email:" + row.Email] = target;
        }
    }

    private static ServiceResult<ParsedCsv> ParseCsv(string csvContent)
    {
        try
        {
            using var parser = new TextFieldParser(new StringReader(csvContent))
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",");
            if (parser.EndOfData)
                return ServiceResult<ParsedCsv>.Fail("The CSV file is empty.");

            var header = parser.ReadFields();
            if (header is null || header.Length == 0)
                return ServiceResult<ParsedCsv>.Fail("The CSV file has no header row.");
            header[0] = header[0].TrimStart('\uFEFF');
            var columns = header.Select((name, index) =>
                new ImportCsvColumn(index, string.IsNullOrWhiteSpace(name) ? $"Column {index + 1}" : name.Trim())).ToList();
            var rows = new List<string[]>();
            while (!parser.EndOfData)
                rows.Add(parser.ReadFields() ?? []);

            return ServiceResult<ParsedCsv>.Success(new ParsedCsv(columns, rows));
        }
        catch (MalformedLineException ex)
        {
            return ServiceResult<ParsedCsv>.Fail($"CSV formatting is invalid near line {ex.LineNumber}.");
        }
    }

    private bool ConsentGiven(string[] fields, int? consentColumn) =>
        string.Equals(OptionalColumn(fields, consentColumn)?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);

    private static string? ValidateMapping(ContactImportMapping mapping, int columnCount, bool ignoreAgent)
    {
        if (!ValidColumn(mapping.NameColumn, columnCount))
            return "Map a CSV column to Name.";
        if (mapping.PhoneColumn is null && mapping.EmailColumn is null)
            return "Map at least one of Phone or Email.";
        if (mapping.PhoneColumn is { } phone && !ValidColumn(phone, columnCount)
            || mapping.EmailColumn is { } email && !ValidColumn(email, columnCount)
            || mapping.ConsentColumn is { } consent && !ValidColumn(consent, columnCount)
            || mapping.ParcelColumn is { } parcel && !ValidColumn(parcel, columnCount)
            || !ignoreAgent && mapping.AssignedAgentColumn is { } agent && !ValidColumn(agent, columnCount))
            return "One of the selected CSV columns is no longer available.";
        if (mapping.SourceColumn >= columnCount || mapping.SourceColumn < -1)
            return "The selected source column is no longer available.";

        var selected = new List<int> { mapping.NameColumn };
        if (mapping.PhoneColumn is { } phoneIndex) selected.Add(phoneIndex);
        if (mapping.EmailColumn is { } emailIndex) selected.Add(emailIndex);
        if (mapping.SourceColumn >= 0) selected.Add(mapping.SourceColumn);
        if (mapping.ConsentColumn is { } consentIndex) selected.Add(consentIndex);
        if (mapping.ParcelColumn is { } parcelIndex) selected.Add(parcelIndex);
        if (!ignoreAgent && mapping.AssignedAgentColumn is { } agentIndex) selected.Add(agentIndex);
        if (selected.Count != selected.Distinct().Count())
            return "Map each field to a different CSV column.";

        return null;
    }

    private static bool ValidColumn(int column, int count) => column >= 0 && column < count;

    private static LeadSource? ParseSource(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return LeadSource.Import;
        var token = new string(value.Where(char.IsLetterOrDigit).ToArray());
        foreach (var source in Enum.GetValues<LeadSource>())
        {
            var sourceToken = new string(source.ToString().Where(char.IsLetterOrDigit).ToArray());
            if (string.Equals(sourceToken, token, StringComparison.OrdinalIgnoreCase))
                return source;
        }
        return null;
    }

    private static string Column(string[] fields, int index) => index < fields.Length ? fields[index] : string.Empty;

    private static string? OptionalColumn(string[] fields, int? index) =>
        index is { } selected ? Column(fields, selected) : null;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContactImportRow Invalid(ContactImportRow row, string reason) =>
        row with { Status = ContactImportRowStatus.Invalid, Reason = reason, WillCreateLead = false };

    private static PreparedRow InvalidPrepared(ContactImportRow row, string reason) =>
        new(Invalid(row, reason), null, null, null, null);

    private static void AddTarget(Dictionary<string, DuplicateTarget> targets, DuplicateTarget target) =>
        targets[target.Key] = target;

    private bool CanReveal(Contact contact, ISet<Guid> visibleIds) =>
        visibleIds.Contains(contact.Id) && !access.IsAgentAssignedToAnotherStaff(contact.AssignedStaffId);

    private static string AppendWarning(string? current, string warning) =>
        string.IsNullOrWhiteSpace(current) ? warning : current + " " + warning;

    private sealed record ParsedCsv(IReadOnlyList<ImportCsvColumn> Columns, IReadOnlyList<string[]> Rows);
    private sealed record PreparedImport(IReadOnlyList<ImportCsvColumn> Columns, List<PreparedRow> Rows);
    private sealed record DuplicateTarget(Guid? ContactId, int? RowNumber, string? Name, bool AssignedToAnotherAgent)
    {
        public string Key => ContactId?.ToString("N") ?? $"row:{RowNumber}";
    }

    private sealed class PreparedRow(
        ContactImportRow display,
        CaptureContactRequest? request,
        Guid? parcelId,
        string? phone,
        string? email)
    {
        public ContactImportRow Display { get; set; } = display;
        public CaptureContactRequest? Request { get; set; } = request;
        public Guid? ParcelId { get; } = parcelId;
        public string? Phone { get; } = phone;
        public string? Email { get; } = email;
    }
}
