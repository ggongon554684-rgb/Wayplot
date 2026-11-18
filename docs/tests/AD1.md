# AD1 Tenant Admin screens checklist

## Team and access

- [ ] Tenant Admin sees Team and Review; Agents and Branch Managers do not see either tab.
- [ ] Direct service calls for staff mutations, reassignment, review actions and commission rules refuse Agents, Branch Managers and Super Admin.
- [ ] Branch Manager staff reads show only the Branch Manager's branch and scoped lead/parcel counts.
- [ ] Invite and Change role are muted, disabled, and show `Needs the account service (planned)` on hover.
- [ ] Staff role is read-only text.
- [ ] Tenant Admin cannot deactivate their own account or the last active Tenant Admin; corresponding controls are muted with a reason.
- [ ] An Agent with open leads or available parcels cannot be deactivated; the refusal includes both counts and points to Reassign.
- [ ] A deactivated Staff account cannot sign in; after reactivation the account can sign in again.
- [ ] Tier C branch changes accept only an existing branch and refuse cleanly on a Version conflict.

## Reassignment

- [ ] Single lead, contact (with optional open leads), and parcel reassignment use the existing service methods and active Agents.
- [ ] Reassign-all preview counts appear in the confirmation; the completion result reports actual moved counts.
- [ ] Reassign-all moves contacts, open leads and assigned parcels while retaining lead history, deals and payouts.
- [ ] Tier C refuses a source/destination move across branches for lead, contact, parcel and bulk reassignment.

## Review

- [ ] One grouped list shows counts for Closed requests awaiting confirmation, payments awaiting confirmation, and Pending/Approved commissions and referral bonuses.
- [ ] No cancel-request group is shown because the application has no cancel-request flow.
- [ ] Closed requests can be confirmed or returned; payments can be confirmed or returned; Pending payouts can be approved and Approved payouts can be marked Paid.
- [ ] Confirmed/returned requests and payments leave their queue groups; Paid payouts leave the unpaid-payout group.

## Rules and company profile

- [ ] Commission rules include percentage/fixed calculations, recipient shares, Agent/parcel amount overrides, Agent-first precedence, referral bonus and payable timing.
- [ ] Current rules display Version and last-changed-by/when; a successful save reports the new Version.
- [ ] Concurrent rule saves produce a clean conflict message.
- [ ] Legacy numeric enum values load; a new rules save writes `CommissionCalculationType` and `CommissionPayableTiming` as text.
- [ ] Any tenant-readable role can open the company profile read-only; only Tenant Admin can save.
- [ ] Company name, contact numbers, email and address validate their maximum lengths; a saved profile survives sign out and sign in.

## Not covered by the current application surface

- The repository has no document-generation or print flow to render the company profile on a generated document.
- There is no cancel-request flow or cancellation review queue.
- Database-backed sign-in, concurrent multi-admin saves, Tier C reassignment, and profile persistence require a tenant database and were not exercised by the automated test suite.
