# Manual testing

## Step 7, Phase 3 — Commissions and referral bonuses

### Agent

- Create a lead/contact with a referral source and, when applicable, select an existing contact, staff member, or outside referrer.
- Request Closed from a Negotiation lead and enter a down-payment amount. Before Tenant Admin confirmation, no commission or referral bonus should exist.
- On the Deals & payments tab, verify the Tenant Admin can see the requested down-payment amount before confirming.
- After confirmation, open Commissions. Verify the Agent sees only commission rows assigned to their own staff account and referral bonuses naming them as referrer; other agents' rows and company shares must not appear.
- Confirm the Agent cannot see Commission rules, Approve, or Mark Paid actions.

### Tenant Admin

- Open Commission rules and save a percentage commission with Agent/Company shares totaling 100%. For Tier C, verify the optional Branch Manager share participates in that total. Save again with changed values and confirm both versions appear in history after reopening.
- Try saving an invalid percentage/amount, shares not totaling 100%, or a Branch Manager share outside Tier C; confirm the service error is displayed and no rule version is added.
- Configure a fixed or percentage referral bonus and select both payable timings in turn. Confirm the configured timing is retained after reopening the rules screen.
- Confirm Closed for an eligible Agent request. Verify the commission shares, optional referral bonus, Closed lead, and StageHistory update are saved together. If rules are missing/invalid or the Tier C branch has no single active Branch Manager for a configured share, confirmation must be refused and the lead must remain unclosed.
- Verify a pending payout can be approved immediately, regardless of payment progress. With a nonzero down payment, verify Mark Paid is refused until confirmed payments reach that amount; then verify Pending → Approved → Paid and that Paid date is populated. The B2 down-payment rule applies regardless of the stored payable-timing setting.
- Verify the Tenant Admin sees all recipients, including Company shares, and can manage rules, approve, and mark payouts Paid.
- If the database is unavailable during Approve or Mark Paid, verify the save fails visibly. In two concurrent sessions against the same tenant database, try approving the same payout twice; only one transition should succeed.
- For an existing confirmed FullyPaid deal with no commission rows, open the Tenant Admin Commissions tab twice. Verify the missing shares are generated once and are not duplicated on the second load.
- Verify a Paid payout is visibly marked “Paid — review” if a deal-ending update flags it. The current Deal screen only cancels unconfirmed deals and has no Defaulted operation, so confirmed-deal cancellation/default flagging cannot currently be exercised.

### Branch Manager with a branch

- Open Commissions and verify the list contains the manager's own payouts and non-company payouts for that branch, but not company shares or other branches' payouts.
- Verify Commission rules, Approve, and Mark Paid are hidden and the list is read-only.

### Branch Manager without a branch

- Open Commissions and verify the screen shows the service message that the staff account has no assigned branch.
- Verify no rules, Approve, or Mark Paid actions are available.

### Super Admin

- Verify the Commissions tab is absent and no tenant commissions or referral bonuses are displayed.

## Run A — staff provisioning, branches, and integrity

### Preflight and migration order

- Run the read-only queries in `docs/sql/run_a_checks.sql` against Master and each tenant database; save the results without changing rows.
- For an existing tenant database, verify the exact sequence: migrate through `20261117123239_RunA_StaffBranchesAndIntegrity`, provision tenant Staff rows from Master logins, then migrate through `20261117130000_RunA_StaffForeignKeys`.
- Verify a fresh tenant follows the same sequence successfully: Part A creates the schema/branches structure, provisioning creates Staff rows, and Part B adds Staff actor foreign keys.
- If Part B stops, verify the error names the table, column, and orphan count. Verify no orphan rows were deleted or rewritten.

### Staff provisioning

- Run provisioning twice and verify exactly one active Staff row per Master login whose `TenantId` matches the tenant, with `Id = UserId`, username, full name, and role populated.
- Verify a soft-deleted or inactive matching Staff row is restored as the one active row, while duplicate Staff rows stop provisioning with a clear error.
- Verify a login-time lazy upsert still creates a missing Staff row when provisioning was not run.
- Verify `Staff.UserId` remains an application-level cross-database link and `AspNetUsers.BranchId` is not used.

### Branches

- In Tier C, verify exactly `Mandaluyong` and `Antipolo` are seeded idempotently.
- Verify `mgr.mandaluyong.c` and `ana.c` have `Staff.BranchId` set to Mandaluyong.
- Verify Tier A and Tier B tenant seeders do not create those branches.
- Verify `SessionStarter` reads the branch only from tenant `Staff.BranchId`.
- Create a new audited tenant record as a staff member and verify its inherited `BranchId` is the creator's Staff branch. Verify records created by a staff member without a branch remain nullable according to the current model.
- Verify `Staff.BranchId` rejects a nonexistent branch and uses restricted delete behavior.

