# FIX2 checklist

Runtime checks are unchecked; this run did not start the app or connect to a database.

- [ ] Reset refuses outside Development and for a non-admin.
- [ ] A failing contacts load shows `Could not load contacts. Try again.` and the app stays open.
- [ ] A failing AttachLeadDialog search shows a status and does not crash.
- [ ] Status refresh raises `DealChanged` only for deals whose status changed and whose save succeeded.
- [ ] A stale Version still skips only that deal.

## Changes

1. `TenantSeeder.ResetDemoDataAsync` now refuses outside Development and uses `TenantAccessPolicy.RequireTenantAdmin` before opening the tenant database. The desktop host supplies the Development predicate from its existing environment. No UI or call site was added.
2. `ContactsView.OnLoad` catches load failures and shows a short status. `AttachLeadDialog` routes Search, Attach, Enter, timer tick, and Shown boundaries through a catch-and-status helper while preserving the search token and `IsDisposed` guards. Other previously unguarded async load boundaries were fixed in `DealsView.OnLoad`, `CommissionsView.OnLoad`, `ParcelMapView.OnLoad`, and `LeafletMapControl.OnLoad`.
3. `DealService.RefreshDealStatusesAsync` dispatches `DealChanged` after each successful changed-row save; unchanged rows and Version conflicts dispatch no event.
4. Recovery answer: first-login seeding returns a failure for an `InProgress` marker. `LoginForm` shows a warning, then continues into the shell; it does not seed demo data. For a disposable development tenant, the smallest safe recovery is to verify and back up the correct tenant database, then have a developer sign in as its Tenant Admin in Development and invoke the guarded `ResetDemoDataAsync`. Reset deletes tenant business data before reseeding; do not use it for a tenant with user work. No recovery UI or call site was added.

## Async void handlers fixed

- `ContactsView.OnLoad`
- `AttachLeadDialog` Search/Attach buttons, Enter key, search timer tick, and Shown handlers (now synchronous event handlers that dispatch through `RunHandlerAsync`)
- `DealsView.OnLoad`
- `CommissionsView.OnLoad`
- `ParcelMapView.OnLoad`
- `LeafletMapControl.OnLoad` (added a general catch after its specific WebView2 catches)
- Async UI event lambdas across `ContactsView`, `DealsView`, `CommissionsView`, `ParcelMapView`, `ContactDialog`, `CommissionRulesDialog`, `ContactImportDialog`, `CreateDealDialog`, `LeadFileDialog`, `LogInteractionDialog`, `ParcelDialog`, `PhotoViewerDialog`, `RecordPaymentDialog`, `ReturnPaymentDialog`, and `LeadCardControl` now route through the shared catch-and-message helper.

`PipelineView.OnLoad`, `LoginForm.btnSignIn_Click`, and `DealCardControl.TogglePayments` already had catches and were left in place.

## Verification

- Added database-free `DealStatusRefreshEventTests.Refresh_dispatches_only_for_a_status_change_that_saved_successfully`, using fake tenant repositories and a simulated Version conflict.
- `dotnet build WayPlot\WayPlot.csproj`: succeeded, 0 warnings and 0 errors.
- `dotnet test WayPlot.Tests`: passed, 7 passed, 0 failed, 0 skipped.
- Could not verify Development reset refusals, actual SQL Server locking/data deletion, UI status display, or runtime continuation without launching the app or using a database.
