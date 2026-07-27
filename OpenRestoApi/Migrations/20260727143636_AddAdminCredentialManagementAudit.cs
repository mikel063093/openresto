using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCredentialManagementAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminCredentialManagementAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActorAdminCredentialId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActorEmailSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    TargetOperatorPrincipalId = table.Column<int>(type: "INTEGER", nullable: true),
                    OperatorAgentCredentialId = table.Column<int>(type: "INTEGER", nullable: true),
                    CredentialKeyIdSnapshot = table.Column<string>(type: "TEXT", nullable: true),
                    TargetOperatorIdentifierSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ScopeRestaurantIdsSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    TtlHoursSnapshot = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCredentialManagementAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_AdminCredentials_ActorAdminCredentialId",
                        column: x => x.ActorAdminCredentialId,
                        principalTable: "AdminCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_OperatorAgentCredentials_OperatorAgentCredentialId",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminCredentialManagementAudits_OperatorPrincipals_TargetOperatorPrincipalId",
                        column: x => x.TargetOperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_ActorAdminCredentialId",
                table: "AdminCredentialManagementAudits",
                column: "ActorAdminCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_CreatedAtUtc",
                table: "AdminCredentialManagementAudits",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_OperatorAgentCredentialId_CreatedAtUtc",
                table: "AdminCredentialManagementAudits",
                columns: new[] { "OperatorAgentCredentialId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminCredentialManagementAudits_TargetOperatorPrincipalId_CreatedAtUtc",
                table: "AdminCredentialManagementAudits",
                columns: new[] { "TargetOperatorPrincipalId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminCredentialManagementAudits");
        }
    }
}
