using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClockItSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNetcashUploadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NetcashReportedAt",
                table: "StipendPaymentRuns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetcashUploadReport",
                table: "StipendPaymentRuns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetcashUploadStatus",
                table: "StipendPaymentRuns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetcashReportedAt",
                table: "StipendPaymentRuns");

            migrationBuilder.DropColumn(
                name: "NetcashUploadReport",
                table: "StipendPaymentRuns");

            migrationBuilder.DropColumn(
                name: "NetcashUploadStatus",
                table: "StipendPaymentRuns");
        }
    }
}
