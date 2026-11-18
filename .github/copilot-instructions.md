# WayPlot: standing rules for coding agents

Multi-tenant, map-based CRM for land and real estate businesses. Staff are the only users. Customers and landowners are records and never log in.

## Roles and review split
- Copilot implements. Codex reviews and runs read-only checks. A read-only task changes no files.
- Work on your own branch (copilot/<run-name>). Do not merge. Do not touch other branches.
- Do only the step you were given. Do not start any other step.
- If git status shows uncommitted changes in a file the run needs, stop and report.

## Solution layout
- Projects: WayPlot (WinForms), WayPlot.api, WayPlot.domain, WayPlot.infrastructure. All net10.0.
- NuGet packages are pinned to 10.0.*. Do not add packages unless the run says so. Version 11.x breaks the build.
- Build with: dotnet build WayPlot\WayPlot.csproj
- Do not start the API. Do not connect to or change any database.
- Do not commit bin, obj or .vs.

## Code rules
- Services return ServiceResult<T> with Ok, Message and Value. Failures carry a clear message.
- Role and scope checks go through TenantAccessPolicy. Super Admin is refused all tenant data. Branch Manager reads are branch-scoped, and Branch Manager mutations are refused. Agents see only their own work.
- Mutations use ExecuteInTransactionAsync and the entity Version token. A Version conflict is refused with a clean message.
- Each data change raises its existing domain event.
- Enums are stored as numbers in the database. Never reorder or insert enum members; only append at the end. In new JSON settings, write enums as text and keep reading old numeric values.
- Pipeline stages come only from IPipelineStageProvider. Do not loop over LeadStage, compare stages by order, or hard-code stage names. Do not call ToString() on LeadStage for display.
- No schema change and no migration unless the run says so.
- Do not change a rule from an earlier run.

## UI rules
- Designer-based layouts only.
- A control that cannot be clicked is shown muted, with a tooltip giving the reason.
- Keyboard accessible. Never use colour as the only meaning.
- A role that should not see a feature does not see it, and the service still refuses it.
- Every async void handler must catch its exceptions and show a short non-sensitive message.

## Docs
- docs/DESIGN_CONTRACT.md is the contract for services, rules and worked examples. Update it only when the run says so.
- WayPlot_CRM_Documentation_v3txt.txt is the product rationale and use cases. Do not edit it unless the run says so.
- Test checklists go in docs/tests/<RunName>.md, not TESTING.md.

## Reporting
End every run with: files changed, every method added, assumptions, and what you could not verify. If you could not run or see something, say so.