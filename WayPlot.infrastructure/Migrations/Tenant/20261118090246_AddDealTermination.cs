using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPlot.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddDealTermination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Deals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminatedAtUtc",
                table: "Deals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TerminatedByStaffId",
                table: "Deals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationMoneyOutcome",
                table: "Deals",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationReason",
                table: "Deals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TerminatedAtUtc",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TerminatedByStaffId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TerminationMoneyOutcome",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                table: "Deals");
        }
    }
}
