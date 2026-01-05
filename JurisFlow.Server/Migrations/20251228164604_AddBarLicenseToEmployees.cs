using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JurisFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBarLicenseToEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BarAdmissionDate",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarJurisdiction",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarNumber",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BarStatus",
                table: "Employees",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarAdmissionDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BarJurisdiction",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BarNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BarStatus",
                table: "Employees");
        }
    }
}
