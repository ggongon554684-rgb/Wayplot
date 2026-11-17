# WayPlot schema, mapping, and parcel-permission audit

## Scope and method

Read-only audit of the requested documentation, current domain model, EF Core contexts, migrations, repositories/services, seeders, presenters, and map control. No API was started, no database was opened, and no source or migration was changed. Evidence paths are repository-relative and line numbers refer to the files inspected on 2026-11-17.

The schema has two physically separate databases:

- Master: ASP.NET Identity logins plus plans, tenants, and subscriptions.
- Tenant: one common schema copied to each tenant database.

The current tenant migration is `20261116101911_Init_Tenant`; `20261117110000_AddDuplicateInquiries` adds `DuplicateInquiries`.

## Executive findings

| Severity | Finding |
|---|---|
| High | Staff and branch actor columns are mostly bare GUIDs; there is no `Branches` table, and the master-to-tenant login relationship cannot be a normal SQL FK. Staff rows are created lazily at tenant login, not guaranteed by the master seeder. |
| High | Parcel authorization is not implemented in `ParcelService`: create accepts a caller-supplied status and has no role check; edit, reassign, archive/delete, price-change, approval, and photo mutation service paths are absent. |
| High | `Deal` duplicates `LeadId`, `ContactId`, and `ParcelId` without a database constraint that the three paths agree. A deal can drift from its lead. |
| High | `UX_Lead_OneNegotiationPerParcel` forbids several simultaneous Negotiation leads for one parcel, contrary to the product documentation's several-buyers scenario. |
| Medium | Contact email is indexed but not unique; the duplicate check says phone OR email, so concurrent or differently-cased email duplicates remain possible. |
| Medium | Contact/parcel/assignment/branch query indexes are incomplete. `Commissions` has the generated `DealId` index, but no `RecipientStaffId` index; `Leads` has no `AssignedStaffId` or plain `ParcelId` index beyond the restrictive filtered unique index. |
| Medium | `Payment` receipt uniqueness is filtered only by `IsDeleted`, while the receipt generator searches ignored soft-deleted rows; the semantics should be made explicit before changing it. |
| Medium | Status is intended to be system-driven, but parcel creation accepts arbitrary `ParcelStatus`, and there is no service-layer parcel status policy for future mutation paths. |
| Low | Referral storage is a polymorphic four-column shape with a copied name but no stored outside-referrer phone on `ReferralBonus`; it is workable as a snapshot but not a clean normalized referrer model. |
| Low | `AreaSqm` is stored and not derived from geometry; this is acceptable as an entered/business value only if validation/reconciliation is defined. |

## 1. Tables, columns, keys, FKs, and indexes

### Conventions

All tenant entities inheriting `BaseEntity` have `Id uniqueidentifier NOT NULL` PK, `BranchId uniqueidentifier NULL`, `CreatedAtUtc datetime2 NOT NULL`, `CreatedBy nvarchar(100) NOT NULL`, `UpdatedAtUtc datetime2 NOT NULL`, `UpdatedBy nvarchar(100) NOT NULL`, `Version int NOT NULL`, `SyncStatus nvarchar(30) NOT NULL`, and `IsDeleted bit NOT NULL`, unless noted otherwise. They have a global query filter `IsDeleted = 0`; FKs are `RESTRICT` (`TenantDbContext.cs:65-68,132-143`). `Outbox` does not inherit `BaseEntity` and has no soft-delete/audit columns.

### Tenant database