### Integrity constraints and indexes

- Verify multiple leads can enter `Negotiation` for one parcel.
- Verify only one active, non-deleted deal can hold a parcel.
- Attempt to save a Deal whose `ContactId` or `ParcelId` differs from its Lead and verify the composite FK rejects it.
- Verify actor columns accept null where currently nullable, but reject non-null IDs that do not exist in `Staffs` after provisioning.
- Verify indexes exist for lead assignment/parcel lookup, parcel assignment, commission recipient, contact assignment, deal agent lookup, and the composite lead/deal paths.

## Run E — agent scoping and service-level authorization

### Tenant A: ana.a, ben.a, and admin.a

- Sign in as `ana.a`. Verify Contacts, Leads, Interactions, lead search, lead files, and each parcel's map lead list contain only Ana's assigned records. Contacts assigned to Ana must appear, as must a contact that is unassigned but has a lead assigned to Ana. Contacts and leads assigned only to Ben, and unassigned contacts/leads, must not appear.
- Sign in as `ben.a`. Verify the same screens show Ben's records only, not Ana's. Directly attempting to move Ana's lead or log an interaction on it must be refused with an ownership message and must not write a stage history or interaction.
- As `admin.a`, verify all contacts, leads, interactions, lead files, and each parcel's leads are visible, including unassigned records. Verify the Admin can capture contacts, attach leads, move stages (except Closed, which remains deal confirmation), log interactions, and create parcels/owners.
- As either Agent, capture or import a contact whose phone/email belongs to a contact assigned to the other Agent. Verify a `DuplicateInquiry` is still written, while the visible result says only that a contact already exists and is assigned to another agent; it must not include the other agent's name, the existing contact name, phone, email, parcel, or lead details.
- On the parcel map as `ana.a` and `ben.a`, verify all parcels remain visible but the selected parcel shows only that Agent's own leads. As `admin.a`, verify the same parcel shows every lead.

### Tenant C: Branch Manager

- Sign in as a Branch Manager with `Staff.BranchId` set. Verify Contacts, Leads, Interactions, lead files, and parcel-map lead lists are limited to that branch; parcels themselves remain visible tenant-wide. Verify New contact, Import, Attach lead, Move stage, and Log interaction actions are hidden.
- Call the mutation services directly as that Branch Manager and verify contact capture/import, lead attachment/stage change, interaction logging, parcel creation, and owner creation all refuse with a role-specific read-only message.
- Temporarily use a Branch Manager staff record with `BranchId = null` in a controlled test database. Verify Contacts, Leads, Interactions, lead files, parcels, and owner lists refuse the request with a clear "no assigned branch" message rather than returning tenant-wide data. Verify all mutation actions remain unavailable.

### Super Admin

- Sign in as Super Admin. Verify tenant data tabs are absent and direct service calls for contacts, leads, interactions, parcels, and imports are refused with a Super Admin message.
- Directly call contact capture/import, lead attachment/stage change, interaction logging, parcel creation, and owner creation as Super Admin; verify every mutation is refused with a Super Admin message.

## Run C1 — Leaflet parcel map

- Open the Parcel map with seeded parcels in each tenant. Verify polygons and latitude/longitude-only parcels appear at the expected Philippines locations and use green Available, amber Reserved, red Sold, and blue Leased styling.
- On a fresh seed, verify `Blk 1 Lot 1`, `Blk 1 Lot 2`, `Blk 1 Lot 4`, `Blk 2 Lot 2`, `Blk 2 Lot 4`, `Blk 3 Lot 1`, and `Blk 3 Lot 4` are Available; `Blk 1 Lot 3`, `Blk 2 Lot 3`, and `Blk 3 Lot 3` are Reserved; and `Blk 2 Lot 1` and `Blk 3 Lot 2` are Sold. Repeat in Tier A, B, and C.
- Click a parcel and verify it is highlighted and the inspector shows its label, status, price, reservation fee, area, price per sqm, zoning, utilities, road access, tax declaration and title numbers, owner details, and linked leads. Verify Attach lead remains available only to permitted roles.
- Click the empty map and verify the inspector closes and the selection highlight clears. Reopen a parcel and use Close; verify the same behavior.
- Drag the splitter and resize the window. Verify the map redraws to the available space. Close and reopen WayPlot, select a parcel, and verify the inspector width is restored.
- Block the configured tile server while leaving the app running. Verify parcel geometry remains visible on a plain background and the unobtrusive offline note appears.
- Sign in as an Agent and verify the parcel map displays only the leads permitted by Run E for each parcel; verify parcels remain visible tenant-wide.
- Use a parcel label containing `<` and verify it appears as literal text in the map label and inspector.
- Run without the Microsoft Edge WebView2 Runtime in a controlled environment and verify the map area explains what to install while other app screens remain usable.

