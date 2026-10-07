using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClockItSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStipendPaymentRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientStipendRates_Clients_ClientId",
                table: "ClientStipendRates");

            migrationBuilder.CreateTable(
                name: "StipendPaymentRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalStudents = table.Column<int>(type: "int", nullable: false),
                    TotalEligibleDays = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NetcashFileToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StipendPaymentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StipendPaymentRuns_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId");
                });

            migrationBuilder.CreateTable(
                name: "StipendPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRunId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    EligibleAttendanceDays = table.Column<int>(type: "int", nullable: false),
                    LeaveDays = table.Column<int>(type: "int", nullable: false),
                    SickLeaveDays = table.Column<int>(type: "int", nullable: false),
                    FamilyResponsibilityLeaveDays = table.Column<int>(type: "int", nullable: false),
                    TotalEligibleDays = table.Column<int>(type: "int", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StipendAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NetcashAccountReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StipendPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StipendPayments_StipendPaymentRuns_PaymentRunId",
                        column: x => x.PaymentRunId,
                        principalTable: "StipendPaymentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StipendPayments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StipendPaymentRuns_ClientId_PeriodFrom_PeriodTo",
                table: "StipendPaymentRuns",
                columns: new[] { "ClientId", "PeriodFrom", "PeriodTo" });

            migrationBuilder.CreateIndex(
                name: "IX_StipendPayments_PaymentRunId_StudentId",
                table: "StipendPayments",
                columns: new[] { "PaymentRunId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StipendPayments_StudentId",
                table: "StipendPayments",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientStipendRates_Clients_ClientId",
                table: "ClientStipendRates",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientStipendRates_Clients_ClientId",
                table: "ClientStipendRates");

            migrationBuilder.DropTable(
                name: "StipendPayments");

            migrationBuilder.DropTable(
                name: "StipendPaymentRuns");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientStipendRates_Clients_ClientId",
                table: "ClientStipendRates",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "ClientId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
