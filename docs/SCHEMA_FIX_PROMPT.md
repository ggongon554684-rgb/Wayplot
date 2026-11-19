HISTORICAL: written before Runs A, E, C2, C3, B2, B2b, B3, AD1, AD2 and S1. Do not use as current status. See docs/tests/ and docs/DESIGN_CONTRACT.md.
Superseded by runs S1 onward. Branch Manager parcel mutation in this file is WRONG: AGENTS.md refuses all Branch Manager mutations.

# Ready-to-run prompt: WayPlot schema and parcel-permission fixes

You are working in the WayPlot repository. Apply the fixes identified in `docs/SCHEMA_AUDIT.md`, in severity order. Before editing, read that audit plus `WayPlot_CRM_Documentation_v3txt.txt`, `wayplot_context.txt`, `wayplot_ui_context.txt`, `docs/TESTING.md`, both EF contexts, every entity/configuration, the tenant/master migrations, all services, presenters/views, and both seeders.

## Safety and project constraints

- Work only in this repository.
- Do not touch a real or development database. Do not start or stop the API.
- Use Designer-based WinForms layouts. Do not hand-edit generated `.Designer.cs` files except through the normal WinForms designer workflow or the existing project convention.
- Add no NuGet packages.
- Build with `dotnet build WayPlot\\WayPlot.csproj`.
- Use one migration per logical group, additive where possible. Before each new constraint or unique index, add/execute a data-check plan in the migration or a safe diagnostic script/test path that reports violating existing rows. Do not silently delete or rewrite business history.
- For every violation, define a deterministic plan: repair, quarantine, leave the constraint deferred, or stop with a clear operator message. Document the plan in the migration comments and final report.
- Do not add Staff foreign keys until the seeder/provisioning path guarantees a `Staffs` row for every tenant login. First make that guarantee, then reconcile existing tenant rows, then add constraints.
- Do not apply fixes outside the source/migration/docs changes required below. At the end report every changed file.

## Required decisions before implementation

1. Confirm removal of `UX_Lead_OneNegotiationPerParcel`. The product documentation allows several buyers to negotiate for one lot; an active deal should be the exclusivity guard. If retained, explain the business requirement and update documentation/tests.
2. Confirm the open parcel-edit rule: should an Agent edit only parcels they created, only parcels assigned to them, or either? The requested policy currently says “their own or assigned parcels”; preserve this as an explicit decision/configuration, not an assumption.
3. Decide whether tenant branch identity is authoritative in tenant `Staff.BranchId` or master `AspNetUsers.BranchId`. Implement one source of truth and migrate the other; never leave the two silently divergent.
4. Confirm whether contact email must be unique per tenant after normalization. The recommended answer is yes, with a filtered unique index for non-null, non-deleted normalized email.
5. Confirm whether `AreaSqm` is entered authoritative metadata or must be calculated/validated from GeoJSON.

## Phase 1 — high-severity integrity and authorization

### 1. Provision staff before constraints

- Update tenant provisioning/seeding so every tenant-scoped master login gets exactly one active tenant `Staff` row before normal tenant operations. Preserve `Staff.Id = AppUser.Id` if that remains the intended mapping.
- Include existing tenants and re-login cases; handle soft-deleted/inactive rows explicitly.
- Ensure branch assignment is populated from the single authoritative source.
- Add a read-only reconciliation/data-check routine that reports: tenant logins missing Staff rows, duplicate `Staff.UserId`, Staff rows whose role/name/username disagree, inactive/deleted matches, and branch references with no branch row.
- Only after this guarantee and a clean or explicitly repaired dataset add tenant-local FKs from actor columns to `Staffs.Id`. Do not attempt a cross-database FK to Master `AspNetUsers`.

### 2. Add branch model and integrity

- Add a tenant `Branches` entity/table with a stable GUID key and required tenant-appropriate name fields, following existing audit/soft-delete conventions.
- Add `Staff.BranchId -> Branches.Id` with the chosen nullability and delete behavior.
- Decide which records must have branch scope in Tier C and enforce that policy in services; do not add FKs to `BaseEntity.BranchId` blindly if Tier A/B rows are legitimately null.
- Add indexes for branch filters used by role-scoped lists.

### 3. Remove the negotiation uniqueness trap

- If decision 1 confirms removal, create an additive migration that drops `UX_Lead_OneNegotiationPerParcel` and updates the model snapshot/configuration.
- Retain or repair `UX_Deal_OneActivePerParcel` as the race guard, explicitly deciding whether soft-deleted rows should participate.
- Add concurrent-service tests for multiple Negotiation leads and one active deal.

### 4. Make Deal relationships consistent

- Before adding a constraint, inspect existing Deals for mismatches between `Deal.LeadId` and the Lead's `ContactId`/`ParcelId`, and for missing references.
- Prefer making Lead the authoritative source. If removing `Deal.ContactId`/`Deal.ParcelId` is too disruptive, retain them only as immutable snapshots and enforce equality at creation/confirmation plus a migration-time check. Do not add a database FK that merely proves each ID exists; that does not prevent drift.
- Keep transaction terms such as price, reservation fee, discount, and down payment as deliberate snapshots.
- Keep balance computed from confirmed payments. Centralize the balance/allocated/reservation calculation so displays, overpayment checks, and status transitions use the same definition.

### 5. Implement parcel service authorization and lifecycle

Add service-layer methods for create, edit, price change, reassignment, review/approval, archive, photo changes, and system status transitions. The exact policy is mandatory:

