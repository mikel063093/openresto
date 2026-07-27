using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorCredentialExpirationPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "OperatorAgentCredentials",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ExpirationPreset",
                table: "OperatorAgentCredentials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpirationPresetSnapshot",
                table: "AdminCredentialManagementAudits",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationPreset",
                table: "OperatorAgentCredentials");

            migrationBuilder.DropColumn(
                name: "ExpirationPresetSnapshot",
                table: "AdminCredentialManagementAudits");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "OperatorAgentCredentials",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
