# WayPlot Design Contract

## Conventions

- Services return `ServiceResult<T>` and expose success/failure through `Ok`, `Message`, and `Value`.
- `TenantAccessPolicy` performs role and scope checks. Super Admin is refused tenant access; Branch Manager reads are branch-scoped and mutations are refused.
- Repository entities are tracked for mutations and short-lived unit-of-work contexts are disposed after operations. Mutations use `ExecuteInTransactionAsync` and the entity `Version` concurrency token.
- Existing domain events such as `LeadCaptured`, `LeadStageChanged`, `DealChanged`, `PaymentConfirmed`, and `PayoutChanged` remain the change-event surface.
- Admin setting and assignment changes also dispatch `TenantSettingChanged`, `PipelineConfigChanged`, `StaffChanged`, `ContactChanged`, and `ParcelChanged` events.
- Later screens use the domain DTOs and service methods below instead of entities.

## Contact duplicate rules

Non-deleted contacts have unique phone numbers under `UX_Contact_Phone` and unique non-null emails under `UX_Contact_Email`; blank and null emails do not collide. Contact capture trims and lowercases email through `TryNormalizeEmail` before lookup or save. `FindByPhoneOrEmailAsync` still tries phone first. If a concurrent capture wins either unique index, the losing capture looks up the contact again in a fresh unit of work and records the existing duplicate inquiry instead of exposing a database error.

## Parcel and staff rules

- Agents and Tenant Admins may create parcels. Tenant Admins may edit any parcel; Agents may edit only parcels assigned to them. Only Tenant Admins may change parcel prices, reassign parcels, or archive parcels.
- Branch Managers with an assigned branch may read parcel lists, details, and owners tenant-wide by design (Run E); parcel reads are not branch-filtered. Branch Managers cannot mutate parcels.
- `TenantStaffProvisioner` provisions/reconciles tenant Staff rows from Master logins. `SessionStarter` creates a missing Staff row or updates its login identity fields at sign-in. `Staff.BranchId` is the only branch source for tenant scope; `SessionStarter` copies it into the session.
- Payment receipt numbers are unique among non-deleted payments (`UX_Payment_Receipt`).
- `Parcel.AreaSqm` is stored metadata (the draw flow may prefill it); the service checks only that it is greater than zero and does not compare it with polygon geometry.
- Tier A has no parcel review or approval step.

## B3 methods and rules

| Area | Method | Caller | Rule |
|---|---|---|---|
| Deal summary | `DealService.GetDealSummaryAsync(Guid)` | Scoped roles | Agent sees only own deals; Admin/Branch scope applies. |
| Deal summaries | `DealService.GetDealSummariesForContactAsync(Guid)` | Scoped roles | Newest deal cards for one contact; Agent sees own deals only. |
| Status refresh | `DealService.RefreshDealStatusesAsync()` | Any role | Sets confirmed nonterminal deals `Current` or `Overdue` as of `TimeProvider`; Version conflicts skip. |
| People | `ContactService.ListPeopleAsync(PeopleFilter)` | Tenant-readable roles | Contacts follow Run E; owners follow parcel visibility; merge only by normalized digits. |
| Add lead | `LeadService.AddLeadForContactAsync(Guid, Guid, string?, Guid?)` | Agent/Admin | Available parcel, one open lead per lot, Agent owns assignment; Admin uses supplied/contact agent or refuses. |
| Switch lot | `LeadService.SwitchLotAsync(Guid, Guid, string?)` | Agent/Admin | Eligible open lead only; creates new lead and loses old lead in one transaction. |
| Pipeline | `LeadService.GetPipelineBoardAsync(PipelineFilter)` | Scoped roles | `IPipelineStageProvider` supplies New, active configurable stages by sort order, Closed, Lost; Closed/Lost default to last 30 days. |
| Allowed moves | `LeadService.GetAllowedMovesAsync(Guid)` | Scoped roles | The provider's position defines the next one-step move; Lost remains available under its existing rule and Closed remains the deal request flow. |
| Pipeline configuration | `PipelineConfigService.GetAsync`, `GetAllForAdminAsync`, `SaveAsync` | Pipeline-readable roles / Tenant Admin saves | Stores versioned history in Setting `PipelineStages`; JSON stage keys are text. New is first, Closed/Lost are fixed last, and the last active configurable stage is the closing stage. A stage with open leads cannot be switched off; at least one configurable stage stays active. Save uses the Setting Version token and raises `PipelineConfigChanged`. Saves, stage moves and Closed requests serialize on a tenant-database transaction lock; the save checks SQL open-lead counts and writes the Setting under that lock. |
| Timeline | `LeadService.GetLeadTimelineAsync(Guid)` | Scoped roles | Merges history, interactions and existing deal fields, newest first and local time. |
| Reassignment | `LeadService.ReassignLeadAsync`, `ReassignContactAsync`, `ReassignAllFromAgentAsync` | Tenant Admin | Active Agents only; Tier C cross-branch moves refuse; history/deals/payouts are preserved. |
| Reassignment preview | `LeadService.PreviewReassignAllFromAgentAsync(Guid, Guid)` | Tenant Admin | Read-only count preview for the bulk confirmation; uses the same active-Agent and Tier C branch checks as bulk reassignment. |
| Staff | `StaffService.ListStaffAsync()` | Admin/BranchManager | Admin sees tenant staff; Branch Manager sees the own branch and scoped counts. |
| Staff mutations | `StaffService.DeactivateAsync(Guid, int)`, `ReactivateAsync(Guid, int)`, `ChangeBranchAsync(Guid, Guid, int)` | Tenant Admin | Soft-deactivate only; refuse self and last active Tenant Admin; Agents with open leads or available parcels must be reassigned first. Branch changes require Tier C and an existing branch. Version conflicts refuse cleanly. |
| Branch list | `StaffService.ListBranchesAsync()` | Tenant Admin, Tier C | Only existing tenant branches are returned for staff assignment. |
| Review queue | `DealService.GetAdminReviewQueueAsync()` | Tenant Admin | Lists unconfirmed Closed requests and payments awaiting confirmation. Confirmation and return actions use the existing DealService methods. |
| Commission rules | `CommissionService.GetRulesAsync`, `SaveRulesAsync` | Tenant Admin | Versioned history; JSON writes `CommissionCalculationType` and `CommissionPayableTiming` as text while accepting legacy numeric enum values. Commission amount overrides may target an active Agent or existing parcel; an Agent override wins when both match. Shares and referral rules remain global. |
| Company profile | `CompanyProfileService.GetAsync`, `SaveAsync(CompanyProfileRequest)` | Tenant-readable roles / Tenant Admin saves | Stores name, contact numbers, email and document address in the `CompanyProfile` Setting JSON with Version, ChangedAtUtc and ChangedBy. Length limits: 150, 200, 254 and 500 characters respectively. |

