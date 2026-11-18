# SPEC CONFORMANCE review

Reviewed commit: `3fc82a1` (working tree contained uncommitted changes; review reflects the files as inspected)

## Tier A use cases by role

“Implemented” means a present screen and service path cover the documented action. “Partial” means only a subset or a different behavior is present. “Missing” means no end-user path was found. This is a source review, not an execution report.

| Role / documented use case | Status | Screen and method / observed behavior |
|---|---|---|
| Agent: plot parcel boundary/marker, label, price, description, photos, owner | Partial | Parcel Map + `ParcelMapView`, `ParcelService`, `ParcelPhotoService`, `PhotoProcessor`; drawing/editing and photos exist. No reliable user-facing offline save exists. |
| Agent: capture contact/lead, source, consent, referrer | Implemented | Contacts/Parcel Map + `ContactService.CaptureAsync`, `LeadService.AttachContactToParcelAsync`; referral capture fields and service validation exist. |
| Agent: import contacts from CSV | Implemented | Contact import dialog + `ContactImportService.PreviewAsync` / `ImportAsync`. |
| Agent: manage contacts | Implemented | Contacts view + `ContactService` and contact/lead/deal service methods; includes contact list, detail, lead and deal cards. |
| Agent: search/filter leads | Implemented | Pipeline + `LeadService.GetPipelineBoardAsync` and repository query filters. |
| Agent: send SMS/email/calls and log them | Partial | Pipeline and lead-file dialogs log Call/SMS/Email/Note interactions through `InteractionService`; no SMS/email send integration or calling action was found. |
| Agent: log viewing/outcome | Implemented | Pipeline/log interaction flows and stage movement through `InteractionService.LogAsync` / `LeadService.MoveStageAsync`. |
| Agent: move lead through pipeline | Implemented | Pipeline move menu/drag-and-drop and `LeadService.MoveStageAsync`; Closed is separately requested and admin-confirmed. |
| Agent: view lead/parcel history | Implemented | Lead file/timeline, parcel map; `LeadService.GetLeadTimelineAsync`, interactions and stage history. |
| Agent: record payment received | Implemented | Deal/contact cards + `DealService.RecordPaymentAsync`; admin confirmation separate. |
| Agent: view own commissions/referral bonuses | Implemented | Commissions view + `CommissionService.ListAsync`, recipient-scoped. |
| Agent: view balances/payment history | Implemented | Contacts and deal cards + deal summary/payment service methods, subject to service scope. |
| Agent: attach documents to deal or parcel | Missing | `Document` entity exists, but no document repository/service/UI attachment flow found. |
| Agent: sync offline changes | Missing | No desktop sync/outbox uploader, local persistence queue, or conflict-resolution UI/service found. `SyncStatus` is audit metadata only in the inspected app path. |
| Tenant Admin: company profile | Missing | No company profile settings screen/service found. |
| Tenant Admin: manage agents and roles | Partial | `TenantStaffProvisioner` reconciles login rows; `StaffService` lists staff. No end-user staff invitation/create/deactivate/role-management UI/service found. |
| Tenant Admin: configure pipeline stages | Missing | Pipeline columns come from `DefaultPipelineStageProvider`; no configuration UI/persistence found. |
| Tenant Admin: assign/reassign leads and parcels | Implemented | Reassign dialogs and `LeadService` reassignment methods; `ParcelService.ReassignAsync`. |
| Tenant Admin: confirm Closed deals | Implemented | Deals/contact/pipeline flows + `DealService.ConfirmClosedAsync`. |
| Tenant Admin: confirm payments | Implemented | Deals/contact flows + `DealService.ConfirmPaymentAsync` and return path. |
| Tenant Admin: set commission/referral rules | Implemented | Commission rules dialog + `CommissionService.SaveRulesAsync`. |
| Tenant Admin: approve commissions and mark Paid | Implemented | Commissions view + `ApproveAsync` / `MarkPaidAsync`. |
| Tenant Admin: cancel/default deal | Implemented | Termination dialog + `DealService.CancelDealAsync` / `DefaultDealAsync`. |
| Tenant Admin: record landowner payouts | Missing | `LandownerPayout` entity exists, but no service/repository contract or UI path found. |

