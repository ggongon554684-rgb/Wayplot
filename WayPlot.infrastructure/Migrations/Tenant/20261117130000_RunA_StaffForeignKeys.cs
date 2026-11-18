using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WayPlot.infrastructure.Data;

#nullable disable

namespace WayPlot.infrastructure.Migrations.Tenant;

public partial class RunA_StaffForeignKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @count int;
            DECLARE @message nvarchar(2048);

            SET @count = (SELECT COUNT(*) FROM Contacts c LEFT JOIN Staffs s ON s.Id = c.AssignedStaffId
                          WHERE c.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Contacts.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51101, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Contacts c LEFT JOIN Staffs s ON s.Id = c.ReferrerStaffId
                          WHERE c.ReferrerStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Contacts.ReferrerStaffId has ', @count, ' orphan IDs.'); THROW 51102, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Commissions c LEFT JOIN Staffs s ON s.Id = c.RecipientStaffId
                          WHERE c.RecipientStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Commissions.RecipientStaffId has ', @count, ' orphan IDs.'); THROW 51103, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Deals d LEFT JOIN Staffs s ON s.Id = d.AgentStaffId
                          WHERE d.AgentStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Deals.AgentStaffId has ', @count, ' orphan IDs.'); THROW 51104, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Deals d LEFT JOIN Staffs s ON s.Id = d.ConfirmedByStaffId
                          WHERE d.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Deals.ConfirmedByStaffId has ', @count, ' orphan IDs.'); THROW 51105, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Documents d LEFT JOIN Staffs s ON s.Id = d.UploadedByStaffId
                          WHERE d.UploadedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Documents.UploadedByStaffId has ', @count, ' orphan IDs.'); THROW 51106, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM DuplicateInquiries i LEFT JOIN Staffs s ON s.Id = i.RecordedByStaffId
                          WHERE i.RecordedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: DuplicateInquiries.RecordedByStaffId has ', @count, ' orphan IDs.'); THROW 51107, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Interactions i LEFT JOIN Staffs s ON s.Id = i.LoggedByStaffId
                          WHERE i.LoggedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Interactions.LoggedByStaffId has ', @count, ' orphan IDs.'); THROW 51108, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Leads l LEFT JOIN Staffs s ON s.Id = l.AssignedStaffId
                          WHERE l.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Leads.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51109, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Parcels p LEFT JOIN Staffs s ON s.Id = p.AssignedStaffId
                          WHERE p.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Parcels.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51110, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Payments p LEFT JOIN Staffs s ON s.Id = p.RecordedByStaffId
                          WHERE p.RecordedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Payments.RecordedByStaffId has ', @count, ' orphan IDs.'); THROW 51111, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM Payments p LEFT JOIN Staffs s ON s.Id = p.ConfirmedByStaffId
                          WHERE p.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Payments.ConfirmedByStaffId has ', @count, ' orphan IDs.'); THROW 51112, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM ReferralBonuses r LEFT JOIN Staffs s ON s.Id = r.ReferrerStaffId
                          WHERE r.ReferrerStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: ReferralBonuses.ReferrerStaffId has ', @count, ' orphan IDs.'); THROW 51113, @message, 1; END;

            SET @count = (SELECT COUNT(*) FROM StageHistories h LEFT JOIN Staffs s ON s.Id = h.ChangedByStaffId
                          WHERE h.ChangedByStaffId IS NOT NULL AND s.Id IS NULL);
            IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: StageHistories.ChangedByStaffId has ', @count, ' orphan IDs.'); THROW 51114, @message, 1; END;
            """);

        AddStaffForeignKey(migrationBuilder, "FK_Contacts_Staffs_AssignedStaffId", "Contacts", "AssignedStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Contacts_Staffs_ReferrerStaffId", "Contacts", "ReferrerStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Commissions_Staffs_RecipientStaffId", "Commissions", "RecipientStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Deals_Staffs_AgentStaffId", "Deals", "AgentStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Deals_Staffs_ConfirmedByStaffId", "Deals", "ConfirmedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Documents_Staffs_UploadedByStaffId", "Documents", "UploadedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_DuplicateInquiries_Staffs_RecordedByStaffId", "DuplicateInquiries", "RecordedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Interactions_Staffs_LoggedByStaffId", "Interactions", "LoggedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Leads_Staffs_AssignedStaffId", "Leads", "AssignedStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Parcels_Staffs_AssignedStaffId", "Parcels", "AssignedStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Payments_Staffs_RecordedByStaffId", "Payments", "RecordedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_Payments_Staffs_ConfirmedByStaffId", "Payments", "ConfirmedByStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_ReferralBonuses_Staffs_ReferrerStaffId", "ReferralBonuses", "ReferrerStaffId");
        AddStaffForeignKey(migrationBuilder, "FK_StageHistories_Staffs_ChangedByStaffId", "StageHistories", "ChangedByStaffId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropStaffForeignKey(migrationBuilder, "FK_Contacts_Staffs_AssignedStaffId", "Contacts");
        DropStaffForeignKey(migrationBuilder, "FK_Contacts_Staffs_ReferrerStaffId", "Contacts");
        DropStaffForeignKey(migrationBuilder, "FK_Commissions_Staffs_RecipientStaffId", "Commissions");
        DropStaffForeignKey(migrationBuilder, "FK_Deals_Staffs_AgentStaffId", "Deals");
        DropStaffForeignKey(migrationBuilder, "FK_Deals_Staffs_ConfirmedByStaffId", "Deals");
        DropStaffForeignKey(migrationBuilder, "FK_Documents_Staffs_UploadedByStaffId", "Documents");
        DropStaffForeignKey(migrationBuilder, "FK_DuplicateInquiries_Staffs_RecordedByStaffId", "DuplicateInquiries");
        DropStaffForeignKey(migrationBuilder, "FK_Interactions_Staffs_LoggedByStaffId", "Interactions");
        DropStaffForeignKey(migrationBuilder, "FK_Leads_Staffs_AssignedStaffId", "Leads");
        DropStaffForeignKey(migrationBuilder, "FK_Parcels_Staffs_AssignedStaffId", "Parcels");
        DropStaffForeignKey(migrationBuilder, "FK_Payments_Staffs_RecordedByStaffId", "Payments");
        DropStaffForeignKey(migrationBuilder, "FK_Payments_Staffs_ConfirmedByStaffId", "Payments");
        DropStaffForeignKey(migrationBuilder, "FK_ReferralBonuses_Staffs_ReferrerStaffId", "ReferralBonuses");
        DropStaffForeignKey(migrationBuilder, "FK_StageHistories_Staffs_ChangedByStaffId", "StageHistories");
    }

    private static void AddStaffForeignKey(MigrationBuilder m, string name, string table, string column) =>
        m.AddForeignKey(name, table, column, "Staffs", "Id", onDelete: ReferentialAction.Restrict);

    private static void DropStaffForeignKey(MigrationBuilder m, string name, string table) =>
        m.DropForeignKey(name, table);
}
