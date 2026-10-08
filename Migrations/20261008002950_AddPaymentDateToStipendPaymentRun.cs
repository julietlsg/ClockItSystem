using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClockItSystem.Migrations
{
    public partial class AddPaymentDateToStipendPaymentRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "StipendPaymentRuns",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTime.MinValue);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "StipendPaymentRuns");
        }
    }
}