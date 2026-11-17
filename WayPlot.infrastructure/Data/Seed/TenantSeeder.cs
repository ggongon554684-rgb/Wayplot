using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure.Data.Seed;

/// <summary>
/// Demo data for a tenant database. Everything is written THROUGH the services, so the same rules that guard real
/// data (phone normalizing, duplicate check, one lead per contact and parcel, pipeline order) also guard the seed.
/// Run it after login: the rows are audited as the signed-in user.
/// </summary>
public sealed class TenantSeeder(
    ParcelService parcelService,
    ContactService contactService,
    LeadService leadService,
    ITenantDbContextFactory dbFactory,
    TenantSession session)
{
    // ---- Fixed demo data (hand-written, so every run is identical) ----

    private const double CenterLatitude = 14.58;
    private const double CenterLongitude = 121.17;
    private const double MetersPerDegreeLat = 111_000.0;
    private static readonly double MetersPerDegreeLon = 111_320.0 * Math.Cos(CenterLatitude * Math.PI / 180.0);

    private const LeadStage New = LeadStage.New;
    private const LeadStage Contacted = LeadStage.Contacted;
    private const LeadStage Viewing = LeadStage.Viewing;
    private const LeadStage Negotiation = LeadStage.Negotiation;
    private const LeadStage Lost = LeadStage.Lost;

    /// <summary>One status per parcel: 7 Available, 3 Reserved, 2 Sold.</summary>
    private static readonly ParcelStatus[] ParcelStatuses =
    [
        ParcelStatus.Available, ParcelStatus.Available, ParcelStatus.Reserved, ParcelStatus.Available,
        ParcelStatus.Sold,      ParcelStatus.Available, ParcelStatus.Reserved, ParcelStatus.Available,
        ParcelStatus.Available, ParcelStatus.Sold,      ParcelStatus.Reserved, ParcelStatus.Available
    ];

    /// <summary>Lead stages per parcel (30 leads). Consistent with the parcel status:
    /// Available has no Negotiation, Reserved has exactly one Negotiation, Sold has only Lost leads.</summary>
    private static readonly LeadStage[][] LeadPlan =
    [
        [Viewing, Contacted, New],          // 1  Available
        [Contacted, New],                   // 2  Available
        [Negotiation, Viewing, Lost],       // 3  Reserved
        [Viewing, New, New],                // 4  Available
        [Lost, Lost],                       // 5  Sold
        [Contacted, New, Lost],             // 6  Available
        [Negotiation, Contacted, Lost],     // 7  Reserved
        [Viewing, Contacted],               // 8  Available
        [New, Contacted, New],              // 9  Available
        [Lost, Lost],                       // 10 Sold
        [Negotiation, Viewing, Lost],       // 11 Reserved
        [New]                               // 12 Available
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

        if ((await parcelService.ListAsync()).Count > 0)
            return RuleResult.Success("Demo data skipped: this tenant already has parcels.");

        try
        {
            return await SeedCoreAsync(companyName);
        }
        catch (SeedFailure ex)
        {
            return RuleResult.Fail($"Demo data stopped early. {ex.Message} Use Reset demo data to start clean.");
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException)
        {
            return RuleResult.Fail("Demo data stopped early because the database refused a save. Use Reset demo data to start clean.");
        }
    }

    /// <summary>Clears this tenant's data (Staff and Settings stay) and seeds again. Destructive: the UI must confirm first.</summary>
    public async Task<RuleResult> ResetDemoDataAsync(string? companyName)
    {
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

    private async Task<RuleResult> SeedCoreAsync(string? companyName)
    {
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

            parcels.Add(Need(await parcelService.CreateAsync(request), $"Parcel {label}"));
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

                if (target != Lost && !contactsWithActiveLead.Contains(contactIndex))
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

    /// <summary>Moves a new lead (stage New) to its target one legal step at a time, so StageHistory is complete.</summary>
    private async Task AdvanceAsync(Guid leadId, LeadStage target, ParcelDto parcel, Random rnd)
    {
        switch (target)
        {
            case New:
                return;

            case Contacted:
                await MoveAsync(leadId, Contacted, "First call made.");
                return;

            case Viewing:
                await MoveAsync(leadId, Contacted, "First call made.");
                await MoveAsync(leadId, Viewing, "Site visit scheduled and done.",
                    offer: rnd.Next(0, 2) == 0 ? OfferFor(parcel, rnd) : null);
                return;

            case Negotiation:
                await MoveAsync(leadId, Contacted, "First call made.");
                await MoveAsync(leadId, Viewing, "Site visit done.");
                await MoveAsync(leadId, Negotiation, "Price discussion started.", offer: OfferFor(parcel, rnd));
                return;

            case Lost:
                await MoveAsync(leadId, Contacted, "First call made.");
                var reason = parcel.Status == ParcelStatus.Sold
                    ? "Parcel was sold to another buyer"
                    : LostReasons[rnd.Next(LostReasons.Length)];
                await MoveAsync(leadId, Lost, null, reason);
                return;
        }
    }

    private async Task MoveAsync(Guid leadId, LeadStage to, string? note, string? lostReason = null, decimal? offer = null)
    {
        var result = await leadService.MoveStageAsync(new MoveStageRequest(leadId, to, note, lostReason, offer));
        if (!result.Ok)
            throw new SeedFailure($"A lead could not move to {to}: {result.Message}");
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