# UI RUNTIME code review

Reviewed commit: `3fc82a1` (working tree contained uncommitted changes; review reflects the files as inspected)

## Findings

### UI-01 — Major

**Evidence**

[PipelineView.cs](../../WayPlot/Views/PipelineView.cs), `OnLoad`:

> 61: `protected override async void OnLoad(EventArgs e)`
> 65: `if (_presenter is not null && !DesignMode)`
> 67: `await LoadFiltersAsync();`
> 68: `await ReloadAsync();`

[PipelinePresenter.cs](../../WayPlot/Presenters/PipelinePresenter.cs), `LoadAsync`:

> 35: `public async Task LoadAsync(PipelineFilter filter)`
> 40: `var result = await leads.GetPipelineBoardAsync(filter);`
> 41: `if (ticket != _latest || view.IsDisposed) return;`

**Rule broken:** UI RUNTIME review requirement: `async void` handlers must handle exceptions.

**What goes wrong:** If the database or provider throws while the pipeline first loads (or while a filter-triggered reload runs), `PipelinePresenter.LoadAsync` does not catch the exception. The exception propagates through the `async void` `OnLoad` or async event delegate, where there is no caller to observe it; WinForms can surface it as an unhandled UI-thread exception and terminate the application instead of showing a load error. Steps: open Pipeline while the database is unavailable or a query throws; the exception reaches the UI event loop.

**Suggested fix:** Catch failures in `PipelinePresenter.LoadAsync` and show a non-sensitive status through the view, or catch them at every async-void boundary. Keep the existing stale-ticket check after the await.

**Confidence:** High.

## Checked and found fine

- **Async and stale results:** Contacts and pipeline presenters use monotonically increasing tickets to ignore older list responses. Photo thumbnail loads check cancellation, disposal, and selected parcel after each await. Photo add/remove paths catch per-operation failures. No `.Result` or `.Wait()` call was found.
- **Async handlers:** `async void` occurrences found are WinForms event/`OnLoad` handlers (including the one above), not public async APIs. Most data-operation paths catch and report exceptions; the pipeline load path above is the exception found. Login status refresh is deliberately fire-and-forget and catches exceptions, so failures are silently discarded.
- **Disposal and sign-out:** `MainShell` removes and disposes active views on tab switches and sign-out. `SessionStarter.End()` clears UserId, username, tenant, branch, tier, role, and tenant connection. No static per-user data cache was found. Photo view and thumbnails dispose their decoded images/streams; photo loading CTS is cancelled/disposed. Pipeline edge-scroll timer and designer-owned timers are stopped/disposed with their controls.
- **WebView/map:** the map host restricts main navigation to the local virtual-host page or configured HTTPS tile origins, denies frame navigation and permissions, and checks message source and message type/shape. `map.js` validates message type; parcel labels, notes, and messages use `textContent`; no `innerHTML`, `outerHTML`, `insertAdjacentHTML`, or `eval` was found. Drawing mode saves the previous double-click zoom state and restores it on mode exit. `ResizeObserver` and the WebView message listener live with the page loaded in the control.
- **Photos:** processing runs through `Task.Run`; supported extensions, source size and processed size limits are checked. EXIF orientations 2–8 are applied before encoding; re-encoding strips metadata. Source, decoded image, bitmap and streams are disposed in the reviewed processing/view paths.
- **Empty/null checks:** collection views generally initialize collections to empty, guard nullable selections, and provide empty states. Photo viewer handles zero photos; parcel selection and map `GeoJson` have explicit null/invalid handling in the presenter/builder path. No evidence-backed main-path index failure was found.
- **Dialog results:** save workflows check `DialogResult.OK` and/or the dialog result before consuming values. Service validation remains in domain services.
- **Layout:** reviewed designer forms use DPI autoscaling; most responsive view content uses docking, anchoring, or table/flow layouts. No definite 125% clipping defect was established by static inspection.

## Questions

- `ContactsView` owns a transient-status WinForms timer outside its designer component container and has no explicit `Dispose` override for that timer. The timer stops on its next tick, but should it be explicitly stopped/disposed when the view is disposed during a tab switch? The short-lived callback could reference the disposed view; I could not prove a crash from the source alone.
- Are the silently swallowed failures in the login-triggered `RefreshDealStatusesAsync` task intended to be invisible to the user? The task is noncritical to login, but failures currently have no logging or status indication.
- The WebView host allows top-level navigation to any path on configured tile origins. No reachable user-controlled navigation was found in the map, so no exploit path was established; confirm if this origin-wide allowance is intended.

## Could not verify without running the app

- Which exception types the actual DB provider can produce on each UI path and whether the app’s global WinForms exception handler terminates the process.
- WebView2 event delivery after control disposal and actual map browser cache/offline behavior.
- Layout at 125% DPI across supported screen sizes and installed fonts.

