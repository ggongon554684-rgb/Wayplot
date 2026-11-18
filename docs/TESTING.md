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
- With “After down payment” configured and a nonzero down payment, try Approve before enough payments are confirmed; verify it is refused with “Wait until the down payment is confirmed.” Confirm payments reaching the down-payment amount, then Approve and Mark Paid. Verify Pending → Approved → Paid and that Paid date is populated.
- With “At confirmation” configured, verify an eligible payout can be approved immediately after Closed confirmation.
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
- Click a parcel and verify it is highlighted and the inspector shows its label, status, price, reservation fee, area, price per sqm, zoning, utilities, road access, tax declaration and title numbers, owner details, and linked leads. Verify Attach lead remains available only to permitted roles.
- Click the empty map and verify the inspector closes and the selection highlight clears. Reopen a parcel and use Close; verify the same behavior.
- Drag the splitter and resize the window. Verify the map redraws to the available space. Close and reopen WayPlot, select a parcel, and verify the inspector width is restored.
- Block the configured tile server while leaving the app running. Verify parcel geometry remains visible on a plain background and the unobtrusive offline note appears.
- Sign in as an Agent and verify the parcel map displays only the leads permitted by Run E for each parcel; verify parcels remain visible tenant-wide.
- Use a parcel label containing `<` and verify it appears as literal text in the map label and inspector.
- Run without the Microsoft Edge WebView2 Runtime in a controlled environment and verify the map area explains what to install while other app screens remain usable.

## Run C2 — parcel drawing, editing, and permissions

### Agent `ana.a`

- Draw a polygon lot and place a marker lot. Verify both appear at the clicked locations, the polygon area is prefilled and editable, and the marker's area can be entered.
- Verify Enter, double-click, Esc, and Backspace behave as indicated while drawing. Verify the Finish, Undo, and Cancel buttons work as keyboard fallbacks.
- Click an existing parcel during polygon drawing and verify it does not become selected.
- Edit Ana's own lot details and shape. Verify her price and reservation fee are read-only when editing.
- Verify Ana cannot edit a lot assigned to another Agent, change its price, reassign it, or archive it; unavailable edit actions should be muted with a tooltip reason.
- Try a duplicate parcel label with different letter casing and verify it is refused.

### Tenant Admin `admin.a`

- Edit any parcel's details and shape, change its price and reservation fee, and reassign it to `ben.a`. Verify the existing leads are not reassigned.
- Archive a parcel with no blocking deals or leads. Verify it disappears from the map and the Attach-lead parcel picker, while its label remains reserved.
- Try to archive a parcel with a deal not Cancelled or Defaulted, then one with a lead not Lost. Verify each attempt is refused and names the blocking status.
- Verify a duplicate label is refused even when the matching parcel is archived.

### Agent `ben.a`

- Verify Ben can edit only parcels assigned to him and cannot change price, reassign, or archive.
- Verify parcel details and shapes remain available only for parcels he owns; Leads on other Agents' parcels are unchanged.

### All roles and Tenant C

- Verify no parcel dialog or map message can set Status by hand; new parcels begin Available.
- Use a parcel label containing `<` and verify it displays as text in the map and inspector.
- In Tenant C, sign in as the Branch Manager. Verify all parcel mutation buttons are hidden, direct mutation attempts are refused, and no draw mode can be started.