## Run C2 — parcel drawing, editing, and permissions

### Agent `ana.a`

- Draw a polygon lot. Verify its area is prefilled and editable. Verify an existing parcel with only Latitude and Longitude still displays as a marker and its point shape can be edited.
- Verify Enter, double-click, Esc, and Backspace behave as indicated while drawing. Verify the Finish, Undo, and Cancel buttons work as keyboard fallbacks.
- Click an existing parcel during polygon drawing and verify it does not become selected.
- Edit Ana's own lot details and shape. Verify her price and reservation fee are read-only when editing.
- Verify Ana cannot edit a lot assigned to another Agent, change its price, reassign it, or archive it; unavailable edit actions should be muted with a tooltip reason.
- Try a duplicate parcel label with different letter casing and verify it is refused.

### Tenant Admin `admin.a`

- Edit any parcel's details and shape, change its price and reservation fee, and reassign it to `ben.a`. Verify the existing leads are not reassigned.
- Archive a parcel with no blocking deals or leads. Verify it disappears from the map and the Attach-lead parcel picker, while its label remains reserved.
- Try to archive a parcel with a deal not Cancelled or Defaulted, then one with a lead not Lost. Verify each attempt is refused and names the blocking status.
- Verify archive refuses a parcel with a Closed lead whose deal is FullyPaid or Current, showing the deal-status refusal message.
- Verify archive allows a parcel with a Closed lead when its deal is Cancelled or Defaulted.
- Verify a duplicate label is refused even when the matching parcel is archived.

### Agent `ben.a`

- Verify Ben can edit only parcels assigned to him and cannot change price, reassign, or archive.
- Verify parcel details and shapes remain available only for parcels he owns; Leads on other Agents' parcels are unchanged.

### All roles and Tenant C

- Verify no parcel dialog or map message can set Status by hand; new parcels begin Available.
- Use a parcel label containing `<` and verify it displays as text in the map and inspector.
- In Tenant C, sign in as the Branch Manager. Verify all parcel mutation buttons are hidden, direct mutation attempts are refused, and no draw mode can be started.

## Run C3 — parcel photos in the tenant database

- Start the API, then confirm all three tenants reach Active at SchemaVersion 5.
- As `ana.a`, add several JPG and PNG photos to her parcel; verify thumbnails appear and each full image opens. Verify the 11th photo is refused, a non-image file renamed `.jpg` is refused, and a very large accepted photo is reduced.
- As `ben.a` in a second login, verify he can view Ana's parcel photos but cannot add or remove photos on a parcel not assigned to him.
- As `admin.a`, remove a photo and verify its stored image data is freed. Archive a parcel and verify its photos remain stored.
- As the Tenant C Branch Manager, verify photos are viewable within the assigned branch and read-only.
- With many photos stored, verify map and parcel-list queries do not load photo data.

## Run U1 — map and shell fixes

- Click a lot and verify the inspector opens and the map zooms to it. Select a parcel from a list or another tab and verify the map also zooms to it.
- Close the inspector and verify the map keeps its current view without zooming out. Verify the existing Fit parcels control still fits all parcels.
- Verify Place marker is gone and new parcels can be created only by drawing a polygon. Verify an old latitude/longitude-only marker parcel still displays and can have its shape edited.
- Verify Import appears only as the Contacts tab's Import CSV button, is available only to Agents and Tenant Admins, and opens the working CSV import flow.
- Plot a polygon and verify the confirmation appears in the status label below the map, not over the map, and disappears automatically after 4 seconds.
- Sign out, confirm the prompt, and verify Login appears again in the same process. Sign in as a different user and verify only that user's data and role-appropriate tabs appear.
- Move a lead stage with a note and verify the note is saved beside its history entry. Verify an ordinary move may omit a note and moving to Lost requires a note as the reason.

## Run B2 — sale lifecycle, payments, and commissions

### Tenant A — `ana.a` and `admin.a`