## Who Initiates and Who Validates

| Documentation row | Status | Evidence in current app |
|---|---|---|
| Register tenant; business owner / Super Admin; payment proof | Missing | No registration UI or API route; API has login only. Development seeder creates demo tenants. |
| Plot parcel; Agent / system duplicate check and Admin review | Partial | Parcel Map and service validations exist; no duplicate-lot review queue. |
| Capture lead; Agent / system duplicate check | Implemented | Contact and lead flows; normalized phone/email matching and duplicate inquiry handling. |
| Import contacts; Agent / format and duplicate check | Implemented | Preview/validation/import service. |
| Assign/reassign lead; Admin/system / Admin override | Implemented | Admin reassignment service/UI and active-agent validation; no automatic distribution rule was found. |
| Move pipeline stage; Agent / Admin confirms Closed | Implemented | Service stage rules and distinct confirm-Closed admin flow. |
| Send promo; Agent / Admin approval and consent | Missing | No campaign model, approval flow, sender, consent-filtered audience or send log found. |
| Activate/suspend subscription; Super Admin/payment check | Partial | Master entities and login read-only calculation exist; no subscription-management UI/API workflow or payment validation flow found. |
| Confirm Closed deal; Agent request / Admin confirmation | Implemented | Deal request and confirm services/UI; service builds deal and payouts. |
| Record payment; Agent / Admin confirmation and duplicate check | Implemented | Record and confirm service paths; receipt uniqueness and duplicate heuristics. |
| Set commission/referral rules; Admin / history kept | Implemented | Settings history JSON with versioned rules; rules dialog. |
| Pay commission/referral; system calculates / Admin approves and pays | Implemented | Deal confirmation calculates Pending shares/bonus; Admin status transitions. |
| Cancel/default; Admin or Agent request / Admin decides | Partial | Admin can terminate directly; no Agent termination-request flow found. |
| Record landowner payout; Admin / owner-share check | Missing | No service/UI path or owner-share validation found. |

## Who can see what

| Documentation role/scope | Status | Notes |
|---|---|---|
| Agent: own commissions/referrals; own customers’ balances/payments; can record payment | Implemented with known gap | Commission and deal scope mostly follows owner. The earlier ACCESS review found `ContactService.ListPeopleAsync` loads all tenant owners for Agents, violating contract owner visibility. |
| Tenant Admin: all commissions plus company share; all payments/deals; confirm/approve/pay | Implemented | Admin scope is tenant-wide in services. |
| Branch Manager: own and branch commissions; branch deals | Implemented with known owner-list gap | Money services scope by branch and own recipient. ACCESS review identified owner details loaded without query scoping; non-money branch people path applies filters in memory. |
| Super Admin: no tenant commission/payment/balance records | Implemented in tenant access services | `TenantAccessPolicy` denies tenant read/mutations. API exposes no tenant-data endpoint. |

## Offline Behavior table: claim versus implementation