| Table and columns (SQL type; nullability) | Key/FK/index notes |
|---|---|
| `Staffs`: `Id uniqueidentifier N`; `UserId uniqueidentifier N`; `Username nvarchar(100) N`; `FullName nvarchar(max) N`; `Role nvarchar(max) N`; `IsActive bit N`; plus base columns | PK `Id`; unique `IX_Staffs_UserId` on `UserId`; no FK to master `AspNetUsers`; `BranchId` has no FK. (`Platform.cs:5-13`, migration `Init_Tenant.cs:110-131,650-653`) |
| `Settings`: `Id`; `Key nvarchar(100) N`; `Value nvarchar(max) N`; plus base | PK; unique `IX_Settings_Key` on `Key`. (`Platform.cs:15-20`, `Init_Tenant.cs:89-107,644-648`) |
| `Outbox`: `Id uniqueidentifier N`; `ChangeId uniqueidentifier N`; `TableName nvarchar(100) N`; `RecordId uniqueidentifier N`; `Operation nvarchar(max) N`; `PayloadJson nvarchar(max) N`; `CreatedAtUtc datetime2 N`; `Attempts int N` | PK; nonunique `IX_Outbox_CreatedAtUtc`; no FK and no unique `ChangeId` despite the entity comment saying retries must be idempotent. (`Platform.cs:22-34`, `TenantDbContext.cs:126-130`) |
| `Owners`: `Id`; `Name nvarchar(max) N`; `Phone nvarchar(max) N`; `Email nvarchar(max) NULL`; `ListingAgreementOnFile bit N`; plus base | PK; no uniqueness on owner name/phone/email. (`DataCollection.cs:5-11`) |
| `Parcels`: `Id`; `Label nvarchar(60) N`; `Description nvarchar(max) N`; `AreaSqm decimal(18,2) N`; `Zoning nvarchar(max) N`; `Price decimal(18,2) N`; `ReservationFee decimal(18,2) N`; `Status nvarchar(30) N`; `GeoJson nvarchar(max) NULL`; `Latitude float NULL`; `Longitude float NULL`; `TaxDeclarationNo nvarchar(max) N`; `TitleNo nvarchar(max) N`; `Utilities nvarchar(max) N`; `RoadAccess nvarchar(max) N`; `OwnerId uniqueidentifier N`; `AssignedStaffId uniqueidentifier NULL`; plus base | PK; FK `OwnerId -> Owners.Id` RESTRICT; unique filtered `UX_Parcel_Label(Label)` filter `[IsDeleted] = 0`; nonunique `IX_Parcels_OwnerId`; no staff/branch FK. (`DataCollection.cs:13-34`, `Init_Tenant.cs:134-172,615-624`) |
| `ParcelPhotos`: `Id`; `ParcelId uniqueidentifier N`; `FileName nvarchar(max) N`; `StoragePath nvarchar(max) N`; plus base | PK; FK `ParcelId -> Parcels.Id` RESTRICT; `IX_ParcelPhotos_ParcelId`. (`DataCollection.cs:36-42`, `Init_Tenant.cs:215-240,610-613`) |
| `Contacts`: `Id`; `FullName nvarchar(150) N`; `Phone nvarchar(max) NULL`; `PhoneNormalized nvarchar(11) NULL`; `Email nvarchar(200) NULL`; `Source nvarchar(30) N`; `Type nvarchar(30) N`; `ConsentSms bit N`; `ConsentEmail bit N`; `ConsentAtUtc datetime2 NULL`; `ReferrerContactId uniqueidentifier NULL`; `ReferrerStaffId uniqueidentifier NULL`; `ReferrerName nvarchar(max) NULL`; `ReferrerPhone nvarchar(max) NULL`; `AssignedStaffId uniqueidentifier NULL`; plus base | PK; nonunique `IX_Contacts_Email`; unique filtered `UX_Contact_Phone(PhoneNormalized)` filter `[PhoneNormalized] IS NOT NULL AND [IsDeleted] = 0`; no FKs for referrer/assigned columns. (`DataCollection.cs:44-61`, `Init_Tenant.cs:15-45,533-549`) |
| `DuplicateInquiries`: `Id`; `ContactId uniqueidentifier N`; `ParcelId uniqueidentifier NULL`; `Source nvarchar(30) N`; `OccurredAtUtc datetime2 N`; `RecordedByStaffId uniqueidentifier NULL`; `Details nvarchar(max) N`; plus base | PK; FKs `ContactId -> Contacts.Id` and `ParcelId -> Parcels.Id`, both RESTRICT; `IX_DuplicateInquiries_ContactId_OccurredAtUtc`, `IX_DuplicateInquiries_ParcelId`; no recorder FK. (`DuplicateInquiry.cs:5-14`, `AddDuplicateInquiries.cs:17-61`) |
| `Leads`: `Id`; `ContactId uniqueidentifier N`; `ParcelId uniqueidentifier N`; `Stage nvarchar(30) N`; `CloseRequested bit N`; `OfferAmount decimal(18,2) NULL`; `LostReason nvarchar(max) NULL`; `AppointmentAtUtc datetime2 NULL`; `LastActivityAtUtc datetime2 N`; `AssignedStaffId uniqueidentifier NULL`; plus base | PK; FKs `ContactId -> Contacts.Id`, `ParcelId -> Parcels.Id` RESTRICT; `IX_Leads_ContactId`; unique filtered `UX_Lead_OneNegotiationPerParcel(ParcelId)` filter `[Stage] = 'Negotiation' AND [IsDeleted] = 0`; no assigned-agent or ordinary parcel index. (`MainTransaction.cs:5-19`, `TenantDbContext.cs:108-112`) |
| `Interactions`: `Id`; `LeadId uniqueidentifier N`; `Type nvarchar(30) N`; `Channel nvarchar(30) N`; `OccurredAtUtc datetime2 N`; `Note nvarchar(max) N`; `Rating int NULL`; `ComplaintStatus nvarchar(30) NULL`; `ResolvedAtUtc datetime2 NULL`; `LoggedByStaffId uniqueidentifier NULL`; plus base | PK; FK `LeadId -> Leads.Id` RESTRICT; `IX_Interactions_LeadId`; no logged-by FK. (`MainTransaction.cs:21-35`) |
| `StageHistories`: `Id`; `LeadId uniqueidentifier N`; `FromStage nvarchar(30) NULL`; `ToStage nvarchar(30) N`; `ChangedAtUtc datetime2 N`; `ChangedByStaffId uniqueidentifier NULL`; `Note nvarchar(max) NULL`; plus base | PK; FK `LeadId -> Leads.Id` RESTRICT; `IX_StageHistories_LeadId`; no changed-by FK. (`MainTransaction.cs:37-47`) |
| `Deals`: `Id`; `LeadId uniqueidentifier N`; `ParcelId uniqueidentifier N`; `ContactId uniqueidentifier N`; `AgentStaffId uniqueidentifier N`; `Price decimal(18,2) N`; `Discount decimal(18,2) N`; `ReservationFee decimal(18,2) N`; `DownPayment decimal(18,2) N`; `IsInstallment bit N`; `InstallmentCount int N`; `InstallmentAmount decimal(18,2) N`; `DueDay int N`; `LatePenaltyPercentPerMonth decimal(18,2) N`; `Status nvarchar(30) N`; `IsActive bit N`; `ConfirmedAtUtc datetime2 NULL`; `ConfirmedByStaffId uniqueidentifier NULL`; plus base | PK; FKs `LeadId -> Leads.Id`, `ContactId -> Contacts.Id`, `ParcelId -> Parcels.Id` RESTRICT; `IX_Deals_LeadId`, `IX_Deals_ContactId`; unique filtered `UX_Deal_OneActivePerParcel(ParcelId)` filter `[IsActive] = 1`; no agent/confirming-staff FKs. (`DealsPayments.cs:5-30`, `Init_Tenant.cs:243-296,551-566`) |
| `Payments`: `Id`; `DealId uniqueidentifier N`; `PaidAtUtc datetime2 N`; `Amount decimal(18,2) N`; `Method nvarchar(30) N`; `ReceiptNo nvarchar(50) N`; `RecordedByStaffId uniqueidentifier NULL`; `Status nvarchar(30) N`; `ReviewNote nvarchar(max) NULL`; `ConfirmedAtUtc datetime2 NULL`; `ConfirmedByStaffId uniqueidentifier NULL`; plus base | PK; FK `DealId -> Deals.Id` RESTRICT; `IX_Payments_DealId`; unique filtered `UX_Payment_Receipt(ReceiptNo)` filter `[IsDeleted] = 0`; no recorder/confirming-staff FKs. (`DealsPayments.cs:32-45`, `Init_Tenant.cs:467-500,627-636`) |
| `Commissions`: `Id`; `DealId uniqueidentifier N`; `RecipientStaffId uniqueidentifier NULL`; `RecipientRole nvarchar(max) N`; `Rate decimal(18,2) NULL`; `Amount decimal(18,2) N`; `Status nvarchar(30) N`; `PaidAtUtc datetime2 NULL`; plus base | PK; FK `DealId -> Deals.Id` RESTRICT; generated `IX_Commissions_DealId`; no `RecipientStaffId` index or FK. (`DealsPayments.cs:47-57`, `Init_Tenant.cs:362-392,534-537`) |
| `ReferralBonuses`: `Id`; `DealId uniqueidentifier N`; `ReferrerContactId uniqueidentifier NULL`; `ReferrerStaffId uniqueidentifier NULL`; `ReferrerName nvarchar(max) N`; `Amount decimal(18,2) N`; `Status nvarchar(30) N`; `PaidAtUtc datetime2 NULL`; plus base | PK; FK `DealId -> Deals.Id` RESTRICT; `IX_ReferralBonuses_DealId`; no referrer FKs/indexes. (`DealsPayments.cs:59-69`) |
| `Documents`: `Id`; `DealId uniqueidentifier NULL`; `ParcelId uniqueidentifier NULL`; `Type nvarchar(30) N`; `FileName nvarchar(max) N`; `StoragePath nvarchar(max) N`; `UploadedByStaffId uniqueidentifier NULL`; plus base | PK; optional FKs `DealId -> Deals.Id`, `ParcelId -> Parcels.Id` RESTRICT; `IX_Documents_DealId`, `IX_Documents_ParcelId`; no uploader FK. (`DealsPayments.cs:71-81`) |
| `LandownerPayouts`: `Id`; `DealId uniqueidentifier N`; `OwnerId uniqueidentifier N`; `Amount decimal(18,2) N`; `PaidAtUtc datetime2 N`; `Reference nvarchar(max) N`; plus base | PK; FKs `DealId -> Deals.Id`, `OwnerId -> Owners.Id` RESTRICT; `IX_LandownerPayouts_DealId`, `IX_LandownerPayouts_OwnerId`; no payout recorder column. (`DealsPayments.cs:83-92`) |

