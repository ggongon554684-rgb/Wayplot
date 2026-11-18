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
