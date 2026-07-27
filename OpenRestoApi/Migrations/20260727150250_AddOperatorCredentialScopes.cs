using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorCredentialScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperatorAgentCredentialScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorAgentCredentialId = table.Column<int>(type: "INTEGER", nullable: false),
                    RestaurantId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorAgentCredentialScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentialScopes_OperatorAgentCredentials_OperatorAgentCredentialId",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentialScopes_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentialScopes_OperatorAgentCredentialId_RestaurantId",
                table: "OperatorAgentCredentialScopes",
                columns: new[] { "OperatorAgentCredentialId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentialScopes_RestaurantId",
                table: "OperatorAgentCredentialScopes",
                column: "RestaurantId");

            migrationBuilder.Sql(
                """
                INSERT INTO "OperatorAgentCredentialScopes" ("OperatorAgentCredentialId", "RestaurantId", "CreatedAt")
                SELECT credential."Id", principalScope."RestaurantId", credential."IssuedAt"
                FROM "OperatorAgentCredentials" AS credential
                INNER JOIN "OperatorRestaurantScopes" AS principalScope
                    ON principalScope."OperatorPrincipalId" = credential."OperatorPrincipalId"
                LEFT JOIN "OperatorAgentCredentialScopes" AS existing
                    ON existing."OperatorAgentCredentialId" = credential."Id"
                    AND existing."RestaurantId" = principalScope."RestaurantId"
                WHERE existing."Id" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperatorAgentCredentialScopes");
        }
    }
}
