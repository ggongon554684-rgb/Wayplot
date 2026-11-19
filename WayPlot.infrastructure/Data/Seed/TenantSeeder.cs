using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure.Data.Seed;

/// <summary>
/// Demo data for a tenant database. System-owned seed rows are written through the unit of work; user-facing
/// contact and lead workflows still use their services. Run it after login so audit fields identify the signed-in user.
/// </summary>
public sealed class TenantSeeder(
    ParcelService parcelService,
    ContactService contactService,
    LeadService leadService,
    ITenantUnitOfWorkFactory uowFactory,
    ITenantDbContextFactory dbFactory,
    TenantSession session,
    SessionContext userSession,
    TenantAccessPolicy access,
    IPipelineStageProvider pipelineStages,
    Func<bool> isDevelopment)
{
    // ---- Fixed demo data (hand-written, so every run is identical) ----

    private const string DemoSeedStateKey = "demo.seed-state";
    private const string DemoSeedInProgress = "InProgress";
    private const string DemoSeedComplete = "Complete";

    private const double CenterLatitude = 14.58;
    private const double CenterLongitude = 121.17;
    private const double MetersPerDegreeLat = 111_000.0;
    private static readonly double MetersPerDegreeLon = 111_320.0 * Math.Cos(CenterLatitude * Math.PI / 180.0);

    private enum SeedLeadPosition { New, First, Middle, Closing, Lost }

    /// <summary>One status per parcel: 7 Available, 3 Reserved, 2 Sold.</summary>
    private static readonly ParcelStatus[] ParcelStatuses =
    [
        ParcelStatus.Available, ParcelStatus.Available, ParcelStatus.Reserved, ParcelStatus.Available,
        ParcelStatus.Sold,      ParcelStatus.Available, ParcelStatus.Reserved, ParcelStatus.Available,
        ParcelStatus.Available, ParcelStatus.Sold,      ParcelStatus.Reserved, ParcelStatus.Available
    ];

    /// <summary>Lead positions per parcel (30 leads). Reserved parcels have one closing-stage lead.</summary>
    private static readonly SeedLeadPosition[][] LeadPlan =
    [
        [SeedLeadPosition.Middle, SeedLeadPosition.First, SeedLeadPosition.New],
        [SeedLeadPosition.First, SeedLeadPosition.New],
        [SeedLeadPosition.Closing, SeedLeadPosition.Middle, SeedLeadPosition.Lost],
        [SeedLeadPosition.Middle, SeedLeadPosition.New, SeedLeadPosition.New],
        [SeedLeadPosition.Lost, SeedLeadPosition.Lost],
        [SeedLeadPosition.First, SeedLeadPosition.New, SeedLeadPosition.Lost],
        [SeedLeadPosition.Closing, SeedLeadPosition.First, SeedLeadPosition.Lost],
        [SeedLeadPosition.Middle, SeedLeadPosition.First],
        [SeedLeadPosition.New, SeedLeadPosition.First, SeedLeadPosition.New],
        [SeedLeadPosition.Lost, SeedLeadPosition.Lost],
        [SeedLeadPosition.Closing, SeedLeadPosition.Middle, SeedLeadPosition.Lost],
        [SeedLeadPosition.New]
    ];

    private static readonly string[] ContactNames =
    [
        "Maria Santos", "Jose Reyes", "Ana Dizon", "Mark Anthony Cruz", "Liza Mendoza", "Paolo Bautista",
        "Grace Ramos", "Joel Navarro", "Cristina Aquino", "Ramon Castillo", "Bea Fernandez", "Noel Villareal",
        "Rina Soriano", "Allan Pascual", "Joy Manalo", "Dennis Ocampo", "Kristine Alvarez", "Rey Domingo",
        "Camille Tolentino", "Victor Salazar", "Jasmine Hernandez", "Edgar Gonzales", "Mylene Lim", "Arnel Padilla"
    ];

    private static readonly string[] PhonePrefixes = ["0917", "0918", "0927", "0905", "0956", "0998"];
    private static readonly LeadSource[] Sources = Enum.GetValues<LeadSource>();

    private static readonly string[] Descriptions =
    [
        "Corner lot facing the main access road",
        "Interior lot on a quiet street",
        "Near the subdivision clubhouse",
        "Slightly elevated lot with a good view",
        "Rectangular lot, ready for building",
        "Beside the planned neighborhood park"
    ];

    private static readonly string[] UtilitiesOptions =
    [
        "Water, electricity, internet",
        "Water and electricity",
        "Water, electricity, drainage",
        "Electricity only"
    ];

    private static readonly string[] RoadOptions =
    [
        "Concrete road, 6 m wide",
        "Concrete road, 8 m wide",
        "Gravel road, 6 m wide",
        "Barangay road, 5 m wide"
    ];

    private static readonly string[] LostReasons =
    [
        "Bought another lot", "Price was too high", "Financing fell through", "No longer interested"
    ];

    // ---- Public API ----

    /// <summary>Seeds demo data only when Parcels is empty. Safe to call after every login.</summary>
    public async Task<RuleResult> SeedAsync(string? companyName)
    {
        if (!session.IsStarted)
            return RuleResult.Fail("Sign in first. The seeder needs an open tenant database.");

        try
        {
            await using var lockContext = await AcquireSeedLockAsync();
            var state = await lockContext.Settings.AsNoTracking()
                .Where(setting => setting.Key == DemoSeedStateKey)
                .Select(setting => setting.Value)
                .FirstOrDefaultAsync();
            var parcelCount = await lockContext.Parcels.CountAsync();

            if (state == DemoSeedInProgress)
                return RuleResult.Fail("A previous demo seed stopped partway through. Use Reset demo data before trying again.");
            if (parcelCount > 0)
                return RuleResult.Success("Demo data skipped: this tenant already has parcels.");

            await SetDemoSeedStateAsync(lockContext, DemoSeedInProgress);
            if (userSession.Tier == Tier.C)
                await EnsureTierCBranchesAsync();

            var seeded = await SeedCoreAsync(companyName);
            if (!seeded.Ok)
                return seeded;
            await SetDemoSeedStateAsync(lockContext, DemoSeedComplete);
            return seeded;
        }
        catch (SeedFailure ex)
        {
            return RuleResult.Fail($"Demo data stopped early. {ex.Message} Use Reset demo data to start clean.");
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException)
        {
            return RuleResult.Fail("Demo data stopped early because the database refused a save. Use Reset demo data to start clean.");
        }
        catch (Exception)
        {
            return RuleResult.Fail("Demo data could not be prepared. Use Reset demo data to start clean.");
        }
    }

    /// <summary>Clears this tenant's data (Staff and Settings stay) and seeds again. Destructive: the UI must confirm first.</summary>
    public async Task<RuleResult> ResetDemoDataAsync(string? companyName)
    {
        if (!isDevelopment())
            return RuleResult.Fail("Demo data reset is available only in Development.");

        var permission = access.RequireTenantAdmin("reset demo data");
        if (!permission.Ok)
            return RuleResult.Fail(permission.Message);

        if (!session.IsStarted)
            return RuleResult.Fail("Sign in first. The seeder needs an open tenant database.");

        try
        {
            await using (var db = dbFactory.Create(session.ConnectionString))
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                // Children first (no cascading deletes). IgnoreQueryFilters so soft-deleted rows go too.
                await db.Payments.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Commissions.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.ReferralBonuses.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Documents.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.LandownerPayouts.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Deals.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.StageHistories.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Interactions.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Leads.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.ParcelPhotos.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Parcels.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Contacts.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Owners.IgnoreQueryFilters().ExecuteDeleteAsync();
                await db.Outbox.ExecuteDeleteAsync();
                await db.Settings.IgnoreQueryFilters()
                    .Where(setting => setting.Key == DemoSeedStateKey)
                    .ExecuteDeleteAsync();

                await tx.CommitAsync();
            }
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException)
        {
            return RuleResult.Fail("Could not clear the tenant data. Nothing was changed.");
        }

        return await SeedAsync(companyName);
    }

    // ---- Seeding steps ----

    private async Task EnsureTierCBranchesAsync()
    {
        await using var db = dbFactory.Create(session.ConnectionString);
        await TenantBranchProvisioner.EnsureTierCAsync(db);
    }

    private async Task<TenantDbContext> AcquireSeedLockAsync()
    {
        var db = dbFactory.Create(session.ConnectionString);
        try
        {
            await db.Database.OpenConnectionAsync();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandTimeout = 75;
            command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock " +
                "@Resource = @resource, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 60000; " +
                "SELECT @result;";
            System.Data.Common.DbParameter resource = command.CreateParameter();
            resource.ParameterName = "@resource";
            resource.Value = $"WayPlotDemoSeed:{userSession.TenantId ?? Guid.Empty:D}";
            command.Parameters.Add(resource);
            var result = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
            if (result < 0)
                throw new InvalidOperationException("Could not acquire the tenant demo seed lock.");
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }

    private static async Task SetDemoSeedStateAsync(TenantDbContext db, string state)
    {
        var setting = await db.Settings.FirstOrDefaultAsync(item => item.Key == DemoSeedStateKey);
        if (setting is null)
            db.Settings.Add(new Setting { Key = DemoSeedStateKey, Value = state });
        else
            setting.Value = state;
        await db.SaveChangesAsync();
    }

    private async Task<RuleResult> SeedCoreAsync(string? companyName)
    {
        await pipelineStages.EnsureLoadedAsync();
        var rnd = new Random(2026);

        // 1. Three owners: the company itself and two landowners.
        var ownerRequests = new[]
        {
            new CreateOwnerRequest(string.IsNullOrWhiteSpace(companyName) ? "The company" : companyName.Trim(),
                "0917 555 0100", null, false),
            new CreateOwnerRequest("Rosario Dela Cruz", "0917 555 0141", "rosario.delacruz@example.com", true),
            new CreateOwnerRequest("Eduardo Villanueva", "0928 555 0172", null, true)
        };

        var owners = new List<OwnerDto>();
        foreach (var request in ownerRequests)
            owners.Add(Need(await parcelService.CreateOwnerAsync(request), $"Owner {request.Name}"));

        // 2. Twelve parcels in a 4 x 3 grid of lots around the center point.
        var parcels = new List<ParcelDto>();
        for (var i = 0; i < ParcelStatuses.Length; i++)
        {
            var column = i % 4;
            var row = i / 4;

            var area = 250m + rnd.Next(0, 31) * 5;                                   // 250 to 400 sq m
            var zoning = rnd.Next(0, 2) == 0 ? "R-1" : "R-2";
            var perSqm = 8_500 + rnd.Next(0, 11) * 500;
            var price = Math.Round(area * perSqm / 1000m, MidpointRounding.AwayFromZero) * 1000m;
            var fee = Math.Round(price * (rnd.Next(0, 2) == 0 ? 0.03m : 0.05m) / 1000m, MidpointRounding.AwayFromZero) * 1000m;

            var width = Math.Min(20.0, Math.Max(16 + rnd.Next(0, 5), (double)area / 23.0));
            var depth = (double)area / width;
            var geoJson = Polygon(rnd, -52 + column * 26, -45 + row * 30, width, depth);

            var label = $"Blk {row + 1} Lot {column + 1}";
            var request = new CreateParcelRequest(
                label,
                Descriptions[rnd.Next(Descriptions.Length)],
                area, zoning, price, fee,
                geoJson, null, null,
                $"TD-2026-{1000 + i * 17:0000}",
                $"TCT-{210000 + i * 331}",
                UtilitiesOptions[rnd.Next(UtilitiesOptions.Length)],
                RoadOptions[rnd.Next(RoadOptions.Length)],
                owners[i % owners.Count].Id,
                ParcelStatuses[i]);

            parcels.Add(await CreateSeedParcelAsync(request));
        }

        // 3. Twenty-four contacts, with mixed consent, sources and types. Two are email-only.
        var contacts = new List<SeedContact>();
        var usedPhones = new HashSet<string>();
        for (var i = 0; i < ContactNames.Length; i++)
        {
            var name = ContactNames[i];
            var type = i % 6 == 5 ? ContactType.Landowner : i % 6 == 3 ? ContactType.Client : ContactType.Customer;
            var emailOnly = i is 4 or 16;

            string? phone = null;
            if (!emailOnly)
            {
                string digits;
                do { digits = PhonePrefixes[rnd.Next(PhonePrefixes.Length)] + rnd.Next(1_000_000, 10_000_000); }
                while (!usedPhones.Add(digits));
                phone = $"{digits[..4]} {digits[4..7]} {digits[7..]}";
            }

            var hasEmail = emailOnly || i % 3 != 1;
            var parts = name.Split(' ');
            var email = hasEmail ? $"{parts[0]}.{parts[^1]}@example.com".ToLowerInvariant() : null;

            var consentSms = phone is not null && i % 4 < 2;
            var consentEmail = hasEmail && i % 2 == 0;
            var source = Sources[rnd.Next(Sources.Length)];

            var captured = Need(
                await contactService.CaptureAsync(new CaptureContactRequest(
                    name, phone, email, source, type, consentSms, consentEmail)),
                $"Contact {name}");

            if (captured.MatchedExisting)
                throw new SeedFailure($"{name} was unexpectedly matched to an existing contact.");

            contacts.Add(new SeedContact(captured.ContactId, name, phone, type));
        }

        // 4. Thirty leads. Landowners are not buyers, so leads come from the other 20 contacts.
        var pool = Enumerable.Range(0, contacts.Count).Where(i => contacts[i].Type != ContactType.Landowner).ToList();
        for (var n = pool.Count - 1; n > 0; n--)
        {
            var k = rnd.Next(n + 1);
            (pool[n], pool[k]) = (pool[k], pool[n]);
        }

        var slot = 0;
        var leadCount = 0;
        var contactsWithActiveLead = new List<int>();

        for (var p = 0; p < parcels.Count; p++)
        {
            foreach (var target in LeadPlan[p])
            {
                var contactIndex = pool[slot++ % pool.Count];   // a parcel never reaches the same contact twice
                var contact = contacts[contactIndex];

                var lead = Need(
                    await leadService.AttachContactToParcelAsync(new AttachLeadRequest(contact.Id, parcels[p].Id)),
                    $"Lead for {contact.FullName} on {parcels[p].Label}");
                leadCount++;

                await AdvanceAsync(lead.Id, target, parcels[p], rnd);

                if (target != SeedLeadPosition.Lost && !contactsWithActiveLead.Contains(contactIndex))
                    contactsWithActiveLead.Add(contactIndex);
            }
        }

        // 5. Prove the duplicate check: the same person comes back with the phone typed in another format.
        //    The service must NOT create a contact; it adds the inquiry to the existing lead's history.
        var duplicateSources = new[] { LeadSource.PhoneCall, LeadSource.WalkIn, LeadSource.Facebook };
        var duplicateCandidates = contactsWithActiveLead.Where(i => contacts[i].Phone is not null).Take(3).ToList();
        for (var d = 0; d < duplicateCandidates.Count; d++)
        {
            var original = contacts[duplicateCandidates[d]];
            var digits = PhoneNormalizer.Normalize(original.Phone)!;

            var retyped = d switch
            {
                0 => $"+63 {digits[1..4]} {digits[4..7]} {digits[7..]}",
                1 => digits[1..],
                _ => $"{digits[..4]}-{digits[4..7]}-{digits[7..]}"
            };

            var parts = original.FullName.Split(' ', 2);
            var nameVariant = parts.Length == 2 ? $"{parts[0][0]}. {parts[1]}" : original.FullName;

            var again = Need(
                await contactService.CaptureAsync(new CaptureContactRequest(
                    nameVariant, retyped, null, duplicateSources[d], ContactType.Customer, false, false,
                    "Asked again about the price and the viewing schedule.")),
                $"Duplicate capture for {original.FullName}");

            if (!again.MatchedExisting || !again.InquiryRecorded || again.ContactId != original.Id)
                throw new SeedFailure($"The duplicate check did not match {original.FullName}.");
        }

        return RuleResult.Success(
            $"Demo data created: {owners.Count} owners, {parcels.Count} parcels, {contacts.Count} contacts, " +
            $"{leadCount} leads, {duplicateCandidates.Count} duplicate captures merged.");
    }

    private async Task<ParcelDto> CreateSeedParcelAsync(CreateParcelRequest request)
    {
        await using var uow = uowFactory.Create();
        var owner = await uow.Owners.GetAsync(request.OwnerId)
            ?? throw new SeedFailure($"Owner for parcel {request.Label} was not found.");

        var parcel = new Parcel
        {
            Label = request.Label.Trim(),
            Description = request.Description.Trim(),
            AreaSqm = request.AreaSqm,
            Zoning = request.Zoning.Trim(),
            Price = request.Price,
            ReservationFee = request.ReservationFee,
            Status = request.Status,
            GeoJson = request.GeoJson,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TaxDeclarationNo = request.TaxDeclarationNo.Trim(),
            TitleNo = request.TitleNo.Trim(),
            Utilities = request.Utilities.Trim(),
            RoadAccess = request.RoadAccess.Trim(),
            OwnerId = owner.Id,
            Owner = owner,
            AssignedStaffId = userSession.Role == Roles.Agent ? userSession.UserId : request.AssignedStaffId,
            BranchId = userSession.BranchId
        };

        uow.Parcels.Add(parcel);
        await uow.SaveAsync();

        var pricePerSqm = parcel.AreaSqm > 0 ? Math.Round(parcel.Price / parcel.AreaSqm, 2) : 0m;
        var reservationPercent = parcel.Price > 0
            ? Math.Round(parcel.ReservationFee / parcel.Price * 100m, 2)
            : 0m;
        return new ParcelDto(
            parcel.Id, parcel.Label, parcel.Description, parcel.AreaSqm, parcel.Zoning, parcel.Price, pricePerSqm,
            parcel.ReservationFee, reservationPercent, parcel.Status, parcel.GeoJson, parcel.Latitude, parcel.Longitude,
            parcel.TaxDeclarationNo, parcel.TitleNo, parcel.Utilities, parcel.RoadAccess,
            owner.Id, owner.Name, owner.Phone, owner.Email, owner.ListingAgreementOnFile, parcel.AssignedStaffId);
    }

    /// <summary>Moves a new lead (stage New) to its target one legal step at a time, so StageHistory is complete.</summary>
    private async Task AdvanceAsync(Guid leadId, SeedLeadPosition target, ParcelDto parcel, Random rnd)
    {
        if (target == SeedLeadPosition.New)
            return;

        var activeStages = pipelineStages.GetConfiguration()
            .Where(stage => stage.IsActive)
            .OrderBy(stage => stage.SortOrder)
            .ToList();
        if (target == SeedLeadPosition.Lost)
        {
            if (activeStages[0].Stage != LeadStage.Negotiation)
                await MoveAsync(leadId, activeStages[0].Stage, "First call made.");
            var reason = parcel.Status == ParcelStatus.Sold
                ? "Parcel was sold to another buyer"
                : LostReasons[rnd.Next(LostReasons.Length)];
            await MoveAsync(leadId, LeadStage.Lost, null, reason);
            return;
        }

        var targetIndex = target switch
        {
            SeedLeadPosition.First => activeStages.Count == 1
                && activeStages[0].Stage == LeadStage.Negotiation ? -1 : 0,
            SeedLeadPosition.Middle => activeStages.Count < 2 ? -1 : 1,
            SeedLeadPosition.Closing => activeStages.Count - 1,
            _ => throw new InvalidOperationException("The demo lead position is not supported.")
        };
        if (targetIndex < 0)
            return;
        for (var index = 0; index <= targetIndex; index++)
        {
            var isTarget = index == targetIndex;
            var note = isTarget
                ? target switch
                {
                    SeedLeadPosition.First => "First call made.",
                    SeedLeadPosition.Middle => "Site visit scheduled and done.",
                    SeedLeadPosition.Closing => "Price discussion started.",
                    _ => "Lead progressed."
                }
                : index == 0 ? "First call made."
                : index == 1 ? "Site visit done."
                : $"Progressed to {activeStages[index].Name}.";
            decimal? offer = isTarget && target == SeedLeadPosition.Closing
                ? OfferFor(parcel, rnd)
                : isTarget && target == SeedLeadPosition.Middle && rnd.Next(0, 2) == 0
                    ? OfferFor(parcel, rnd)
                    : null;
            await MoveAsync(leadId, activeStages[index].Stage, note, offer: offer);
        }
    }

    private async Task MoveAsync(Guid leadId, LeadStage to, string? note, string? lostReason = null, decimal? offer = null)
    {
        var result = await leadService.MoveStageAsync(new MoveStageRequest(leadId, to, note, lostReason, offer));
        if (!result.Ok)
            throw new SeedFailure(
                $"A lead could not move to {pipelineStages.GetDisplayName(to)}: {result.Message}");
    }

    /// <summary>An offer between 90% and 98% of the price, rounded to the nearest thousand.</summary>
    private static decimal OfferFor(ParcelDto parcel, Random rnd) =>
        Math.Round(parcel.Price * (0.90m + rnd.Next(0, 9) / 100m) / 1000m, MidpointRounding.AwayFromZero) * 1000m;

    // ---- Helpers ----

    /// <summary>Unwraps a successful service result, or stops the seed with the service's own message.</summary>
    private static T Need<T>(ServiceResult<T> result, string what) where T : class =>
        result.Ok && result.Value is not null
            ? result.Value
            : throw new SeedFailure($"{what}: {result.Message}");

    /// <summary>A lot-sized rectangle with slightly uneven corners, as GeoJSON. x and y are meters from the center point.</summary>
    private static string Polygon(Random rnd, double x, double y, double width, double depth)
    {
        (double X, double Y)[] corners = [(x, y), (x + width, y), (x + width, y + depth), (x, y + depth)];

        var ring = new List<string>();
        foreach (var (cx, cy) in corners)
        {
            var jitterX = (rnd.NextDouble() - 0.5) * 1.2;
            var jitterY = (rnd.NextDouble() - 0.5) * 1.2;
            ring.Add(Coordinate(cx + jitterX, cy + jitterY));
        }
        ring.Add(ring[0]);   // a GeoJSON ring is closed

        return "{\"type\":\"Polygon\",\"coordinates\":[[" + string.Join(",", ring) + "]]}";
    }

    private static string Coordinate(double xMeters, double yMeters)
    {
        var longitude = CenterLongitude + xMeters / MetersPerDegreeLon;
        var latitude = CenterLatitude + yMeters / MetersPerDegreeLat;
        return "[" + longitude.ToString("F6", CultureInfo.InvariantCulture) + ","
                   + latitude.ToString("F6", CultureInfo.InvariantCulture) + "]";
    }

    private sealed record SeedContact(Guid Id, string FullName, string? Phone, ContactType Type);

    /// <summary>Internal flow control only. SeedAsync turns it into a RuleResult, so callers never see an exception.</summary>
    private sealed class SeedFailure(string message) : Exception(message);
}