## B2 and B2b existing service surface

| Area | Methods | Caller/rule |
|---|---|---|
| Deals | `DealService.ListAsync`, `ListCandidatesAsync`, `RequestClosedAsync`, `ConfirmClosedAsync`, `ReturnRequestAsync` | Admin confirms/returns Closed; Agents request own eligible leads. |
| Termination | `DealService.GetTerminationSummaryAsync`, `CancelDealAsync`, `DefaultDealAsync` | Tenant Admin only; confirmed Current/Overdue deals; pending payments and invalid money outcomes refuse. |
| Payments | `DealService.ListPaymentsAsync`, `RecordPaymentAsync`, `ConfirmPaymentAsync`, `ReturnPaymentAsync` | Agent records own deal payment; Tenant Admin confirms/returns; Admin may record any accessible deal. |
| Payouts | `CommissionService.GetRulesAsync`, `SaveRulesAsync`, `ListAsync`, `ApproveAsync`, `MarkPaidAsync` | Admin manages and pays; Agents see own payouts; payable timing controls Paid. |

## Schedule and overdue decisions

- Obligation order: reservation fee, down payment, installments or one full-payment remainder.
- Installment 1 is due on the next DueDay strictly after confirmation; later installments use the same due day and clamp to month length.
- The last installment takes the remainder so the obligation total equals net price.
- Confirmed payments are allocated to obligations in order. Pending payments are informational only.
- A due obligation before today's local date is overdue while not fully covered. Late penalties are not calculated.
- `DealType` is inferred only from current parcel status: Sold = Sale, Leased = Lease.

## Screen mapping

- Contacts tab: `ListPeopleAsync`, contact deal cards from `GetDealSummariesForContactAsync`.
- Add/Switch lot: `AddLeadForContactAsync`, `SwitchLotAsync`.
- Pipeline and timeline: `GetPipelineBoardAsync`, `GetAllowedMovesAsync`, `GetLeadTimelineAsync`.
- Pipeline stage settings are managed by Tenant Admin in the Pipeline tab. The provider caches settings per tenant session, resolves inactive stage names for history, and refreshes on `PipelineConfigChanged` and sign-out. Defaults are Contacted, Viewing, and Negotiation active in their existing order, followed by four inactive custom slots named Stage 1 through Stage 4. Configurable names are 1–30 characters and unique ignoring case.
- If an open lead is found in a configurable stage that is now inactive, the board keeps it visible in a marked inactive column. Its next move is the next active stage after its configured position, or the closing stage if there is no later active stage.
- Request Closed is allowed only from the last active configurable stage, both when offered as an allowed move and when `DealService.RequestClosedAsync` validates it. Stage progression follows configured position rather than enum value or stage name.
- Admin team tools: staff list/mutations, `ListBranchesAsync`, reassignment preview and the lead/contact/parcel/bulk reassignment methods.
- Tenant Admin screens: Team and Review; commission rules and profile dialogs are role-gated by their services. Invite and role changes are unavailable until the account service exists.
- Company profile reads are available to Agent, Branch Manager and Tenant Admin. Only Tenant Admin can save.
- There is no cancel-request queue in this application; confirmed-deal cancellation is a direct Tenant Admin action, not a queued request.

