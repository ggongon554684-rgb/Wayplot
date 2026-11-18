# ACCESS code review

Reviewed commit: `3fc82a1`

No `AGENTS.md` exists in the repository or its parent directory, so rule 1 could not be quoted or checked. The comparison below uses the Design Contract and the product documentation’s “Who can see what” section.

## Finding

### ACCESS-01 — Major

**Evidence**

[ContactService.cs](../../WayPlot.domain/Services/DataCollection/ContactService.cs), `ListPeopleAsync`:

> 167: `var allLeads = await uow.Leads.ListAsync(null, filter.SearchText, scope: scope.Value);`
> 168: `var allDeals = await uow.Deals.ListAsync(`
> 171: `var parcels = await uow.Parcels.ListAsync();`
> 172: `if (scope.Value.Kind == TenantDataScopeKind.Branch)`
> 173: `    parcels = parcels.Where(parcel => parcel.BranchId == scope.Value.BranchId).ToList();`

[EfRepositories.cs](../../WayPlot.infrastructure/Repositories/EfRepositories.cs), `OwnerRepository.ListAsync`:

> 16: `public async Task<IReadOnlyList<Owner>> ListAsync() =>`
> 17: `    await db.Owners.AsNoTracking().OrderBy(o => o.Name).ToListAsync();`

**Rule broken:** Design Contract, B3 “People” (`owners follow parcel visibility`); documentation, “Who can see what” (role-limited records; Agents see own customers, Branch Managers their branch).

**What goes wrong:** An Agent opening the people list causes the service to fetch every parcel and every owner in the tenant. The service narrows parcels only for Branch Managers, and never narrows owner rows for Agents. The Agent therefore gets all standalone landowner names, phone numbers, and emails in `PeopleListItemDto`, including owners whose parcels are assigned to other agents. Branch Managers have parcel filtering but it happens after the unscoped parcel query, contrary to the required query-level scoping. Steps: sign in as Agent A, open People and select Landowner; every tenant owner not merged into a contact can appear, even if their parcels belong to Agent B.

**Suggested fix:** Scope the parcel query by the current agent/branch before loading it and derive the allowed owner IDs from those scoped parcels. Query only those owners and build parcel counts from the same scoped parcel set.

**Confidence:** High.

## Public service method authorization matrix

`Policy?` indicates use of `TenantAccessPolicy` directly in the method or its shared mutation helper. “Read policy” allows Tenant Admin (tenant scope), Agent (own scope), and Branch Manager (branch scope, failing closed without a branch); Super Admin is refused. “Mutation policy” allows identified Agents and Tenant Admins, with resource-specific checks noted. A method delegated to a private helper is marked by that helper.