- As `ana.a`, request Closed on a Negotiation lead and verify the parcel changes from Available to Reserved. Request Closed again on the same parcel and verify the refusal names the existing pending/current deal.
- As `admin.a`, return the pending request and verify the parcel returns to Available, while the lead remains in Negotiation. Request again, confirm as Sold, and verify the parcel is Sold, the lead is Closed with a StageHistory row, the contact type is Client, and commissions are Pending (plus a Pending referral bonus if applicable).
- On another eligible parcel, request Closed and confirm as Leased. Verify the parcel is Leased and the map uses the blue Leased style.
- Verify Tenant Admin can request Closed on any eligible Negotiation lead; Agents can request only on their own leads. Branch Managers are read-only and Super Admin is refused.
- As `ana.a`, record a payment with amount, date, method, and receipt number. Verify the balance does not decrease until `admin.a` confirms it. Verify a duplicate receipt number, a duplicate amount/date on the same deal, and an overpayment are refused.
- Confirm payments until the net balance reaches zero. Verify the deal becomes FullyPaid and the parcel status does not change during recording or confirmation of payments.
- Verify a Tenant Admin can approve a Pending commission before payments reach the down payment. Use the PayableTiming checks in Run B2b for the Paid threshold.
- Verify deal and payout statuses are visible. As `ben.a`, verify Ana's deals and payments are not visible; verify an Agent sees only their own payout rows.
- Verify Branch Manager in Tenant C can view branch-scoped deals/payments/commissions but cannot mutate them.

## Run B2b — deal termination and payable timing

- Start the API and verify all three tenants reach `SchemaVersion` 6 after `AddDealTermination`.
- As `admin.a`, cancel a Current deal with Refunded, another with PartlyRefunded and a valid amount, and default a third with Kept. Verify each deal's status and termination fields, its Sold/Leased parcel returns to Available (green on the map), Pending/Approved commissions and referral bonuses become Cancelled, Paid payouts become PaidNeedsReview, the lead stays Closed, and the contact becomes Customer only when it has no other Current, Overdue, or FullyPaid deal.
- Verify a FullyPaid deal cannot be cancelled or defaulted, and a deal with any AwaitingAdmin payment is refused with “Confirm or return the pending payments first.”
- Verify PartlyRefunded is refused when the amount is zero, negative, or at/above the confirmed-paid total. Verify the dialog requires a reason (maximum 500 characters) and a money outcome, enables the amount only for PartlyRefunded, and displays the confirmed-paid total and payout-effect summary.
- As `ana.a`, verify Cancel and Default are hidden and direct service calls refuse. Verify Branch Managers and Super Admin are refused.
- As `admin.a`, request Closed on an unassigned Negotiation lead and verify it is refused with “Assign this lead to an agent before requesting Closed.” Verify requests on assigned leads retain the assigned Agent as the deal's agent.
- Configure PayableTiming as OnConfirmation and verify an approved commission or referral bonus can be marked Paid after deal confirmation without waiting for the down payment. Configure AfterDownPayment and verify Paid is refused until confirmed payments reach the down payment (when above zero). Verify a missing or unset timing uses AfterDownPayment, and approval is never gated.
- Verify `admin.a` can record a payment on any accessible confirmed deal, and can still confirm it.

## Foundations (B3): verified through the screens built in U2, F2, P1 and P2

- Open a contact as `ana.a` or `admin.a` and verify the People kind filter shows Customers, Clients and Landowners. Verify an owner and matching landowner contact with the same normalized phone appear once, while a name-only match remains separate.
- Verify Agent deal-card summaries show only the Agent's deals, with net price, confirmed/pending totals, balance, percent paid, next due date, overdue amount and paid/installment counts.
- Verify an installment Current deal, an Overdue deal and a FullyPaid deal reconcile to the displayed schedule and to the stored net price.
- Verify an unconfirmed/returned deal timeline shows request-created and request-returned events; confirmed and cancelled/defaulted deals show their existing stored dates and termination details.
- Add a lead for a contact on a parcel; verify it starts at New, is assigned correctly, and refuses lost/closed or already-open one-per-lot conflicts and unavailable parcels.
- Switch an eligible lead to an Available lot; verify the new lead starts at New and the old lead becomes Lost with the switch reason.
- Open the pipeline board and verify every column comes from the stage provider, closed/lost default to the last 30 days, and the all-filter includes older rows.
- Verify allowed-move options reproduce Closed through request/confirmation and Lost requiring a note; Agents cannot move leads assigned to another Agent.
- Verify the lead timeline merges stage history, interactions and deal events newest-first and converts UTC times to local time.
- As Admin, reassign a lead, optionally reassigned open leads with a contact, and reassign all records from one Agent to another. Verify Closed/Lost leads, deals, payments and commissions are never reassigned and cross-branch moves are refused.
- Open the Staff list as Admin and Branch Manager; verify active staff, open-lead counts and parcel counts, with Branch Manager rows branch-scoped.