### Master database

`MasterDbContext` uses ASP.NET Identity's standard schema plus three application tables (`MasterDbContext.cs:11-53`; migration `Init_Master.cs:14-302`). Identity string columns use the migration's standard `nvarchar(256)`/`nvarchar(max)` sizes; key types are `uniqueidentifier` except Identity claim `Id int IDENTITY`.

| Table and columns | Key/FK/index notes |
|---|---|
| `Plans`: `Id uniqueidentifier N`; `Name nvarchar(50) N`; `Tier nvarchar(10) N`; `MonthlyPrice decimal(18,2) N`; `IsActive bit N` | PK; no unique plan name/tier index. |
| `Tenants`: `Id`; `Name nvarchar(120) N`; `PlanId uniqueidentifier N`; `Status nvarchar(30) N`; `ServerName nvarchar(200) N`; `DatabaseName nvarchar(128) N`; `SchemaVersion int N`; `CreatedAtUtc datetime2 N`; `ExpiresAtUtc datetime2 NULL` | PK; FK `PlanId -> Plans.Id` RESTRICT; unique `IX_Tenants_DatabaseName`; `IX_Tenants_PlanId`. |
| `Subscriptions`: `Id`; `TenantId uniqueidentifier N`; `PlanId uniqueidentifier N`; `Status nvarchar(30) N`; `StartsAtUtc datetime2 N`; `ExpiresAtUtc datetime2 N`; `PaymentReference nvarchar(100) N` | PK; FKs to `Tenants.Id` and `Plans.Id`, RESTRICT; `IX_Subscriptions_TenantId`, `IX_Subscriptions_PlanId`; no uniqueness for one active subscription per tenant. |
| `AspNetUsers`: `Id`; `TenantId uniqueidentifier NULL`; `BranchId uniqueidentifier NULL`; `DisplayName nvarchar(120) N`; `IsActive bit N`; standard Identity `UserName/NormalizedUserName/Email/NormalizedEmail nvarchar(256) NULL`, confirmation/lockout bits, `PasswordHash/SecurityStamp/ConcurrencyStamp/PhoneNumber nvarchar(max) NULL`, `LockoutEnd datetimeoffset NULL`, `AccessFailedCount int N` | PK; FK `TenantId -> Tenants.Id` RESTRICT; `UserNameIndex` unique filtered on non-null normalized username; `EmailIndex`, `IX_AspNetUsers_TenantId`; `BranchId` has no FK. |
| `AspNetRoles`: `Id`; `Name nvarchar(256) NULL`; `NormalizedName nvarchar(256) NULL`; `ConcurrencyStamp nvarchar(max) NULL` | PK; unique filtered `RoleNameIndex` on non-null normalized name. |
| `AspNetRoleClaims`: `Id int IDENTITY N`; `RoleId uniqueidentifier N`; `ClaimType nvarchar(max) NULL`; `ClaimValue nvarchar(max) NULL` | PK; FK to `AspNetRoles.Id` CASCADE; `IX_AspNetRoleClaims_RoleId`. |
| `AspNetUserClaims`: `Id int IDENTITY N`; `UserId uniqueidentifier N`; `ClaimType nvarchar(max) NULL`; `ClaimValue nvarchar(max) NULL` | PK; FK to `AspNetUsers.Id` CASCADE; `IX_AspNetUserClaims_UserId`. |
| `AspNetUserLogins`: `LoginProvider nvarchar(450) N`; `ProviderKey nvarchar(450) N`; `ProviderDisplayName nvarchar(max) NULL`; `UserId uniqueidentifier N` | Composite PK `(LoginProvider, ProviderKey)`; FK `UserId -> AspNetUsers.Id` CASCADE; `IX_AspNetUserLogins_UserId`. |
| `AspNetUserRoles`: `UserId uniqueidentifier N`; `RoleId uniqueidentifier N` | Composite PK `(UserId, RoleId)`; FKs to users/roles CASCADE; `IX_AspNetUserRoles_RoleId`. |
| `AspNetUserTokens`: `UserId uniqueidentifier N`; `LoginProvider nvarchar(450) N`; `Name nvarchar(450) N`; `Value nvarchar(max) NULL` | Composite PK `(UserId, LoginProvider, Name)`; FK to users CASCADE. |

