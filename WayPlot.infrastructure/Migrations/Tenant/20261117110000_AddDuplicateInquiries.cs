using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WayPlot.infrastructure.Data;

#nullable disable

namespace WayPlot.infrastructure.Migrations.Tenant;

[DbContext(typeof(TenantDbContext))]
[Migration("20261117110000_AddDuplicateInquiries")]
public partial class AddDuplicateInquiries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DuplicateInquiries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ParcelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                SyncStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Version = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DuplicateInquiries", x => x.Id);
                table.ForeignKey(
                    name: "FK_DuplicateInquiries_Contacts_ContactId",
                    column: x => x.ContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DuplicateInquiries_Parcels_ParcelId",
                    column: x => x.ParcelId,
                    principalTable: "Parcels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DuplicateInquiries_ContactId_OccurredAtUtc",
            table: "DuplicateInquiries",
            columns: new[] { "ContactId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DuplicateInquiries_ParcelId",
            table: "DuplicateInquiries",
            column: "ParcelId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "DuplicateInquiries");
}
