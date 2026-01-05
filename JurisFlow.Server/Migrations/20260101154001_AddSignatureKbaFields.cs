using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureKbaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresKba",
                table: "SignatureRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SignerIp",
                table: "SignatureRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignerLocation",
                table: "SignatureRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignerUserAgent",
                table: "SignatureRequests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresKba",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "SignerIp",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "SignerLocation",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "SignerUserAgent",
                table: "SignatureRequests");
        }
    }
}