## 2. Normalization and duplicated data

### Deliberate snapshots or derived values

- `Deal.Price`, `Discount`, `ReservationFee`, `DownPayment`, installment terms, and commission `Rate`/`Amount` are transaction snapshots. They should remain stable when the parcel price or rules later change. Evidence: `DealsPayments.cs:13-26`, `CommissionService.cs:397-415`.
- Deal balance is **not stored**. `Deal` has no balance column; `DealService.ToListItem` computes confirmed payment/reservation values and remaining balance from `Deal.Payments` (`DealService.cs:363-390`). Payment acceptance also computes allocated and remaining amounts (`DealService.cs:246-252`). This is the correct general shape, but the two calculations use different concepts: overpayment checks count non-returned payments, display balance also counts non-returned payments, while reservation progress counts confirmed payments only. Keep it computed and centralize the definition.
- `ReferralBonus.ReferrerName` is a snapshot copied when the bonus is created (`CommissionService.cs:242-260,267-289`). That is deliberate for historical payout display. Outside referrer phone is not copied to the bonus.
- `Staff.Username` and `Staff.FullName` are tenant-local copies of the master login display data, refreshed at login (`SessionStarter.cs:32-57`). That is a useful tenant audit/display snapshot, but it is not a relational FK.
- `pricePerSqm` and `reservationPercent` are DTO-derived values, not stored columns (`ParcelService.cs:119-127`).

