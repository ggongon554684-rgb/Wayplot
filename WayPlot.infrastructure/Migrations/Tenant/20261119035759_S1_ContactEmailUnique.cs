using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPlot.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class S1_ContactEmailUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Duplicate normalized groups and non-null blank values stop the migration.
            // Operators must merge or correct those rows and rerun; this guard changes no data.
            // Differently formatted distinct values are informational only. This relies on each
            // tenant's default collation being case-insensitive; verify that assumption per tenant.
            migrationBuilder.Sql("""
                DECLARE @duplicateGroupCount int;
                DECLARE @blankRowCount int;

                SELECT @duplicateGroupCount = COUNT(*)
                FROM
                (
                    SELECT LOWER(LTRIM(RTRIM(Email))) AS NormalizedEmail
                    FROM dbo.Contacts
                    WHERE IsDeleted = 0
                      AND Email IS NOT NULL
                    GROUP BY LOWER(LTRIM(RTRIM(Email)))
                    HAVING COUNT(*) > 1
                ) AS DuplicateEmails;

                SELECT @blankRowCount = COUNT(*)
                FROM dbo.Contacts
                WHERE IsDeleted = 0
                  AND Email IS NOT NULL
                  AND LTRIM(RTRIM(Email)) = '';

                IF @duplicateGroupCount > 0 OR @blankRowCount > 0
                BEGIN
                    DECLARE @message nvarchar(2048) = CONCAT(
                        'Cannot create UX_Contact_Email on Contacts.Email: ',
                        @duplicateGroupCount, ' duplicate normalized group(s), ',
                        @blankRowCount, ' non-null blank row(s). Merge or correct these rows and rerun. No rows were changed.');
                    THROW 51020, @message, 1;
                END
                """);

            migrationBuilder.DropIndex(
                name: "IX_Contacts_Email",
                table: "Contacts");

            migrationBuilder.CreateIndex(
                name: "UX_Contact_Email",
                table: "Contacts",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Contact_Email",
                table: "Contacts");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                table: "Contacts",
                column: "Email");
        }
    }
}
