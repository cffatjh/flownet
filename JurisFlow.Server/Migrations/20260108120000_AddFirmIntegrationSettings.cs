using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JurisFlow.Server.Data;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    [DbContext(typeof(JurisFlowDbContext))]
    [Migration("20260108120000_AddFirmIntegrationSettings")]
    public partial class AddFirmIntegrationSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationsJson",
                table: "FirmSettings",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrationsJson",
                table: "FirmSettings");
        }
    }
}
