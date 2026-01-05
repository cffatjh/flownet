using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddOpposingPartiesAndConflictCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConflictCheckCleared",
                table: "Matters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConflictCheckDate",
                table: "Matters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConflictWaiverObtained",
                table: "Matters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedRepresentatives",
                table: "Clients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncorporationState",
                table: "Clients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredAgent",
                table: "Clients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpposingParties",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: true),
                    TaxId = table.Column<string>(type: "TEXT", nullable: true),
                    IncorporationState = table.Column<string>(type: "TEXT", nullable: true),
                    CounselName = table.Column<string>(type: "TEXT", nullable: true),
                    CounselFirm = table.Column<string>(type: "TEXT", nullable: true),
                    CounselEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CounselPhone = table.Column<string>(type: "TEXT", nullable: true),
                    CounselAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpposingParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpposingParties_Matters_MatterId",
                        column: x => x.MatterId,
                        principalTable: "Matters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpposingParties_MatterId",
                table: "OpposingParties",
                column: "MatterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpposingParties");

            migrationBuilder.DropColumn(
                name: "ConflictCheckCleared",
                table: "Matters");

            migrationBuilder.DropColumn(
                name: "ConflictCheckDate",
                table: "Matters");

            migrationBuilder.DropColumn(
                name: "ConflictWaiverObtained",
                table: "Matters");

            migrationBuilder.DropColumn(
                name: "AuthorizedRepresentatives",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IncorporationState",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RegisteredAgent",
                table: "Clients");
        }
    }
}
