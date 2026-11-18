# FIX1 checklist

Source fixes are complete. Runtime checks are unchecked because this run did not start the app or access a database.

- [ ] A failing pipeline load shows `Could not load the pipeline. Try again.` and the app stays open.
- [ ] Switching tabs repeatedly leaves no transient-status timer running.
- [ ] `ben.a` sees no owner of a parcel outside his scope in People.
- [ ] A Branch Manager sees only owners of parcels in their branch.
- [ ] Contact deal cards remain the same for Admin, Agent, and Branch Manager.
- [ ] With two deals and one stale Version, status refresh skips only the stale deal.
- [ ] A failed multi-step confirm leaves no half-saved deal.
- [ ] PipelineView async UI boundaries catch exceptions and show short, non-sensitive status.
- [ ] AttachLeadDialog search timer is disposed with its component container.
- [ ] Commission down-payment eligibility subtracts the reservation fee from confirmed payments before checking the down-payment threshold.
- [ ] Concurrent first-login demo seeding is serialized; an interrupted seed remains marked in progress and cannot be reported as complete.
- [ ] Tier C branch provisioning wraps its multiple saves in one database transaction.
- [ ] Login-triggered status refresh failures are logged and remain silent in the UI.
- [ ] Multi-save audit: `ConfirmClosedAsync`, payment status changes, and commission status changes each stage changes for a single save; `ParcelPhotoService.RemovePhotoAsync` already uses `ExecuteInTransactionAsync`; `TenantBranchProvisioner.EnsureTierCAsync` now wraps its two saves in a transaction. `SessionStarter.StartAsync` has mutually exclusive insert/update branches, each with at most one save.

## Finding report

| Review | Finding | Status | File and method |
|---|---|---|---|
| UI Runtime | UI-01 — Major | Fixed | `WayPlot/Presenters/PipelinePresenter.cs` — `LoadAsync`; `WayPlot/Views/PipelineView.cs` — `OnLoad` and async UI event paths |
| Access | ACCESS-01 — Major | Fixed | `WayPlot.domain/Services/DataCollection/ContactService.cs` — `ListPeopleAsync`; `WayPlot.infrastructure/Repositories/EfRepositories.cs` — `OwnerRepository.ListForVisibleParcelsAsync`, `ParcelRepository.ListVisibleToAsync` |
| Access | Deal summary in-memory scope (explicit FIX1 item) | Fixed | `WayPlot.domain/Services/MainTransaction/DealService.cs` — `GetDealSummariesForContactAsync`; `WayPlot.infrastructure/Repositories/EfRepositories.cs` — `DealRepository.ListByContactAsync` |
| Money | MONEY-01 — Major | Fixed | `WayPlot.domain/Services/MainTransaction/CommissionService.cs` — `PaidBlockedReason` |
| Money | Per-deal status refresh (explicit FIX1 item) | Fixed | `WayPlot.domain/Services/MainTransaction/DealService.cs` — `RefreshDealStatusesAsync` |
| Data and Startup | STARTUP-01 — Major | Fixed | `WayPlot.infrastructure/Data/Seed/TenantSeeder.cs` — `SeedAsync`, `AcquireSeedLockAsync`, `SetDemoSeedStateAsync` |
| UI Runtime | ContactsView transient timer | Fixed | `WayPlot/Views/ContactsView.cs` — timer initialization; component container disposal |
| UI Runtime | AttachLeadDialog search timer | Fixed | `WayPlot/Views/AttachLeadDialog.cs` — timer initialization and component container ownership |
| Data and Startup | Tenant branch provisioner multi-save operation | Fixed | `WayPlot.infrastructure/Data/Seed/TenantBranchProvisioner.cs` — `EnsureTierCAsync` |
| Data and Startup | Login-triggered refresh failure logging | Fixed | `WayPlot/LoginForm.cs` — background `RefreshDealStatusesAsync` continuation |
| Spec Conformance | Documents, offline sync, company profile, staff management, pipeline configuration, landowner payouts, promo/campaign, registration, and subscription-management gaps | Needs feature | No implementing screen/service path; excluded by FIX1 scope |
| UI Runtime | Map tile-origin allowance | Skipped | `WayPlot/UI/LeafletMapControl.cs` — explicitly excluded by FIX1 |

## Report-only clarification

The ContactsView Agent-only `Cancel request` flow is present for a pending Closed request: `WayPlot/Views/ContactsView.cs`, `ConfigureDealCardActions` assigns `CancelRequestRequested` to `ReturnDealRequestAsync` for Agents, and `ReturnDealRequestAsync` calls `DealService.ReturnRequestAsync`. The SPEC CONFORMANCE statement that there is no Agent termination-request flow remains true for cancelling/defaulting an already confirmed deal; these are separate actions.

## Could not verify

- No app runtime, database, concurrency, UI timer, or login-flow checks were run.
- `dotnet build WayPlot\WayPlot.csproj` succeeded with 0 warnings and 0 errors.
- The review files’ other “Questions” remain questions; they were not promoted to findings or changed without source evidence.