- Plot a new parcel (label, price, description, photos, owner): Agent and Tenant Admin; Branch Manager only within their own branch (Tier C); never Super Admin.
- Edit a parcel: Tenant Admin any parcel; Agent only their own or assigned parcels (flag this as an open decision for the maintainer to confirm); Branch Manager only their branch's parcels.
- Edit price of an existing parcel: Tenant Admin only.
- Reassign a parcel: Tenant Admin only.
- Review or approve a plotted parcel: Tenant Admin.
- Delete: never hard-delete a parcel referenced by any Lead or Deal. Use archive/soft-delete, Tenant Admin only. Preserve photos/history according to existing restricted-FK behavior.
- Status `Available`, `Reserved`, `Sold`, `Leased`: system-driven only through deal/payment events. No role may set status by hand. A new plot must use the defined initial/pending state rather than accepting arbitrary status from a request.
- Enforce all of the above in services before updating the UI.
- Hide or mute matching controls with the shared muted-button helper after service enforcement. UI visibility is not security.

Implement the missing cancellation/default behavior consistently: recompute parcel state from active confirmed deals/payments and define what happens to returned/refunded payments. Do not claim status changed if the service did not change it.

## Phase 2 — medium-severity constraints and query support

### 6. Contact uniqueness

- Run a data check for duplicate normalized emails among non-deleted contacts, treating null/blank as absent.
- If confirmed, add normalized email storage/normalization and a filtered unique index, with a plan for existing duplicates before adding it.
- Keep phone uniqueness semantics explicit and test phone OR email duplicate capture under concurrency.

### 7. Actor and relationship indexes

Add only indexes supported by current query paths and document why:

- `Leads(AssignedStaffId)` and nonunique `Leads(ParcelId)` if needed after dropping the filtered unique index.
- `Commissions(RecipientStaffId)`.
- `Contacts(AssignedStaffId)`, `Parcels(AssignedStaffId)`, and deal/payment actor columns where role-scoped queries use them.
- Branch-scoped composite indexes where the final query predicates require them.

Do not duplicate indexes already created by EF for FKs. Verify the generated migration and model snapshot.

### 8. Receipt numbers

- Decide whether receipt numbers are immutable tenant-wide, including soft-deleted/returned payments. The recommended policy is no reuse.
- Align the generator's `IgnoreQueryFilters` scan with the database uniqueness constraint, preserving the existing retry behavior for SQL duplicate errors.
- Add a data check for duplicate receipt numbers before changing the index.

## Phase 3 — referral and map model cleanup

### 9. Referral source

- Preserve historical referral snapshots on `ReferralBonus`.
- Normalize outside referrer phone before storage and decide whether the bonus must snapshot the phone too.
- Enforce exactly one of existing contact, staff, or outsider at the service boundary and, where SQL Server-compatible, with a check constraint after data cleanup.
- Add proper FKs/indexes for tenant-local contact/staff referrers only after the staff/branch prerequisite is complete.

### 10. Geometry and area

- Keep GeoJSON as the current storage format unless there is a compelling package-free reason to change it.
- Validate supported GeoJSON geometry types and `[longitude, latitude]` WGS84 degrees consistently. Preserve marker fallback and the existing map projection behavior.
- Decide whether `AreaSqm` is authoritative input or derived from geometry. Add validation/reconciliation accordingly; do not silently alter historical parcel area.
- Add parcel-photo service operations and restricted-FK handling under the parcel edit policy.

## Verification requirements

- Update model snapshots and create only the logical migrations described above.
- Run `dotnet build WayPlot\\WayPlot.csproj` without starting the API or connecting to a database.
- Add/update unit/service tests for authorization, status transitions, relationship consistency, referral exclusivity, unique email/receipt behavior, and branch scoping.
- Append the following manual checklist as a new section in `docs/TESTING.md`, after the existing commission/referral scenarios.

## Manual test checklist to append to `docs/TESTING.md`

### Parcel permissions and lifecycle

#### Agent

- Plot a parcel with label, price, description, owner, geometry/marker, and photos: succeeds.
- Attempt to set status manually to Reserved/Sold/Leased: rejected; new plot uses the defined initial status.
- Edit a parcel allowed by the confirmed “own or assigned” decision; attempt an unowned/unassigned parcel and verify rejection.
- Attempt price change, reassignment, approval, archive, and hard delete: each is rejected.
- Verify disabled actions use the shared muted-button helper.

#### Tenant Admin

- Plot, edit, change price, reassign, review/approve, and archive any parcel.
- Verify an archived parcel remains in history and cannot be physically deleted if any Lead or Deal references it.
- Verify status changes occur only after the specified deal/payment events and that cancellation/default recalculates correctly.

#### Branch Manager (Tier C)

- Plot only within their own branch; attempt another branch and verify rejection.
- Edit/view only their branch's parcels; attempt price change, reassignment, approval, archive, and hard delete and verify rejection.
- Verify a manager without a branch receives a clear service error and no enabled mutation controls.

#### Super Admin

- Verify no tenant parcel data or parcel mutation controls are available.

#### Integrity and concurrency

- Sign in every tenant login at least once and verify exactly one Staff row per login; verify branch assignment is consistent.
- Test two Negotiation leads for one parcel if that decision is confirmed; test that only one active deal can be confirmed.
- Test duplicate normalized phone/email and concurrent receipt generation.
- Test deal Lead/Contact/Parcel mismatch diagnostics before and after migration.

## Final handoff

Report:

1. Every file changed, grouped by source, migration, tests, docs, and UI.
2. Every migration name and its logical group.
3. All pre-migration data checks and any violating rows found (without exposing secrets).
4. Build result from `dotnet build WayPlot\\WayPlot.csproj`.
5. Any unresolved decision, especially the Agent “own or assigned” parcel edit rule, negotiation uniqueness, branch source of truth, email uniqueness, and AreaSqm authority.
