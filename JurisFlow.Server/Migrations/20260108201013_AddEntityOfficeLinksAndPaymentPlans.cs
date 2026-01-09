using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityOfficeLinksAndPaymentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "TrustTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "TrustBankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "TrustBankAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentPlanId",
                table: "PaymentTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "PaymentTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PaymentTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Matters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "Matters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "ClientTrustLedgers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficeId",
                table: "ClientTrustLedgers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentContentIndexes",
                columns: table => new
                {
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedContent = table.Column<string>(type: "TEXT", nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", nullable: true),
                    ContentLength = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentContentIndexes", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DocumentContentIndexes_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<double>(type: "REAL", nullable: false),
                    InstallmentAmount = table.Column<double>(type: "REAL", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextRunDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RemainingAmount = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AutoPayEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoPayMethod = table.Column<string>(type: "TEXT", nullable: true),
                    AutoPayReference = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustTransactions_EntityId_OfficeId",
                table: "TrustTransactions",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustBankAccounts_EntityId_OfficeId",
                table: "TrustBankAccounts",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentPlanId",
                table: "PaymentTransactions",
                column: "PaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Matters_EntityId_OfficeId",
                table: "Matters",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Matters_OfficeId",
                table: "Matters",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_EntityId_OfficeId",
                table: "Invoices",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EntityId_OfficeId",
                table: "Employees",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OfficeId",
                table: "Employees",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrustLedgers_EntityId_OfficeId",
                table: "ClientTrustLedgers",
                columns: new[] { "EntityId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentContentIndexes_ContentHash",
                table: "DocumentContentIndexes",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_ClientId_Status",
                table: "PaymentPlans",
                columns: new[] { "ClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_NextRunDate",
                table: "PaymentPlans",
                column: "NextRunDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_FirmEntities_EntityId",
                table: "Employees",
                column: "EntityId",
                principalTable: "FirmEntities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Offices_OfficeId",
                table: "Employees",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matters_FirmEntities_EntityId",
                table: "Matters",
                column: "EntityId",
                principalTable: "FirmEntities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matters_Offices_OfficeId",
                table: "Matters",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_FirmEntities_EntityId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Offices_OfficeId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Matters_FirmEntities_EntityId",
                table: "Matters");

            migrationBuilder.DropForeignKey(
                name: "FK_Matters_Offices_OfficeId",
                table: "Matters");

            migrationBuilder.DropTable(
                name: "DocumentContentIndexes");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrustTransactions_EntityId_OfficeId",
                table: "TrustTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TrustBankAccounts_EntityId_OfficeId",
                table: "TrustBankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PaymentPlanId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Matters_EntityId_OfficeId",
                table: "Matters");

            migrationBuilder.DropIndex(
                name: "IX_Matters_OfficeId",
                table: "Matters");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_EntityId_OfficeId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EntityId_OfficeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OfficeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_ClientTrustLedgers_EntityId_OfficeId",
                table: "ClientTrustLedgers");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "TrustTransactions");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "TrustBankAccounts");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "TrustBankAccounts");

            migrationBuilder.DropColumn(
                name: "PaymentPlanId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Matters");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "Matters");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "ClientTrustLedgers");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "ClientTrustLedgers");
        }
    }
}