### Accidental or drift-prone duplication

- `Deal` stores `LeadId`, `ContactId`, and `ParcelId`, while `Lead` already stores `ContactId` and `ParcelId`. `DealService.CreateAsync` populates all three from the lead (`DealService.cs:107-115`), but the database only enforces that each independently exists. A later update, import, sync bug, or direct SQL write can point a deal to a different contact or parcel than its lead. **Severity: high.** Fix by making the lead the authoritative relationship and either removing the duplicate IDs (larger code change) or enforcing a composite relationship/validation that the deal's contact and parcel equal the lead's values before save and in migration data checks.
- `Deal.AgentStaffId` duplicates the current lead assignment concept (`Lead.AssignedStaffId`) but is a legitimate transaction snapshot of the selling agent at deal creation. The service currently takes the current user, not the lead's assignment, and first checks equality (`DealService.cs:81-116`); document it as immutable deal ownership.
- `Contact.ReferrerName`/`ReferrerPhone` alongside `ReferrerContactId`/`ReferrerStaffId` is intentionally polymorphic but permits null/invalid combinations at database level. Service validation permits only one source, but direct writes can violate it (`ContactService.cs:184-230` and `:ValidateReferrerFields` at the displayed lines around 170-190). Add a constraint or canonical referral table after data cleanup.
- `Owner` and `Contact` both hold person names/phones because an owner is a parcel-side party and a buyer contact is a CRM party. This is not automatically accidental duplication; there is no owner-to-contact FK or service that copies one into the other. The risk is duplicate people, not child-table denormalization. Consider a shared party model only as a later redesign.
- Child tables do not copy customer names or phone numbers. They reach them through `Deal -> Lead -> Contact`; only the referral bonus copies the referrer name as a snapshot.
- No money balance is duplicated onto `Contact`; contact screens must derive from deals/payments. `Deal` itself stores terms and derives current balance.

## 3. Integrity gaps and constraints

### Bare actor/branch GUIDs

There is no `Branches` entity/table anywhere in the inspected code; `BaseEntity.BranchId` is merely a nullable GUID (`BaseEntity.cs:8-18`). Therefore every inherited `BranchId` on `Staff`, `Setting`, `Owner`, `Parcel`, `ParcelPhoto`, `Contact`, `DuplicateInquiry`, `Lead`, `Interaction`, `StageHistory`, `Deal`, `Payment`, `Commission`, `ReferralBonus`, `Document`, and `LandownerPayout` is currently a bare branch GUID. `AppUser.BranchId` in Master is also bare (`AppUser.cs:5-11`).

The following tenant columns are bare staff/referrer actor GUIDs and need explicit policy before any FK is added:

- `Parcels.AssignedStaffId` (`DataCollection.cs:30-33`).
- `Contacts.ReferrerStaffId`, `Contacts.AssignedStaffId` (`DataCollection.cs:56-60`).
- `DuplicateInquiries.RecordedByStaffId` (`DuplicateInquiry.cs:9-14`).
- `Leads.AssignedStaffId` (`MainTransaction.cs:14-18`).
- `Interactions.LoggedByStaffId` and `StageHistories.ChangedByStaffId` (`MainTransaction.cs:27-45`).
- `Deals.AgentStaffId` and `Deals.ConfirmedByStaffId` (`DealsPayments.cs:9-27`).
- `Payments.RecordedByStaffId` and `Payments.ConfirmedByStaffId` (`DealsPayments.cs:34-44`).
- `Commissions.RecipientStaffId` (`DealsPayments.cs:49-55`).
- `ReferralBonuses.ReferrerStaffId` (`DealsPayments.cs:61-67`).
- `Documents.UploadedByStaffId` (`DealsPayments.cs:73-80`).
- `Staff.UserId`, which is a cross-database reference to Master `AspNetUsers.Id`, not a tenant-local staff FK (`Platform.cs:5-13`).
- `CreatedBy` and `UpdatedBy` are `nvarchar(100)`, not GUIDs, so they cannot be FKs; they are free-text audit identities (`BaseEntity.cs:12-15`).

**Severity: high.** Add a tenant `Branches` table and `Staff.BranchId` FK only after a migration/data-check plan. Staff FKs can reference `Staffs.Id` once every existing actor value is reconciled. Cross-database `Staff.UserId -> Master.AspNetUsers.Id` should remain an application-level check unless both databases are intentionally co-located.

