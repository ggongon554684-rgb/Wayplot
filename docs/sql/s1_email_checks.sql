/*
   S1 contact-email preflight checks. READ-ONLY: every statement is SELECT-only.

   Run these queries against each tenant database before applying the
   S1_ContactEmailUnique migration. Duplicate normalized groups and non-null
   blank emails stop the migration. Merge or correct those contacts, then
   rerun the migration; the migration never rewrites or deletes contact rows.

   Differently formatted values are informational only. The unique Email
   index relies on the tenant database's default collation being
   case-insensitive. Verify that assumption for each tenant database.
*/

-- Non-deleted contacts sharing the same trimmed, lowercase email.
SELECT
    LOWER(LTRIM(RTRIM(Email))) AS NormalizedEmail,
    COUNT(*) AS ContactCount,
    STRING_AGG(CONVERT(varchar(max), Id), ',') AS ContactIds
FROM dbo.Contacts
WHERE IsDeleted = 0
  AND Email IS NOT NULL
GROUP BY LOWER(LTRIM(RTRIM(Email)))
HAVING COUNT(*) > 1;

-- Non-deleted contacts whose non-null email is blank after trimming.
SELECT Id, Email
FROM dbo.Contacts
WHERE IsDeleted = 0
  AND Email IS NOT NULL
  AND LTRIM(RTRIM(Email)) = '';

-- Informational: stored values that differ byte-for-byte from trim/lowercase.
SELECT Id, Email, LOWER(LTRIM(RTRIM(Email))) AS NormalizedEmail
FROM dbo.Contacts
WHERE IsDeleted = 0
  AND Email IS NOT NULL
  AND CONVERT(varbinary(max), Email)
      <> CONVERT(varbinary(max), LOWER(LTRIM(RTRIM(Email))));