## Run U2 — Contacts list and lead actions

- Sign in as `ana.a` and `ben.a`; verify the people list follows Run E and the two lists differ.
- Sign in as `admin.a`; verify all people appear and the All agents filter plus each Agent filter works.
- Apply All, Customers, Clients and Landowners filters; verify each row kind and parcel/balance columns match.
- Verify a Landowner contact and an Owner with the same normalized phone appear as one row, including the Owner's parcel count.
- Select a person and verify the detail panel opens; close it and verify it hides. Resize it and reopen it after recreating the tab; verify the width is remembered.
- Search by phone with spaces and `+63`; verify the matching person is found.
- For a Customer or Client, verify the detail shows kind, phone, email, source, consent and assigned agent, plus lead cards.
- Use Add lead on an Available parcel; verify the lead is created at New. Verify Reserved and Sold parcels are not offered.
- Verify an already-open lead on the parcel is refused. Switch to an Available lot and verify the old lead is Lost and a new lead starts at New.
- As `ben.a`, verify Ana's lead cannot be switched and the service refusal is shown.
- Use Show on map from a lead or Landowner parcel and verify the Map tab opens, selects the parcel and zooms.
- Verify the New contact dialog offers only Customer and Landowner. Attempt service calls with `Client` and verify the exact automatic-status refusal.
- In CSV import, add a `Type`, `ContactType` or `Kind` column containing `Client`; verify that row is refused and CSV without that column defaults to Customer.
- As Branch Manager or Super Admin, verify New contact, Import CSV, Add lead and Switch lot are hidden. Verify direct mutations remain refused.

## Run F2 — Contact deal cards and money summary

- As `ana.a`, verify the request Closed action appears only for a lead allowed by `GetAllowedMovesAsync`; open the deal request for a Negotiation lead and confirm an Awaiting confirmation card is shown.
- As `admin.a`, confirm the pending deal as Sold or Leased; verify the card becomes Current and the parcel/contact statuses update.
- Record a payment as `ana.a`; verify the card's bar remains unchanged until `admin.a` confirms it, while the payment shows AwaitingAdmin.
- Confirm the payment as Admin; verify the bar and remaining balance move and the payment becomes Confirmed.
- Verify an overdue deal shows the red Overdue chip beside the name, the overdue amount and overdue day count.
- Verify a FullyPaid deal displays blue, 100%, and no next payment.
- Verify a Cancelled or Defaulted card is muted and shows reason, money outcome and refund amount where applicable.
- As `ben.a`, verify Ana's deals and customers are not visible; Branch Manager cards are read-only.
- Verify Agent users have no Deals & payments tab; Tenant Admin retains the existing Deals tab.
- Verify the list displays a red text Overdue chip while retaining the Balance column.
- Verify all money uses `en-PH` formatting with two decimals, including the custom money bar.
- Resize the contact detail panel and set 125 percent DPI; verify deal cards wrap and do not clip.

## Run P1 — Pipeline board

- Sign in as `ana.a`; verify the board contains only Ana's leads in provider stage order.
- Drag an allowed lead card to a destination column; verify the move note dialog opens and the card moves only after confirmation.
- Cancel the move dialog; verify the card remains in place.
- Drag onto a disallowed column; verify the reason is shown and the board reloads.
- Drop an eligible card onto Closed; verify Request Closed opens and a successful request shows the Closed requested chip.
- Move to Lost; verify Lost requires a note.
- Open Move to...; verify only allowed stages are listed.
- Sign in as `ben.a`; verify Ben cannot move Ana's card.
- Sign in as `admin.a`; verify all leads, parcel filter and agent filter work and search filters the cards.
- Verify columns with more than 100 cards show the first-100 notice.
- Log call, text, email, viewing or note from the drawer; verify the timeline updates and the board reloads.
- Verify Open contact selects the contact and Show on map opens the pending parcel selection.
- Sign in as a Branch Manager; verify no move, log, switch or Request Closed controls appear, while the drawer and timeline remain readable.
- Reassign a lead and verify Days in Stage is not reset.
- Sign out and in as a different user; verify the pipeline contains only that user's scoped leads.