### Login-to-Staff precondition

`MasterSeeder` creates nine demo logins, including the tenant accounts (`MasterSeeder.cs:72-100`), but it does not seed tenant `Staffs` rows. `SessionStarter` creates one row lazily, with `Staff.Id = login.UserId`, only after successful tenant login (`SessionStarter.cs:9-45`). It rejects soft-deleted/inactive rows and copies username/name/role (`SessionStarter.cs:47-60`). Consequently:

- For a fresh tenant, tenant logins that have successfully started a session will have matching `Staffs.UserId` rows; a master login that never logged into that tenant will not.
- `superadmin` has no tenant and must not have a tenant Staff row (`MasterSeeder.cs:72-76`, `AuthController.cs:32-41`).
- Existing tenant databases can contain master tenant users with no staff row, inactive/deleted staff, or a staff row whose role/name has not yet been refreshed. A direct FK to `Staffs.UserId` would fail for missing rows; a FK to `Staffs.Id` would not solve the master-to-tenant completeness problem.
- Branch assignment is not copied from `AppUser.BranchId`; `SessionStarter` explicitly starts with null and then reads `staff.BranchId` (`SessionStarter.cs:20-23,60`). The seeded branch manager therefore has no branch unless another uninspected management path assigns it. **Severity: high.**

Fix order: make provisioning/seeding/upsert guarantee staff rows for every tenant login, reconcile existing rows, then add only tenant-local staff FKs. Do not add Staff FKs before that guarantee.

### Missing indexes

The current migration contains `IX_Commissions_DealId` (`Init_Tenant.cs:534-537`), so the claim that Commissions has only its PK is not true for the checked migration. It does lack `RecipientStaffId`, which is used by commission listing filters (`EfRepositories.cs:157-180`). `Leads` has only `IX_Leads_ContactId` plus the filtered unique parcel index; it lacks ordinary `ParcelId` and `AssignedStaffId` indexes despite service queries (`EfRepositories.cs:242-282`). Other likely additions are `Contacts.AssignedStaffId`, `Parcels.AssignedStaffId`, `Deals.AgentStaffId`, payment recorder/confirmers, and history/interactions actor columns, but add only where query plans justify them. **Severity: medium.**

### Uniqueness and filtered indexes

- Receipt number: unique filtered `UX_Payment_Receipt` on non-soft-deleted rows only (`TenantDbContext.cs:120-124`, migration `Init_Tenant.cs:627-636`). The generator scans `IgnoreQueryFilters()` including deleted rows (`EfRepositories.cs:390-403`), which avoids reusing old sequences but is not the same as the unique constraint's scope. Decide whether receipt numbers are globally immutable (recommended) and then align index and generator.
- Parcel label: unique per tenant among non-deleted parcels via `UX_Parcel_Label` (`TenantDbContext.cs:82-86`). This is not branch-scoped; the requested interpretation is tenant-wide uniqueness.
- Contact email: only a nonunique `IX_Contacts_Email`; email is normalized to lowercase in the service, but no unique filtered email index exists (`TenantDbContext.cs:88-98`, `ContactService.cs:93-95`). The service duplicate check is phone OR email, so add a unique filtered email index if one contact per email is intended, after checking existing duplicates. **Severity: medium.**
- `UX_Lead_OneNegotiationPerParcel` allows only one non-deleted lead in `Negotiation` for a parcel. The documentation describes several buyers potentially negotiating for one lot, and the requested audit explicitly says that requirement is not documented. **Severity: high/design decision.** Recommend removing this index and keeping only the active-deal uniqueness guard; if the business wants a reservation hold, model that explicitly rather than blocking all negotiations.
- `UX_Deal_OneActivePerParcel` allows only one `IsActive = 1` deal, including an unconfirmed close request. This is a reasonable race guard, but its filter does not include `IsDeleted = 0`; soft-deleted active rows can still block a new deal. Verify intended archive semantics before changing it. **Severity: medium.**

### Decimal and enum storage

All mapped `decimal` properties are conventionally `decimal(18,2)` in both contexts (`TenantDbContext.cs:44-46`, `MasterDbContext.cs:18-23`), and the migrations show money as `decimal(18,2)`. No money column found outside that precision. `double` is used only for latitude/longitude, which is appropriate for the current simple map model.

Enums in both contexts are stored as strings, not integers (`TenantDbContext.cs:47-60`, `MasterDbContext.cs:20-23`). Therefore inserting/reordering `LeadSource.Import` (`Enums.cs:9`) will not corrupt existing values. Any future raw integer data or external consumers should still be checked, but the stated integer-enum failure is not present in this schema.

## 4. Referral source model

