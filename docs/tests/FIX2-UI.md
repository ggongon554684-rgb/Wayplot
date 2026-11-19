# FIX2 — Runtime bug fixes and UI polish

**Run date:** 2026-11-18  
**Scope:** WayPlot WinForms project only. No schema changes. No migrations.

---

## Changes made

### 1. AttachLeadDialog crash — `ArgumentNullException (container)` fixed
**File:** `WayPlot/Views/AttachLeadDialog.cs` line 16  
**Root cause:** `components` field is `null` at the point `new System.Windows.Forms.Timer(components!)` is called because `InitializeComponent()` never creates a `Container` for it.  
**Fix:** Changed to `new System.Windows.Forms.Timer { Interval = 350 }` (no container argument). Added explicit `_searchTimer?.Dispose()` in `AttachLeadDialog.Designer.cs` `Dispose(bool)` so the timer is released when the dialog closes.

### 2. Pipeline board — empty column boxes on first load
**File:** `WayPlot/Views/PipelineView.cs`  
**Root cause:** `Height = boardPanel.ClientSize.Height` in `ShowBoard` evaluates to 0 when the panel has not yet completed its first layout pass. Columns rendered as zero-height boxes.  
**Fix:**  
- In `ShowBoard`: `var colHeight = Math.Max(400, boardPanel.ClientSize.Height)` — guarantees a visible minimum.  
- Added `boardPanel.Resize` handler in constructor that updates all column heights to `Math.Max(400, boardPanel.ClientSize.Height)` whenever the panel is resized.

### 3. Contacts — "Loading contacts..." never cleared
**File:** `WayPlot/Views/ContactsView.cs` `ShowContacts`  
**Root cause:** Guard condition `if (lblSummary.Text.StartsWith("Loading", ...))` prevented the label from updating after any prior error status was set.  
**Fix:** Removed the guard — `ShowContacts` always sets `lblSummary.Text = _normalStatus` and `ForeColor = Theme.Muted`.

### 4. Contact detail panel — UI polish
**Files:** `WayPlot/Views/ContactsView.Designer.cs`, `ContactsView.cs`  
**Changes:**
- `detailPanel` 1 px left padding + border-colour `BackColor` → visible separator line.
- `detailTop` proper padding, `Height = 52`, column styles, `lblDetailName` vertically centred; `detailTop.BackColor = Theme.Surface`.
- `detailScroll` and `detailBody` padding for breathing room.
- `AddLeadCard` rewritten using `TableLayoutPanel` (body) + `FlowLayoutPanel` (buttons) — buttons wrap and are never clipped at any panel width.
- Lead card `BorderStyle = FixedSingle`; `Width` auto-sizes to panel width.

---

## Verification checklist

- [ ] Stop WayPlot.exe → `dotnet build WayPlot\WayPlot.csproj` → restart.
- [ ] Click **Attach lead**: dialog must open without crashing.
- [ ] **Pipeline** tab without checkbox: columns must be full-height with cards.
- [ ] Resize window: pipeline columns must stretch.
- [ ] **Contacts** tab: label shows "N people" immediately.
- [ ] Select a contact with multiple leads: all buttons ("Show on map", "Switch lot", "Request Closed") are fully visible.
- [ ] Narrow the detail pane: buttons wrap rather than overflow.
- [ ] 1 px border separator is visible between list and detail.

---

## What could not be verified
No runtime execution (no DB, no UI driver in this environment). Role-based button visibility not runtime-tested.