| Documentation claim | Status | Source behavior |
|---|---|---|
| Read leads, contacts, parcels, notes, history, balances, commissions from local copy | Missing | Services read tenant database directly; no local cache/database was found. |
| Capture lead, log interaction, change stage, log viewing offline and queue sync | Missing | Operations call the live unit-of-work/SQL service paths; no offline queue or sync conflict workflow. |
| Plot/edit parcel offline and queue sync | Missing | Parcel writes save directly to SQL. |
| Previously viewed map tiles/parcel shapes offline | Partial | WebView2 loads remote tiles; browser caching may occur, but no explicit offline guarantee exists. Parcel shapes are sent from live service-loaded data. |
| Queue SMS/email/promos while offline | Missing | No send/queue/promo feature found. |
| First login and plan changes require connection | Partial | First login calls API; no subscription plan-change UI exists. |
| Seven-day subscription grace period offline | Missing | Login returns a read-only calculation based on tenant status/expiry, but there is no offline grace-period implementation. The desktop does not use the returned read-only flag to gate mutations. |
| Record payment/document offline, upload later | Missing | Payment writes are live SQL calls; document flow is absent. |
| Confirmation, commission status and termination require connection | Partial | Operations are live database calls, but there is no explicit connection-state/offline gate; they fail through ordinary errors if DB is unreachable. |
| Important sync conflicts flagged for Admin | Missing | Version tokens detect concurrent writes, but no offline conflict review queue/UI exists. |

## Contract drift

- Design Contract convention says mutations use `ExecuteInTransactionAsync` and the entity Version token. Source shows many mutations call one `SaveAsync`/EF `SaveChangesAsync` directly (e.g. `ConfirmClosedAsync`, payment status changes, commission status changes). EF `SaveChanges` is transactional for one save, and tokens are configured, but the explicit contract method is not consistently used.
- Design Contract B3 says owners in People follow parcel visibility. `ContactService.ListPeopleAsync` fetches all parcels/owners for Agent scope and only narrows branch data in memory. This matches ACCESS-01 from the prior review.
- Design Contract B3 `GetDealSummariesForContactAsync` states scoped results; service loads all contact deals and filters in memory rather than passing scope to the repository. Returned DTOs are filtered, but the SQL-scoping convention is not met.
- Design Contract `RefreshDealStatusesAsync` says conflicts skip; implementation catches a lifecycle conflict around the batch save and skips the batch operation as a whole. Depending on EF SaveChanges failure semantics, multiple status changes can roll back together rather than letting non-conflicting rows persist. This is a wording/behavior mismatch; no partial-row conflict handling exists.
- Design Contract “Later screens use domain DTOs and service methods instead of entities” is followed by presenters/views found in the UI review; no direct repository or DbContext access was found in views/presenters.
- Product documentation claims broad offline mode, reminders/scoring, messaging/promos, documents, registration/subscription management, and landowner payouts that are not present in this code. Those are larger product/documentation mismatches, not hidden implementations.

## Documentation Git history

`git log -p -- WayPlot_CRM_Documentation_v3txt.txt` shows the file was first added in commit `4236a03f04d7774967397c7b37d8244131a11477` on **2026-11-17** (`updating database`). It was a new-file commit containing the full document. There are no later commits modifying this file, so there are no post-creation changes to enumerate.

## Checked and found fine

- Tier A lead, contact, parcel, deal, payment, and commission paths exist for the implemented actions listed above.
- Domain services repeat core input/role checks rather than depending only on disabled UI buttons.
- Super Admin tenant data access is denied in `TenantAccessPolicy`; Branch Manager reads fail closed without a branch.
- Commission and referral rules are history-versioned; confirmed deal creation creates the initial pending payouts.
- No code path was found that sends customer SMS/email without a consent-gated campaign implementation; sending itself is absent.

## Questions

- Are the absent Tier A functions (documents, staff/role management, company profile, pipeline configuration, landowner payouts) intentionally deferred, or should the product documentation be narrowed to match this build?
- Should the Tier B/C offline, reminders, lead scoring, messaging, and reporting descriptions remain in the product spec while those modules are absent?
- The contract says “Version conflicts skip” for refresh. Does that mean skip the entire refresh batch or continue committing non-conflicting rows individually?

## Could not verify without running the app

- Which tabs and controls are reachable for each role and tenant tier at runtime.
- Actual offline behavior of WebView2/Edge tile caching and connection-failure UX.
- Whether service exception paths and read-only subscription state are consistently presented across all user flows.