At contact capture, exactly one of these is intended: `ReferrerContactId` for an existing customer/contact, `ReferrerStaffId` for an active staff member, or `ReferrerName` + `ReferrerPhone` for an outsider (`ContactService.cs:ValidateReferrerFields` and `:ValidateReferrerAsync` in the referral block; `ContactService.cs:120-137,184-230`). Existing contacts and staff are validated; outsider phone is normalized for validation but the stored `ReferrerPhone` is the trimmed input, not the normalized number. Re-capturing an existing contact can add a duplicate inquiry and will not overwrite a different existing referrer (`ContactService.cs:RecordInquiryOnExistingAsync`, `ApplyReferrer`).

When the deal is confirmed, `CommissionService` resolves the selected contact/staff to a current name or uses the outsider name, then creates one `ReferralBonus` with `DealId`, one referrer ID, copied `ReferrerName`, amount, and status (`CommissionService.cs:242-260,267-289`).

Assessment: service-level exclusivity is clean enough for the current workflow, and the copied name is a useful payout snapshot. Database integrity is weak: there is no check constraint enforcing exactly one source, no FK for contact/staff, no normalized outsider phone, and no `ReferrerPhone` snapshot in `ReferralBonus`. **Severity: medium/low.** Proposed fix: introduce a small referral-source value object/table or add a check constraint after cleaning rows; preserve historical name and normalized phone on the bonus if audit needs the outsider's details.

## 5. Parcel and map model

