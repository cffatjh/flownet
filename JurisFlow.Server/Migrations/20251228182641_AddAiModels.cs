using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CasePredictions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PredictedOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    FactorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SimilarCasesJson = table.Column<string>(type: "TEXT", nullable: true),
                    SettlementMin = table.Column<decimal>(type: "TEXT", nullable: true),
                    SettlementMax = table.Column<decimal>(type: "TEXT", nullable: true),
                    EstimatedTimeline = table.Column<string>(type: "TEXT", nullable: true),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePredictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    ContractType = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    KeyTermsJson = table.Column<string>(type: "TEXT", nullable: true),
                    KeyDatesJson = table.Column<string>(type: "TEXT", nullable: true),
                    PartiesJson = table.Column<string>(type: "TEXT", nullable: true),
                    RisksJson = table.Column<string>(type: "TEXT", nullable: true),
                    RiskScore = table.Column<int>(type: "INTEGER", nullable: false),
                    UnusualClausesJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    MatterId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", nullable: false),
                    Jurisdiction = table.Column<string>(type: "TEXT", nullable: true),
                    PracticeArea = table.Column<string>(type: "TEXT", nullable: true),
                    Response = table.Column<string>(type: "TEXT", nullable: true),
                    CitationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    KeyPointsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedCasesJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CasePredictions");

            migrationBuilder.DropTable(
                name: "ContractAnalyses");

            migrationBuilder.DropTable(
                name: "ResearchSessions");
        }
    }
}
