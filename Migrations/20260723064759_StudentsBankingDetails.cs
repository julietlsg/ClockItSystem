using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClockItSystem.Migrations
{
    /// <inheritdoc />
    public partial class StudentsBankingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceApprovals_AttendanceRecords_AttendanceRecordId",
                table: "AttendanceApprovals");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceApprovals_AttendanceRecordId",
                table: "AttendanceApprovals");

            migrationBuilder.AddColumn<string>(
                name: "AccountHolderName",
                table: "Students",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Students",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountTypeId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankBranchId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    AccountTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.AccountTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    BankId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.BankId);
                });

            migrationBuilder.CreateTable(
                name: "ReportResultViewModel",
                columns: table => new
                {
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProgrammeOrCourse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    VerificationMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysApproved = table.Column<int>(type: "int", nullable: true),
                    DaysAbsent = table.Column<int>(type: "int", nullable: true),
                    LeaveDays = table.Column<int>(type: "int", nullable: true),
                    AttendancePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalStudents = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "BankBranches",
                columns: table => new
                {
                    BankBranchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankBranches", x => x.BankBranchId);
                    table.ForeignKey(
                        name: "FK_BankBranches_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "BankId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_AccountTypeId",
                table: "Students",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_BankBranchId",
                table: "Students",
                column: "BankBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_BankId",
                table: "Students",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceApprovals_AttendanceRecordId",
                table: "AttendanceApprovals",
                column: "AttendanceRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankBranches_BankId",
                table: "BankBranches",
                column: "BankId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceApprovals_AttendanceRecords_AttendanceRecordId",
                table: "AttendanceApprovals",
                column: "AttendanceRecordId",
                principalTable: "AttendanceRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AccountTypes_AccountTypeId",
                table: "Students",
                column: "AccountTypeId",
                principalTable: "AccountTypes",
                principalColumn: "AccountTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_BankBranches_BankBranchId",
                table: "Students",
                column: "BankBranchId",
                principalTable: "BankBranches",
                principalColumn: "BankBranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Banks_BankId",
                table: "Students",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "BankId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceApprovals_AttendanceRecords_AttendanceRecordId",
                table: "AttendanceApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AccountTypes_AccountTypeId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_BankBranches_BankBranchId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Banks_BankId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropTable(
                name: "BankBranches");

            migrationBuilder.DropTable(
                name: "ReportResultViewModel");

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_Students_AccountTypeId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_BankBranchId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_BankId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceApprovals_AttendanceRecordId",
                table: "AttendanceApprovals");

            migrationBuilder.DropColumn(
                name: "AccountHolderName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AccountTypeId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "BankBranchId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceApprovals_AttendanceRecordId",
                table: "AttendanceApprovals",
                column: "AttendanceRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceApprovals_AttendanceRecords_AttendanceRecordId",
                table: "AttendanceApprovals",
                column: "AttendanceRecordId",
                principalTable: "AttendanceRecords",
                principalColumn: "Id");
        }
    }
}
