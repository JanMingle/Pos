using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceFollowUpTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowUpCount",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpNotes",
                table: "Invoices",
                type: "nvarchar(800)",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpStatus",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Not Started");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFollowUpAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextFollowUpDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpCount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FollowUpNotes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FollowUpStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastFollowUpAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NextFollowUpDate",
                table: "Invoices");
        }
    }
}
