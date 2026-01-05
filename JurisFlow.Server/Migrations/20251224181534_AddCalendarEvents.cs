using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    RecurrencePattern = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Matters_MatterId",
                        column: x => x.MatterId,
                        principalTable: "Matters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrustBankAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BankName = table.Column<string>(type: "TEXT", nullable: false),
                    AccountNumberEnc = table.Column<string>(type: "TEXT", nullable: false),
                    RoutingNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentBalance = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientTrustLedgers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    TrustAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    RunningBalance = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTrustLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientTrustLedgers_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientTrustLedgers_Matters_MatterId",
                        column: x => x.MatterId,
                        principalTable: "Matters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientTrustLedgers_TrustBankAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TrustAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BankStatementBalance = table.Column<double>(type: "REAL", nullable: false),
                    TrustLedgerBalance = table.Column<double>(type: "REAL", nullable: false),
                    ClientLedgerSumBalance = table.Column<double>(type: "REAL", nullable: false),
                    IsReconciled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiscrepancyAmount = table.Column<double>(type: "REAL", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationRecords_TrustBankAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_MatterId",
                table: "CalendarEvents",
                column: "MatterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrustLedgers_ClientId",
                table: "ClientTrustLedgers",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrustLedgers_MatterId",
                table: "ClientTrustLedgers",
                column: "MatterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrustLedgers_TrustAccountId",
                table: "ClientTrustLedgers",
                column: "TrustAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_TrustAccountId",
                table: "ReconciliationRecords",
                column: "TrustAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "ClientTrustLedgers");

            migrationBuilder.DropTable(
                name: "ReconciliationRecords");

            migrationBuilder.DropTable(
                name: "TrustBankAccounts");
        }
    }
}