## AD2 fixes

- Stage switch-off race: `PipelineConfigService.SaveAsync` holds one tenant-database transaction and the `WayPlot:PipelineStages` application lock while it checks the Setting `Version`, counts open leads with SQL across the whole tenant, and writes the Setting. `LeadService.MoveStageAsync` and `DealService.RequestClosedAsync` take the same lock, read the `PipelineStages` Setting from the database, and validate the target stage, `Next` and the closing-stage rule against that fresh copy. A failed stage validation returns "The pipeline stages were just changed. Refresh and try again." The existing `PipelineConfigChanged` and per-change domain events are still dispatched.
- An open lead left in a configurable stage that is now inactive stays on the board in a marked inactive column and moves to the next active position, or the closing stage when no later active stage exists.
- `AppLog.Error` redacts password, pwd, user id, uid, token and apikey values, and whole connection strings, from exception text while keeping the exception type, message and stack. `SensitiveValueRedactor.Redact` is a pure, database-free function.
- Every async void handler in the WinForms project keeps all work inside a try/catch and shows a short, non-sensitive failure message.

## Worked deal summary examples

### 1. Current installment deal

As-of date: **2026-11-18**. Confirmation date: **2026-11-01**. Terms: net price `₱300,000.00`, reservation `₱10,000.00`, down payment `₱40,000.00`, 12 installments, and `DueDay = 31`.

- The installment amount is fixed at `₱20,833.33` for the first 11 installments, except the last installment takes the remainder.
- Last installment: `₱250,000.00 - (11 × ₱20,833.33) = ₱20,833.37`.
- Confirmed paid: `₱50,000.00`.
- Balance: `₱300,000.00 - ₱50,000.00 = ₱250,000.00`.
- Percent paid: `₱50,000.00 / ₱300,000.00 × 100 = 16.67%`.
- The first installment is due strictly after confirmation on **2026-11-30**, then monthly on clamped DueDay 31.
- Next due date: **2026-11-30**. Next due amount: **₱20,833.33**.
- Overdue amount: `₱0.00`; 0 days overdue.
- Installments paid: 0 of 12.

Code walk: `DealScheduleBuilder` emits reservation, down payment, and 12 installment obligations. The confirmed payments cover reservation and down payment first. No obligation is due before the fixed as-of date, so the next uncovered obligation is the November 30 installment.

### 2. Overdue installment deal

As-of date: **2026-11-18**. Confirmation date: **2026-01-01**. Terms: net price `₱240,000.00`, no reservation fee, down payment `₱20,000.00`, 10 installments of `₱22,000.00`, and `DueDay = 31`.

- Confirmed paid: `₱20,000.00`.
- Balance: `₱240,000.00 - ₱20,000.00 = ₱220,000.00`.
- Percent paid: `₱20,000.00 / ₱240,000.00 × 100 = 8.33%`.
- The installment obligations are due: Jan 31, Feb 28, Mar 31, Apr 30, May 31, Jun 30, Jul 31, Aug 31, Sep 30, and Oct 31.
- Next due date: **2026-01-31**. Next due amount: **₱22,000.00**.
- All 10 installments are uncovered and due before 2026-11-18.
- Overdue amount: `10 × ₱22,000.00 = ₱220,000.00`.
- Days overdue: 291 days from 2026-01-31 to 2026-11-18.
- Installments paid: 0 of 10.

Code walk: `DealScheduleBuilder` emits the down payment on January 1 and the 10 clamped monthly installments beginning January 31. Confirmed payment fully covers the down payment. The summary marks every earlier uncovered installment as overdue and uses January 31 as the earliest due date.

### 3. Fully paid deal

As-of date: **2026-11-18**. Confirmation date: **2026-09-01**. Terms: net price `₱180,000.00`, reservation `₱5,000.00`, down payment `₱25,000.00`, full-payment remainder `₱150,000.00`, and `IsInstallment = false`.

- Confirmed paid: `₱180,000.00`.
- Balance: `₱180,000.00 - ₱180,000.00 = ₱0.00`.
- Percent paid: `100.00%`.
- The three obligations total `₱5,000.00 + ₱25,000.00 + ₱150,000.00 = ₱180,000.00`.
- Next due date: none. Next due amount: none.
- Overdue amount: `₱0.00`; 0 days overdue.
- Installments paid: 0 of 0.

Code walk: `DealScheduleBuilder` emits reservation, down payment, and one full-payment remainder obligation on 2026-09-01. Confirmed payments cover all obligations in order. Balance is zero, PercentPaid is 100, and no next obligation remains.