| Service method | Policy? | Effective access |
|---|---|---|
| `ContactService.CaptureAsync` | Yes | Mutation policy; agent ownership rules and duplicate masking applied |
| `ContactService.GetAsync`, `ListAsync`, `ListPeopleAsync`, `ListReferralOptionsAsync` | Yes | Read policy; scoped by role, except ACCESS-01 owner/parcel leak; referral staff options are tenant wide |
| `ContactService.RecordInquiryOnExistingAsync` (private helper) | Yes | Existing contact visibility and lead ownership checked; masks duplicate identity |
| `ContactImportService.PreviewAsync`, `ImportAsync` | Yes | Preview uses read scope; import uses mutation policy; scoped matching for contacts |
| `InteractionService.LogAsync` | Yes | Mutation policy plus lead ownership |
| `InteractionService.ListAsync` | Yes | Read policy and repository query scope |
| `LeadService.AttachContactToParcelAsync`, `AddLeadForContactAsync` | Yes | Mutation policy; read scope and target eligibility checks |
| `LeadService.ListByParcelAsync`, `ListByContactAsync`, `ListAsync`, `GetFileAsync`, `GetPipelineBoardAsync`, `GetAllowedMovesAsync`, `GetLeadTimelineAsync` | Yes | Read policy and scoped repository query; role scopes as above |
| `LeadService.SwitchLotAsync`, `MoveStageAsync` | Yes | Mutation policy plus lead ownership |
| `LeadService.ReassignLeadAsync`, `ReassignContactAsync`, `ReassignAllFromAgentAsync` | Yes | Tenant Admin only; destination active Agent; branch move restrictions |
| `ParcelPhotoService.ListPhotosAsync`, `GetPhotoImageAsync` | Yes | Parcel read policy and branch ownership check |
| `ParcelPhotoService.AddPhotoAsync`, `RemovePhotoAsync` | Yes | Parcel edit/ownership checks |
| `ParcelService.CreateOwnerAsync` | Yes | Mutation policy; agent/admin |
| `ParcelService.ListOwnersAsync`, `ListAsync`, `GetAsync` | Yes | Parcel read policy; currently returns tenant-wide owners and parcels for Agent/Admin, branch scope for Branch Manager. Agent parcel visibility is not assigned-parcel-only in policy/docs. |
| `ParcelService.ListActiveAgentsAsync` | Yes | Mutation policy before listing; agent/admin |
| `ParcelService.CreateAsync`, `UpdateAsync` | Yes | Plot/edit parcel policy; agent assigned parcel restrictions where applicable |
| `ParcelService.ChangePriceAsync`, `ReassignAsync`, `ArchiveAsync` | Yes | Tenant Admin only |
| `DealService.ListAsync`, `GetDealSummaryAsync`, `GetDealSummariesForContactAsync`, `GetTerminationSummaryAsync`, `GetPaymentsAsync`, `ListPaymentsAsync` | Yes | Read policy and deal owner/branch scope. Contact summary loads deals first then filters in memory. |
| `DealService.RefreshDealStatusesAsync` | No | Internal system operation; invoked after payment confirmation. No user-facing data returned. |
| `DealService.ListCandidatesAsync`, `RequestClosedAsync`, `ReturnRequestAsync` | Yes | Mutation policy; lead ownership checked for request/return |
| `DealService.ConfirmClosedAsync` | Yes | Tenant Admin only |
| `DealService.CancelDealAsync`, `DefaultDealAsync` | Yes, via helper | Tenant Admin only; deal access scope checked |
| `DealService.RecordPaymentAsync` | Yes | Mutation policy plus deal access scope; Agent own deals, Admin all |
| `DealService.ConfirmPaymentAsync`, `ReturnPaymentAsync` | Yes | Tenant Admin only |
| `CommissionService.GetRulesAsync`, `SaveRulesAsync` | Yes | Tenant Admin only |
| `CommissionService.ListAsync` | Yes | Read policy; Agent own payouts, Branch Manager own plus branch payouts, Admin tenant payouts |
| `CommissionService.ApproveAsync`, `MarkPaidAsync` | Yes, via helper | Tenant Admin only |
| `StaffService.ListStaffAsync` | Yes | Tenant Admin tenant staff; Branch Manager own branch; Agent returns no staff rows |
| `TenantAccessPolicy` public methods | N/A (policy implementation) | Central role/scope decisions; see policy behavior above |

## SQL scoping and leak-channel trace

