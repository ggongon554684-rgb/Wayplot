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

## Follow-up diagnosis: parcel inspector and empty pipeline

### Summary

The screenshots and current source point to two separate behaviors. The Pipeline checkbox has a source-confirmed filtering effect that can explain an empty board. The parcel screenshot suggests selection may reach the inspector, but the screenshot does not include the transient message text or parcel values, so the exact reason those values are not visible is still unconfirmed. Neither symptom, by itself, indicates a schema defect.

### Pipeline: why records appear only after checking the box

`PipelineView.ReloadAsync` passes `chkOlder.Checked` into `PipelineFilter.IncludeAllClosedAndLost`. The checkbox starts unchecked. `LeadService.GetPipelineBoardAsync` then removes leads whose stage is `Closed` or `Lost` when their `LastActivityAtUtc` is more than 30 days old. Checking the box disables that filter and includes those records.

This means the most likely explanation for “nothing until I check the box” is that this tenant’s visible leads are all old `Closed` or `Lost` leads. This is consistent with the filter’s intended behavior; it is not evidence that the pipeline failed to load. Confirm it by checking whether the cards revealed by the box are all `Closed` or `Lost` and older than 30 days. If active or recently updated leads are also missing, the filter alone does not explain the result and the query result needs inspection.

There is a second clue in the screenshot: the Parcel filter appears blank. `PipelineView.LoadFiltersAsync` adds an “All parcels” choice only when `PipelinePresenter.ParcelsAsync` succeeds. If that service result fails, the method currently leaves the combo empty and does not show a status message for that failure. This could indicate a parcel-list read problem, but it does not directly explain why the pipeline board changes when the older-leads checkbox is toggled. Verify the parcel filter separately.

The pipeline board query is in `WayPlot.domain/Services/DataCollection/LeadService.cs`, method `GetPipelineBoardAsync`. The UI passes the checkbox in `WayPlot/Views/PipelineView.cs`, method `ReloadAsync`; the checkbox reload is wired in the `PipelineView` constructor. The service uses the configured stage provider to produce columns, then puts matching leads into them.

### Parcel inspector: what the screenshot establishes

The parcel click path is `assets/map/map.js` (`parcelClicked`) -> `LeafletMapControl.HandleWebMessageReceived` (validates the parcel ID against `_knownParcelIds`) -> `ParcelMapPresenter.SelectAsync` (looks up the DTO by ID) -> `ParcelMapView.ShowParcel` (opens the panel and fills its label, status, facts, and description).

If the screenshot showing the open inspector and visible **Archive** action was captured immediately after the failed click, that is evidence that the presenter selected a parcel: the Archive action is only made visible for a selected parcel when the user has the Tenant Admin permission. In that case, the click and parcel lookup likely succeeded; the failure is later in the inspector display/update path or in the values being rendered. `ShowParcel` assigns parcel fields before it awaits the lead list, and `ShowLeadsError` only changes the leads section, so a leads-query failure should not erase the parcel’s label or facts.

Parcel creation follows a different entry path: `ReloadAndSelectSavedAsync` reloads the parcel list and then selects the newly created ID. That path can succeed even if an existing map feature click is being cleared by a competing event, the wrong view/build is running, or the click’s parcel DTO has unexpected values. The screenshots do not distinguish among those causes. The earlier lookup-refresh race is guarded in `ParcelMapPresenter.LoadAsync` and `SelectAsync`, but it is not proven to be the complete cause of the blank inspector.

The next runtime check should record, for one click on an existing parcel, whether `parcelClicked` reaches C#, whether `SelectAsync` finds the ID, whether `ShowParcel` receives the expected label, and whether a `mapBackgroundClicked` arrives immediately afterward and calls `ClearParcel`. Also inspect the parcel’s returned DTO fields. If `ShowParcel` receives the expected label and no clear event follows, the issue is in the WinForms inspector layout or painting, not in the schema or map geometry.

### Splitter exception versus these two symptoms

The earlier `InvalidOperationException` was a separate layout failure. WinForms rejected the panel minimum sizes because they were applied inside generated `InitializeComponent`, while the splitter still had its initial size/position. The active `wayplot` checkout now applies those constraints after initialization in the view constructors. A recurrence of a stack trace pointing to `InitializeComponent` line 78 or line 180 means the running process is still using an older assembly or another checkout; the updated source no longer sets those minimums at those designer lines.

### Verification checklist

- [ ] Stop the running `WayPlot.exe`, rebuild the `wayplot` checkout, and confirm the loaded executable is from that checkout.
- [ ] With the Pipeline checkbox off, note the board cards and stages; turn it on and confirm whether every newly visible card is an old `Closed` or `Lost` lead.
- [ ] Confirm the Parcel filter displays “All parcels”; if blank, inspect the `ParcelsAsync` service result and show its failure message rather than silently leaving the combo empty.
- [ ] Click an existing parcel and verify `parcelClicked`, presenter lookup, `ShowParcel`, and any subsequent `mapBackgroundClicked`/`ClearParcel` sequence.
- [ ] Compare the clicked parcel DTO’s label and fact fields with the inspector text; verify the inspector is not clipped or covered after the splitter expands.
- [ ] Repeat with a newly created parcel and compare the event sequence with the existing-parcel click.

### Limits of this diagnosis

The API could not connect to the local `WayPlot_Master` database in this environment, and the WinForms UI could not be driven here. Therefore, the data distribution hypothesis for the Pipeline and the exact parcel inspector failure are not runtime-confirmed. No schema change is indicated by the source evidence; do not add a migration based only on these screenshots.
