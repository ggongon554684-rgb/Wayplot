/*
   Run A preflight checks. READ-ONLY: every statement is SELECT-only.

   Run the tenant queries while connected to each tenant database, before
   applying 20261117123239_RunA_StaffBranchesAndIntegrity. The Staff actor
   checks are run again by 20261117130000_RunA_StaffForeignKeys after the
   tenant provisioning step.

   The Master query is separate because Master and tenant databases are
   intentionally separate. Compare its UserId list with tenant Staffs.UserId.
*/

-- Master database: every tenant login that must be provisioned.
SELECT
    u.Id AS UserId,
    u.UserName,
    u.DisplayName,
    u.TenantId,
    r.Name AS RoleName
FROM AspNetUsers AS u
LEFT JOIN AspNetUserRoles AS ur ON ur.UserId = u.Id
LEFT JOIN AspNetRoles AS r ON r.Id = ur.RoleId
WHERE u.TenantId IS NOT NULL
ORDER BY u.TenantId, u.UserName;

-- Tenant database: duplicate login-to-Staff rows.
SELECT UserId, COUNT(*) AS StaffRowCount
FROM Staffs
GROUP BY UserId
HAVING COUNT(*) > 1;

-- Tenant database: Staff rows whose UserId is not in the Master result above
-- must be reviewed; this query is intentionally local and cannot infer Master.
SELECT Id, UserId, Username, FullName, Role, IsActive, IsDeleted, BranchId
FROM Staffs
ORDER BY Username, Id;

-- Tenant database: branch references that would fail the Staff.BranchId FK.
SELECT s.Id AS StaffId, s.UserId, s.BranchId
FROM Staffs AS s
LEFT JOIN Branches AS b ON b.Id = s.BranchId
WHERE s.BranchId IS NOT NULL AND b.Id IS NULL;

-- Tenant database: Deal snapshot consistency.
SELECT d.Id AS DealId, d.LeadId, d.ContactId AS DealContactId, l.ContactId AS LeadContactId,
       d.ParcelId AS DealParcelId, l.ParcelId AS LeadParcelId
FROM Deals AS d
LEFT JOIN Leads AS l ON l.Id = d.LeadId
WHERE l.Id IS NULL
   OR d.ContactId <> l.ContactId
   OR d.ParcelId <> l.ParcelId;

-- Tenant database: orphan actor IDs. Each result names the table and column.
SELECT 'Contacts' AS TableName, 'AssignedStaffId' AS ColumnName, COUNT(*) AS OrphanCount
FROM Contacts AS x LEFT JOIN Staffs AS s ON s.Id = x.AssignedStaffId
WHERE x.AssignedStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Contacts', 'ReferrerStaffId', COUNT(*)
FROM Contacts AS x LEFT JOIN Staffs AS s ON s.Id = x.ReferrerStaffId
WHERE x.ReferrerStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Commissions', 'RecipientStaffId', COUNT(*)
FROM Commissions AS x LEFT JOIN Staffs AS s ON s.Id = x.RecipientStaffId
WHERE x.RecipientStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Deals', 'AgentStaffId', COUNT(*)
FROM Deals AS x LEFT JOIN Staffs AS s ON s.Id = x.AgentStaffId
WHERE x.AgentStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Deals', 'ConfirmedByStaffId', COUNT(*)
FROM Deals AS x LEFT JOIN Staffs AS s ON s.Id = x.ConfirmedByStaffId
WHERE x.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Documents', 'UploadedByStaffId', COUNT(*)
FROM Documents AS x LEFT JOIN Staffs AS s ON s.Id = x.UploadedByStaffId
WHERE x.UploadedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'DuplicateInquiries', 'RecordedByStaffId', COUNT(*)
FROM DuplicateInquiries AS x LEFT JOIN Staffs AS s ON s.Id = x.RecordedByStaffId
WHERE x.RecordedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Interactions', 'LoggedByStaffId', COUNT(*)
FROM Interactions AS x LEFT JOIN Staffs AS s ON s.Id = x.LoggedByStaffId
WHERE x.LoggedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Leads', 'AssignedStaffId', COUNT(*)
FROM Leads AS x LEFT JOIN Staffs AS s ON s.Id = x.AssignedStaffId
WHERE x.AssignedStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Parcels', 'AssignedStaffId', COUNT(*)
FROM Parcels AS x LEFT JOIN Staffs AS s ON s.Id = x.AssignedStaffId
WHERE x.AssignedStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Payments', 'RecordedByStaffId', COUNT(*)
FROM Payments AS x LEFT JOIN Staffs AS s ON s.Id = x.RecordedByStaffId
WHERE x.RecordedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'Payments', 'ConfirmedByStaffId', COUNT(*)
FROM Payments AS x LEFT JOIN Staffs AS s ON s.Id = x.ConfirmedByStaffId
WHERE x.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'ReferralBonuses', 'ReferrerStaffId', COUNT(*)
FROM ReferralBonuses AS x LEFT JOIN Staffs AS s ON s.Id = x.ReferrerStaffId
WHERE x.ReferrerStaffId IS NOT NULL AND s.Id IS NULL
UNION ALL
SELECT 'StageHistories', 'ChangedByStaffId', COUNT(*)
FROM StageHistories AS x LEFT JOIN Staffs AS s ON s.Id = x.ChangedByStaffId
WHERE x.ChangedByStaffId IS NOT NULL AND s.Id IS NULL;
