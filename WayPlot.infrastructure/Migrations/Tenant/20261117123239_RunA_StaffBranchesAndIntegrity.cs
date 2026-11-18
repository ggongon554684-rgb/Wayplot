using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPlot.infrastructure.Migrations.Tenant;

public partial class RunA_StaffBranchesAndIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Deals_Leads_LeadId",
            table: "Deals");

        migrationBuilder.DropIndex(
            name: "UX_Lead_OneNegotiationPerParcel",
            table: "Leads");

        migrationBuilder.DropIndex(
            name: "IX_Deals_LeadId",
            table: "Deals");

        migrationBuilder.DropIndex(
            name: "UX_Deal_OneActivePerParcel",
            table: "Deals");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_Leads_Id_ContactId_ParcelId",
            table: "Leads",
            columns: new[] { "Id", "ContactId", "ParcelId" });

        migrationBuilder.CreateTable(
            name: "Branches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                SyncStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Branches", x => x.Id));

        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM Staffs s LEFT JOIN Branches b ON b.Id = s.BranchId
                       WHERE s.BranchId IS NOT NULL AND b.Id IS NULL)
            BEGIN
                DECLARE @count int = (SELECT COUNT(*) FROM Staffs s LEFT JOIN Branches b ON b.Id = s.BranchId
                                      WHERE s.BranchId IS NOT NULL AND b.Id IS NULL);
                DECLARE @message nvarchar(2048) = CONCAT('Run A Part A blocked: Staffs.BranchId has ', @count, ' orphan IDs.');
                THROW 51001, @message, 1;
            END
            """);

        migrationBuilder.CreateIndex("IX_Staffs_BranchId", "Staffs", "BranchId");
        migrationBuilder.CreateIndex("IX_Parcels_AssignedStaffId", "Parcels", "AssignedStaffId");
        migrationBuilder.CreateIndex("IX_Leads_AssignedStaffId", "Leads", "AssignedStaffId");
        migrationBuilder.CreateIndex("IX_Leads_ParcelId", "Leads", "ParcelId");
        migrationBuilder.CreateIndex("UX_Lead_Id_ContactId_ParcelId", "Leads",
            new[] { "Id", "ContactId", "ParcelId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Interactions_LoggedByStaffId", "Interactions", "LoggedByStaffId");
        migrationBuilder.CreateIndex("IX_Documents_UploadedByStaffId", "Documents", "UploadedByStaffId");
        migrationBuilder.CreateIndex("IX_Deals_AgentStaffId", "Deals", "AgentStaffId");
        migrationBuilder.CreateIndex("IX_Deals_ConfirmedByStaffId", "Deals", "ConfirmedByStaffId");
        migrationBuilder.CreateIndex("IX_Deals_LeadId_ContactId_ParcelId", "Deals",
            new[] { "LeadId", "ContactId", "ParcelId" });
        migrationBuilder.CreateIndex("UX_Deal_OneActivePerParcel", "Deals", "ParcelId",
            unique: true, filter: "[IsActive] = 1 AND [IsDeleted] = 0");
        migrationBuilder.CreateIndex("IX_Contacts_AssignedStaffId", "Contacts", "AssignedStaffId");
        migrationBuilder.CreateIndex("IX_Contacts_ReferrerStaffId", "Contacts", "ReferrerStaffId");
        migrationBuilder.CreateIndex("IX_Commissions_RecipientStaffId", "Commissions", "RecipientStaffId");
        migrationBuilder.CreateIndex("IX_ReferralBonuses_ReferrerStaffId", "ReferralBonuses", "ReferrerStaffId");
        migrationBuilder.CreateIndex("IX_Payments_RecordedByStaffId", "Payments", "RecordedByStaffId");
        migrationBuilder.CreateIndex("IX_Payments_ConfirmedByStaffId", "Payments", "ConfirmedByStaffId");
        migrationBuilder.CreateIndex("IX_StageHistories_ChangedByStaffId", "StageHistories", "ChangedByStaffId");
        migrationBuilder.CreateIndex("IX_DuplicateInquiries_RecordedByStaffId", "DuplicateInquiries", "RecordedByStaffId");
        migrationBuilder.CreateIndex("IX_Branches_Name", "Branches", "Name",
            unique: true, filter: "[IsDeleted] = 0");

        migrationBuilder.AddForeignKey(
            name: "FK_Deals_Leads_LeadId_ContactId_ParcelId",
            table: "Deals",
            columns: new[] { "LeadId", "ContactId", "ParcelId" },
            principalTable: "Leads",
            principalColumns: new[] { "Id", "ContactId", "ParcelId" },
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Staffs_Branches_BranchId",
            table: "Staffs",
            column: "BranchId",
            principalTable: "Branches",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Deals_Leads_LeadId_ContactId_ParcelId", "Deals");
        migrationBuilder.DropForeignKey("FK_Staffs_Branches_BranchId", "Staffs");
        migrationBuilder.DropTable("Branches");

        foreach (var index in new[]
        {
            "IX_Staffs_BranchId", "IX_Parcels_AssignedStaffId", "IX_Leads_AssignedStaffId", "IX_Leads_ParcelId",
            "UX_Lead_Id_ContactId_ParcelId", "IX_Interactions_LoggedByStaffId", "IX_Documents_UploadedByStaffId",
            "IX_Deals_AgentStaffId", "IX_Deals_ConfirmedByStaffId", "IX_Deals_LeadId_ContactId_ParcelId",
            "UX_Deal_OneActivePerParcel", "IX_Contacts_AssignedStaffId", "IX_Contacts_ReferrerStaffId",
            "IX_Commissions_RecipientStaffId", "IX_ReferralBonuses_ReferrerStaffId", "IX_Payments_RecordedByStaffId",
            "IX_Payments_ConfirmedByStaffId", "IX_StageHistories_ChangedByStaffId",
            "IX_DuplicateInquiries_RecordedByStaffId", "IX_Branches_Name"
        })
            migrationBuilder.DropIndex(index, index == "IX_Branches_Name" ? "Branches" :
                index.StartsWith("IX_Staffs") ? "Staffs" : index.StartsWith("IX_Parcels") ? "Parcels" :
                index.StartsWith("IX_Leads") || index.StartsWith("UX_Lead") ? "Leads" :
                index.StartsWith("IX_Interactions") ? "Interactions" : index.StartsWith("IX_Documents") ? "Documents" :
                index.StartsWith("IX_Deals") || index.StartsWith("UX_Deal") ? "Deals" :
                index.StartsWith("IX_Contacts") ? "Contacts" : index.StartsWith("IX_Commissions") ? "Commissions" :
                index.StartsWith("IX_Referral") ? "ReferralBonuses" : index.StartsWith("IX_Payments") ? "Payments" :
                index.StartsWith("IX_Stage") ? "StageHistories" : "DuplicateInquiries");

        migrationBuilder.DropUniqueConstraint("AK_Leads_Id_ContactId_ParcelId", "Leads");
        migrationBuilder.CreateIndex("UX_Lead_OneNegotiationPerParcel", "Leads", "ParcelId",
            unique: true, filter: "[Stage] = 'Negotiation' AND [IsDeleted] = 0");
        migrationBuilder.CreateIndex("IX_Deals_LeadId", "Deals", "LeadId");
        migrationBuilder.CreateIndex("UX_Deal_OneActivePerParcel", "Deals", "ParcelId",
            unique: true, filter: "[IsActive] = 1");
        migrationBuilder.AddForeignKey("FK_Deals_Leads_LeadId", "Deals", "LeadId", "Leads", "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
