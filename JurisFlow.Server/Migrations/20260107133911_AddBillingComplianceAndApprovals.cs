using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingComplianceAndApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrustTransactions_Matters_MatterId",
                table: "TrustTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "MatterId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "AllocationsJson",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckNumber",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "TrustTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LedgerId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayorPayee",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedBy",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrustAccountId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultHourlyRate = table.Column<double>(type: "REAL", nullable: false),
                    PartnerRate = table.Column<double>(type: "REAL", nullable: false),
                    AssociateRate = table.Column<double>(type: "REAL", nullable: false),
                    ParalegalRate = table.Column<double>(type: "REAL", nullable: false),
                    BillingIncrement = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumTimeEntry = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundingRule = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultPaymentTerms = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoicePrefix = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultTaxRate = table.Column<double>(type: "REAL", nullable: false),
                    LedesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UtbmsCodesRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    EvergreenRetainerMinimum = table.Column<double>(type: "REAL", nullable: false),
                    TrustBalanceAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Billed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ExpenseCode = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_Matters_MatterId",
                        column: x => x.MatterId,
                        principalTable: "Matters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FirmSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FirmName = table.Column<string>(type: "TEXT", nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", nullable: true),
                    LedesFirmId = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Rate = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Billed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBillable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityCode = table.Column<string>(type: "TEXT", nullable: true),
                    TaskCode = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Matters_MatterId",
                        column: x => x.MatterId,
                        principalTable: "Matters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingSettings_Id",
                table: "BillingSettings",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_MatterId_Date",
                table: "Expenses",
                columns: new[] { "MatterId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FirmSettings_Id",
                table: "FirmSettings",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_MatterId_Date",
                table: "TimeEntries",
                columns: new[] { "MatterId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_TrustTransactions_Matters_MatterId",
                table: "TrustTransactions",
                column: "MatterId",
                principalTable: "Matters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrustTransactions_Matters_MatterId",
                table: "TrustTransactions");

            migrationBuilder.DropTable(
                name: "BillingSettings");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FirmSettings");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "AllocationsJson",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "CheckNumber",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "LedgerId",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "PayorPayee",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "TrustAccountId",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "TrustTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "MatterId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrustTransactions_Matters_MatterId",
                table: "TrustTransactions",
                column: "MatterId",
                principalTable: "Matters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
