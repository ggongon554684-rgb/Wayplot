# DATA AND STARTUP code review

Reviewed commit: `3fc82a1`

## Findings

### STARTUP-01 — Major

**Evidence**

[TenantSeeder.cs](../../WayPlot.infrastructure/Data/Seed/TenantSeeder.cs), `SeedAsync`:

> 118: `var parcels = await parcelService.ListAsync();`
> 121: `if (parcels.Value.Count > 0)`
> 122: `    return RuleResult.Success("Demo data skipped: this tenant already has parcels.");`
> 124: `try`
> 126: `    return await SeedCoreAsync(companyName);`

`SeedCoreAsync` creates owners and parcels through separate service/unit-of-work saves; the parcel labels are fixed (`Blk 1 Lot 1`, etc.).

**Rule broken:** `TenantSeeder.SeedAsync` XML contract at line 109: “Safe to call after every login.”

**What goes wrong:** Two first logins can both see zero parcels and enter `SeedCoreAsync`. Each can create the same three owners before either reaches the first parcel. One caller then inserts the first fixed-label parcel; the other collides on that label and returns a failure after its owners were already saved. The surviving tenant can therefore have duplicate seeded owners. If the winning caller also fails later, the loser’s next login sees at least one parcel and reports seeding skipped, leaving an incomplete demo dataset that will not self-repair.

**Suggested fix:** Serialize seed attempts per tenant or perform the empty check and all inserts as one atomic/idempotent operation. Ensure a failed partial run remains detectable and can be resumed safely rather than treating any parcel as proof that seeding completed.

**Confidence:** High.

## Schema, migration, and startup review

- **Commit / migration inventory:** reviewed commit `3fc82a1`. Tenant migration IDs are `20261116101911_Init_Tenant`, `20261117110000_AddDuplicateInquiries`, `20261117123239_RunA_StaffBranchesAndIntegrity`, `20261117130000_RunA_StaffForeignKeys`, `20261118053644_AddParcelPhotoData`, and `20261118090246_AddDealTermination`. The migrator’s expected-last ID matches the current final tenant migration. The staff provisioning boundary is Part A (`RunA_StaffBranchesAndIntegrity`); staff reconciliation runs before remaining pending migrations, including Part B staff FKs.
- **Model and snapshots:** current tenant/master context configuration and the last tenant/master snapshots were inspected. Persisted enum properties in the tenant model use string conversions (including payment/deal/payout statuses); Master `Tier`, `TenantStatus`, and `SubscriptionStatus` use string conversions. DTO-only enums and commission rule enums stored in JSON are not EF columns. Decimal conventions are `decimal(18,2)` in both contexts, including explicit termination refund precision. Tenant migration designers carry matching string/decimal column types.
- **Down methods and destructive changes:** each non-designer migration has a `Down`. The initial migrations drop their created tables. The duplicate inquiry migration drops its table; Part A removes the Branch table and introduced constraints/indexes; Part B drops its FKs; photo data drops its data table; termination drops its five added columns. Reverting those migrations can discard the corresponding newer data, as expected. No unintended destructive operation in an `Up` method was found. `TenantSeeder.ResetDemoDataAsync` performs explicit physical deletes in child-to-parent order inside a transaction and is deliberately destructive.
- **Foreign keys and indexes:** model-wide tenant FK behavior is Restrict; explicit parcel/photo, assignment and deal relationships agree. Reviewed uniqueness includes tenant database name, active branch name, parcel label, normalized non-deleted contact phone, payment receipt, active deal per parcel, branch name, and one-to-one photo data. Part A drops/recreates the active-deal and negotiation indexes for the new rules. No duplicate or missing model/migration index mismatch was established from source inspection.
- **Migrator recovery:** it skips Suspended tenants, validates migration discovery, migrates each tenant independently, marks a failed tenant `Failed`, and continues to later tenants before saving master status. Failed tenants are reconsidered on the next run because only Suspended is excluded. The boundary/provision/rest sequence supports retry after partial progress; no migration is marked complete until EF succeeds. `SchemaVersion` is the applied migration count, not a migration ID, and is updated only after the expected last migration is present and pending migrations are empty. No failure-path defect was established.
- **Provisioning:** staff rows reconcile from Master login IDs and roles, including soft-deleted rows; existing matching rows are reactivated. Tier C branches are re-enabled/created and null branch assignments for the demo manager/Ana are filled. Existing non-null assignments are preserved; unexpected assignments fail provisioning rather than being overwritten. Repeated successful calls are idempotent. Provisioning itself uses multiple saves (branches then assignments), so an error can leave the first save committed; retry is safe for these operations.
- **Seed assignments and parcel/deal consistency:** Agent-created seed parcels are assigned to the signed-in Agent; Admin seeding follows parcel service request assignment behavior; branch is taken from session. Tier C branch provisioning assigns the seeded manager/Ana only when their branch is null. Demo data intentionally seeds two Sold parcels with only Lost leads and no Deals; the seeder comments make this explicit, but this does not model a confirmed sale under the product lifecycle (“Closed confirmation” creates the deal). Treat as a demo-fixture fidelity question, not a data-loss defect.
- **Secrets and login:** appsettings contain only a local trusted-connection string and local API URL; no committed API key or production password found. `MasterSeeder.DemoPassword` is committed but the only startup call to that seeder is guarded by Development. It uses `UserManager.CreateAsync(user, password)`, so Identity hashes the password; failures are not checked/reported in the seeder, but no plaintext password is stored. Login returns the same Unauthorized text for unknown, inactive, locked, and wrong-password cases. Identity access-failure tracking and configured five-failure/five-minute lockout are present. No rate limiter or CORS policy/middleware was found; the reviewed API is a local client API and appsettings does not enable cross-origin clients. No user-enumeration message difference was found.
- **Rules JSON:** `CommissionService` serializes rules via `System.Text.Json`; enum `PayableTiming` is therefore stored numerically by default. Its reader accepts valid numeric or string forms and selects the latest history item. This is internally consistent; no mismatch found.

## Checked and found fine

- Expected migration ID and staff Part A boundary match the checked-in migration sequence.
- Migration `Down` methods are present; normal `Up` methods showed no unexpected table/data destruction.
- Persisted enum values are converted to strings; rule JSON enum values round-trip as numbers.
- Decimal money columns use precision 18,2 in context conventions and migration definitions.
- Tenant migration errors are isolated per tenant and retried on later startup; schema version advances only after full completion.
- Login errors do not reveal whether a username exists, password creation uses Identity hashing, and development demo login seeding is environment-gated.

## Questions

- Should demo Sold parcels be represented by confirmed deals and Closed leads, or are they intentionally inventory-only fixtures? Current seed data has no deal for those parcels.
- Is a specific CORS policy needed for future browser clients? None is configured now; no browser client requirement was found in the reviewed startup files.

## Could not verify without running the app

- Runtime EF model validation and generated SQL against a real database, including snapshot equivalence.
- Concurrent migration/startup behavior with two API processes sharing the same Master and tenant databases.
- The concurrent first-login seeding race’s exact interleaving against SQL Server; the partial-save race is established from code structure, but its occurrence timing depends on concurrent execution.
