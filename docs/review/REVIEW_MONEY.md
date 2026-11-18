# MONEY code review

Reviewed commit: `3fc82a1`

## Findings

### MONEY-01 — Major

**Evidence**

[CommissionService.cs](../../WayPlot.domain/Services/MainTransaction/CommissionService.cs), `PaidBlockedReason`:

> 403: `if (payableTiming == CommissionPayableTiming.AfterDownPayment && deal.DownPayment > 0)`
> 405: `var confirmed = deal.Payments`
> 406: `.Where(payment => payment.Status == PaymentStatus.Confirmed)`
> 407: `.Sum(payment => payment.Amount);`
> 408: `if (confirmed < deal.DownPayment)`

**Rule broken:** Design Contract, B2/B2b existing service surface (payable timing controls Paid); Design Contract, Schedule and overdue decisions (reservation fee and down payment are distinct obligations).

**What goes wrong:** With a ₱100,000 net deal, ₱10,000 reservation fee, and ₱40,000 down payment, confirming ₱30,000 against the reservation/down-payment obligations makes `confirmed == ₱30,000` only, so it blocks as expected. But confirming ₱40,000 total (₱10,000 reservation plus ₱30,000 toward down payment) passes the `confirmed < DownPayment` check and permits Mark Paid while ₱10,000 of the down payment remains unpaid. This violates `AfterDownPayment` timing.

**Suggested fix:** Determine how much confirmed money has been allocated to the down-payment obligation after the reservation obligation, then compare that amount to `DownPayment`. Reuse the schedule allocation logic so the two obligations are treated separately.

**Confidence:** High.

## Mutation and concurrency trace

The mutation methods in scope are listed below. Each writes through one `TenantUnitOfWork` / `TenantDbContext`; `SaveChangesAsync` is atomic in EF Core, and `TenantDbContext` configures the entity `Version` as a concurrency token. Methods that update multiple related entities save the whole change set together. The explicit `ExecuteInTransactionAsync` wrapper is used for `RefreshDealStatusesAsync` and `TerminateDealAsync`; the other listed methods rely on the single `SaveAsync`/`SaveChangesAsync` transaction.

| Method | Status / type changes | Transaction and Version path |
|---|---|---|
| `DealService.RefreshDealStatusesAsync` | Deal Current/Overdue | Explicit transaction; Deal token |
| `DealService.RequestClosedAsync` | Parcel Available→Reserved; pending Deal created | One `SaveAsync`; Parcel and Deal tokens; active-deal unique constraint |
| `DealService.ConfirmClosedAsync` | Deal, Parcel, Lead Closed, Contact Client, commissions/bonus created | One `SaveAsync`; Deal, Parcel, Lead, Contact and payout tokens |
| `DealService.ReturnRequestAsync` | Pending Deal Cancelled, Parcel Available, request cleared | One `SaveAsync`; lifecycle and parcel tokens |
| `DealService.TerminateDealAsync` (`CancelDealAsync` / `DefaultDealAsync`) | Deal Cancelled/Defaulted; Parcel Available if currently Sold/Leased; Contact Customer; commission statuses | Explicit transaction; lifecycle and parcel tokens |
| `DealService.RecordPaymentAsync` | Payment AwaitingAdmin | One `SaveAsync`; touches Deal Version; receipt uniqueness constraint |
| `DealService.ConfirmPaymentAsync` | Payment Confirmed; Deal FullyPaid if paid total reaches net | One `SaveAsync`; touches Deal Version and Payment token |
| `DealService.ReturnPaymentAsync` | Payment Returned | One `SaveAsync`; touches Deal Version and Payment token |
| `CommissionService.SaveRulesAsync` | Commission rules history setting | One `SaveAsync`; Setting Version token |
| `CommissionService.EnsureMissingFullyPaidAsync` | Backfills commissions / referral bonuses | One `SaveAsync`; payout tokens (also filtered to fully paid deals) |
| `CommissionService.ChangeStatusAsync` (`ApproveAsync` / `MarkPaidAsync`) | Commission or referral bonus status | One `SaveAsync`; payout Version token |
| `LeadService.ReassignLeadAsync`, `ReassignContactAsync`, `ReassignAllFromAgentAsync` | Assignment fields only; no money recipient changes | Transactional saves and row Version tokens; deal agent and payout recipients are not reassigned |

`DealScheduleBuilder.Build` and `DealService.BuildDealSummary` contain the schedule/balance/overdue calculations found in the reviewed path. Money values use `decimal`; no `double` or `float` was found in the deal, payment, schedule, summary, or commission calculations. `DueDay` clamps to 1–31 at schedule build, the first due day is strictly after the local confirmation date, and the final installment takes the remainder. These behaviors match the Design Contract examples/rules.

## Checked and found fine

- Concurrent payment confirmation and overpayment: confirmation checks the active deal and remaining confirmed balance, then touches the Deal Version in the same SaveChanges transaction. Simultaneous requests cannot both commit against the same Deal version; a loser receives a concurrency conflict. Awaiting payments are excluded from the balance until confirmed, as required.
- Confirming a returned payment: `ConfirmPaymentAsync` requires `AwaitingAdmin`; a returned payment cannot pass that guard.
- Cancelling/defaulting while a payment awaits confirmation: `TerminateDealAsync` refuses while any payment is `AwaitingAdmin`.
- Two Closed requests for one parcel: request creation reserves/touches the parcel and checks an active deal; the unique active-deal constraint and concurrency handling protect competing requests.
- Commissions use `Price - Discount` as their base; agent/manager shares are rounded to cents, and the company receives the remainder so shares total to the commission amount. Rules validate that configured shares total 100%.
- Pending/Approved payouts transition to Cancelled on deal termination; Paid payouts transition to PaidNeedsReview. Payable timing is read from the latest rules history, defaults to AfterDownPayment, and is enforced in the service method.
- Referral bonuses are created in the deal confirmation unit of work only when a referrer exists, and the add-if-missing path checks whether a bonus already exists. Reassignment methods do not move existing commission or referral bonus recipients.
- Termination changes a parcel to Available only when its current status is Sold or Leased. Contact type resets to Customer only when no other live deal exists for that contact.
- UI rules for termination and payment are repeated in the service validation; no money-specific rule was found enforced only in the inspected UI.

## Questions

- Should a bonus for a staff or contact referrer require that referrer to remain active/available at confirmation? Current code does so for staff and requires the contact row to exist; documentation does not specify this lifecycle rule.

## Could not verify without running the app

- Actual SQL Server transaction isolation and conflict behavior under simultaneous requests; this review confirms the EF transaction/version setup from source only.
- Whether the payment date UI always supplies a local `DateTime` as expected by `ToUniversalTime` for non-UTC values.