- Geometry is stored as nullable `Parcel.GeoJson` text (`DataCollection.cs:22-25`). The service only verifies that it parses as JSON, not that it is valid GeoJSON, a supported geometry, or a consistent CRS (`ParcelService.cs:63-68,132-143`).
- A marker fallback is stored as nullable `Latitude` and `Longitude` (`float` in SQL, C# `double?`). The service requires both or neither and validates latitude [-90,90], longitude [-180,180] (`ParcelService.cs:65-68`).
- GeoJSON coordinates are interpreted as `[longitude, latitude]`, converted from degrees to Web Mercator-like `(x,y)` radians for drawing. The builder supports Polygon, MultiPolygon, Point, Feature, FeatureCollection, and GeometryCollection; it falls back to the marker if no usable polygon exists (`ParcelShapes.cs:71-103,106-200`). It includes a Philippines-oriented latitude/longitude swap safety net (`ParcelShapes.cs:185-194`). The stored CRS is implicitly WGS84 geographic degrees; no CRS column or database spatial type is present.
- `AreaSqm` is entered and stored independently. The seeder calculates area before generating demo polygons (`TenantSeeder.cs:194-215`), and the runtime never calculates polygon area. This can drift from geometry. **Severity: low/medium.** Proposed fix: state whether area is authoritative entered metadata or derive/validate it on polygon save; do not silently overwrite a business area without a product decision.
- Photos are linked by `ParcelPhoto.ParcelId` with `FileName` and `StoragePath`, and a restricted FK (`DataCollection.cs:36-42`, `Init_Tenant.cs:215-240`). `ParcelService` does not expose photo create/edit/delete; the current parcel DTO does not include photos (`ParcelService.cs:119-127`).
- Owner linkage is `Parcel.OwnerId -> Owners.Id` RESTRICT. Owner details are not copied into parcels (`DataCollection.cs:30-31`, migration `Init_Tenant.cs:152-171`).
- Parcel status is initialized from a caller-supplied `CreateParcelRequest.Status` in `ParcelService.CreateAsync` (`ParcelService.cs:79-96`). Later payment confirmation changes `Available -> Reserved` at reservation-fee threshold and to `Sold` when confirmed payments reach price (`DealService.cs:313-329`). Cancellation currently says it does not change parcel status (`DealService.cs:176-198`), so the documented return-to-Available behavior is not implemented. There is no Lease transition path found. **Severity: high.**
- The WinForms map is a read-only renderer/selector in the inspected UI: `ParcelMapPresenter` loads DTOs, builds shapes, selects parcels, and attaches leads (`ParcelMapPresenter.cs:40-121`); `ParcelMapView` shows facts and leads but has no create/edit parcel controls (`ParcelMapView.cs:23-38`, designer lines 93-118); `ParcelMapControl` only draws, pans, zooms, and hit-tests. Thus the documented draw workflow is not currently connected to a parcel mutation UI.

## 6. Parcel permission and mutation audit

| Operation | Code path found | Current service role policy | Gap/fix |
|---|---|---|---|
| Create owner | `ParcelService.CreateOwnerAsync` (`ParcelService.cs:14-37`) | No role check | Add service authorization; policy was not explicitly supplied for standalone owner creation. |
| Create parcel | `ParcelService.CreateAsync` (`ParcelService.cs:45-102`); called by `TenantSeeder` (`TenantSeeder.cs:204-217`) | No role check; accepts caller-supplied status and owner | High: enforce Agent/Tenant Admin, Branch Manager only own branch Tier C, never Super Admin; force initial status and audit branch. |
| Edit parcel fields | No `Update`/edit method found in `ParcelService`, presenter, or map view | None | Add role-aware service method. Tenant Admin any; Agent own/assigned (open decision); Branch Manager own branch. |
| Change price | No method found; create only accepts `Price` | No restriction because no update path | Add explicit Tenant Admin-only method; block price changes through generic edit. |
| Reassign parcel | No method found; `AssignedStaffId` exists but is not set by `CreateAsync` | None | Add Tenant Admin-only method and enforce same-tenant staff/branch rules. |
| Review/approve plotted parcel | No approval field or method found | None | Add explicit review state/workflow or document that creation is immediately approved; requested fix prompt requires Tenant Admin review/approval. |
| Archive/delete parcel | No parcel delete/archive service method found. EF `SaveChanges` converts entity deletes to soft delete (`TenantDbContext.cs:187-193`) | No role check at service layer | Add Tenant Admin-only archive; refuse hard delete/physical delete when referenced by lead/deal, and preserve history. |
| Change status | `DealService.ConfirmPaymentAsync` (`DealService.cs:293-339`) changes it from payment events; create can set it by request | No role check on create; no hand-edited update path | Make status system-driven only; implement cancellation/default transitions and recalculation rules. |
| Change photos | No photo service/UI mutation path found; only entity/FK exists | None | Add role policy consistent with parcel edit, with service enforcement and muted UI. |

The requested target policy is therefore not a small UI patch: the service layer needs the missing operations and a shared authorization policy first. UI controls should then be hidden or muted with the existing shared button helper, not treated as authorization.

## 7. Staff management

Master login creation is in `MasterSeeder`; it creates users and roles but does not create tenant staff (`MasterSeeder.cs:72-100`). On desktop login, `SessionStarter.StartAsync` opens the tenant, creates a tenant `Staff` row keyed by the login's `UserId`, or refreshes username/name/role. It rejects inactive/deleted staff and sets `SessionContext.BranchId` from the tenant staff row (`SessionStarter.cs:13-68`).

Branch assignment is incomplete: `AppUser` has a master `BranchId`, but `SessionStarter` sets the session branch to null before reading only `Staff.BranchId`; it never copies `AppUser.BranchId` (`AppUser.cs:5-11`, `SessionStarter.cs:20-23,60`). There is no branch entity, branch management service, or branch assignment UI in the repository. The Tier C demo Branch Manager therefore cannot reliably get a branch. **Severity: high.** Proposed fix: decide whether branch ownership is master-level or tenant-level, add a tenant `Branches` table and management path, seed/assign branch rows before adding FKs, and make session startup use one authoritative assignment.

## Finding register

| ID | Severity | Evidence | Proposed fix |
|---|---|---|---|
| F-01 | High | `Deal.cs:7-12`; `DealService.cs:107-115` | Remove redundant deal IDs or enforce lead/contact/parcel agreement with data checks and service validation. |
| F-02 | High | `TenantDbContext.cs:108-112`; `PipelineRules.cs:32-33`; documentation scenario says several buyers may negotiate | Remove `UX_Lead_OneNegotiationPerParcel`; retain an explicit active-deal race guard. |
| F-03 | High | `SessionStarter.cs:13-60`; `MasterSeeder.cs:72-100` | Provision Staff rows for all tenant logins before actor FKs; reconcile missing/inactive/deleted rows. |
| F-04 | High | `BaseEntity.cs:11`; no Branch entity; `AppUser.cs:9`; `SessionStarter.cs:20-23,60` | Add branch model/assignment and data checks; then add branch FKs. |
| F-05 | High | `ParcelService.cs:45-102`; no update/archive methods; `ParcelMapView.cs` | Implement service authorization and missing mutation/review/archive operations; UI follows service policy. |
| F-06 | High | `ParcelService.cs:87`; `DealService.cs:313-329`; `:176-198` | Force initial Available/pending status, centralize system status transitions, implement cancel/default recalculation. |
| F-07 | Medium | `ContactService.cs:36-38`; `TenantDbContext.cs:88-98` | Data-check emails, then add a filtered unique normalized-email index if one email maps to one contact. |
| F-08 | Medium | `EfRepositories.cs:242-282`; `Init_Tenant.cs:534-537,593-603` | Add indexes for actual assignment/parcel/recipient filters after query-plan review. |
| F-09 | Medium | `TenantDbContext.cs:120-124`; `EfRepositories.cs:390-403` | Decide immutable receipt scope; align generator and filtered index, preserving historical receipt numbers. |
| F-10 | Medium | `CommissionService.cs:242-289`; `ContactService.cs` referral validation | Normalize and snapshot outside referrer data; enforce one referral source at DB/service boundaries. |
| F-11 | Low/Medium | `Parcel.cs:17`; `TenantSeeder.cs:194-215`; `ParcelShapeBuilder` | Decide whether AreaSqm is authoritative or derived; add validation/reconciliation, not silent drift. |
| F-12 | Low | `TenantDbContext.cs:47-60`; `Enums.cs:9` | No fix required for current enum storage; keep string conversions and test unknown values. |