- **Contacts:** contact search/get/referral-contact queries apply Agent and Branch predicates in SQL. Agent visibility includes contacts assigned to them or attached to their leads. Duplicate matching by phone/email is intentionally tenant-wide to prevent duplicate creation; response identity is masked for contacts outside the caller’s scope.
- **Leads, interactions, pipeline, board, timeline:** repositories apply Agent/Branch predicates in SQL. Timeline uses a scoped lead lookup and scoped interactions/history before creating DTOs. Stage history’s only broad access is by a lead ID after the caller has scoped that lead.
- **Deals and summaries:** deal lists and payment lists use agent/branch predicates in SQL. `GetDealSummaryAsync` loads one row by ID before `DealAccessFailure`; this is a scoped identifier lookup and returns a generic not-found/outside-scope failure. `GetDealSummariesForContactAsync` loads every deal for the contact then filters in memory; DTO filtering is present, but SQL scoping is not. It does not reveal rows in its returned list, though it needlessly loads out-of-scope entities.
- **Commissions/referral bonuses:** list predicates are applied in SQL for recipient and branch. Tenant Admin sees company shares; Branch Manager sees branch and personal payouts; Agent sees own recipient payouts. A Branch Manager’s combined own/branch predicate is intentional. Single-payout mutations fetch by ID then validate Tenant Admin role; payout data is not returned to unauthorized roles.
- **Parcels and photos:** photo repository queries filter by parcel/photo ID; service first verifies the parcel/photo is visible, including Branch Manager branch check. Parcel lists/get and owners are role-gated, but Agents and Tenant Admins are tenant wide under the current parcel policy. Owner data in `ListPeopleAsync` violates the stronger B3 “owners follow parcel visibility” rule (ACCESS-01).
- **People:** contacts, leads and deals use role scoping, but parcels/owners are fetched broadly and only Branch Manager parcels/owners are filtered after loading. Agent owner visibility leaks as described above. The assigned-staff filter is discarded for non-admins rather than exposing arbitrary assignments.
- **Staff:** main staff list query is SQL scoped to branch. However, `ContactService.ListReferralOptionsAsync` obtains all active referral staff and returns names/roles to every role allowed to read contacts; its staff query is not scoped by Agent/Branch. It exposes other staff names, although the documentation does not explicitly prohibit seeing staff identities. No finding raised absent a clear confidentiality rule.
- **`IgnoreQueryFilters`:** `ParcelRepository.LabelExistsAsync` is the intended label check. `LeadRepository.ListStagesByParcelAsync` also calls `IgnoreQueryFilters` and returns stages for every lead on a parcel, including soft-deleted rows. The method is used for parcel deletion/eligibility rather than presenting contact details; no direct leak was established from the call path reviewed.
- **Includes/projections:** joins/includes in scoped lead, interaction, deal and payout repositories are constrained through the root entity’s SQL predicate. No separate agent name leakage was found in those projections. People’s assigned-agent names are based on staff IDs from scoped contacts/leads; the owner leak is separate.
- **Counts/refusal messages:** People counts derive from the scoped contact/lead/deal lists except parcel/owner counts (ACCESS-01). Duplicate refusal messages do not disclose another contact’s name or ID. Out-of-scope deal lookups return generic refusal text. Branch Manager read methods fail closed when branch ID is missing; Super Admin is refused by read/mutation policy.
- **Direct UI access:** no presenter, view, or UI class was found referencing a `TenantDbContext`, repository, or unit-of-work factory. Repository access remains behind services.

## Checked and found fine

- Super Admin read scopes fail and mutation policy refuses tenant data.
- Branch Manager scope creation fails when `BranchId` is absent.
- Agent contact visibility through assigned leads is implemented in the contact repository query.
- Duplicate identity masking prevents the capture flow from returning an inaccessible contact’s name or ID.
- Unassigned leads are excluded from Agent lead queries; Admin tenant scope includes them.
- No UI/repository bypass was found in presenters or views.

## Questions

- Does product policy intend agents to see every tenant parcel and owner through Parcel Map/List? Current policy allows parcel reads for any Agent; product documentation describes Agents plotting parcels but does not state that parcel visibility is restricted to assigned parcels. B3 specifically says owners follow parcel visibility, so ACCESS-01 uses that explicit rule.
- Should active referral-staff choices be branch/agent scoped? The current method exposes names and roles across the tenant to callers who can view contacts, but documentation does not state that staff identities are confidential.
- Should `ListStagesByParcelAsync` include soft-deleted leads? It explicitly bypasses query filters, but the affected output contains only stage values and its exact deletion-related purpose appears non-disclosing.

## Could not verify without running the app

- SQL generated for EF Core query predicates and global filters, and actual data returned under each role.
- Whether UI navigation fully prevents callers from reaching service methods that return role-scoped refusals; source shows service-side checks, but runtime composition was not exercised.
