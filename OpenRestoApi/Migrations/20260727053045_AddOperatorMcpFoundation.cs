using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorMcpFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByOperatorId",
                table: "Bookings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedViaChannel",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OperatorPrincipals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorPrincipals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperatorAgentCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorPrincipalId = table.Column<int>(type: "INTEGER", nullable: false),
                    CredentialKeyId = table.Column<string>(type: "TEXT", nullable: false),
                    TokenDigest = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorAgentCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorAgentCredentials_OperatorPrincipals_OperatorPrincipalId",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorRestaurantScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorPrincipalId = table.Column<int>(type: "INTEGER", nullable: false),
                    RestaurantId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorRestaurantScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorRestaurantScopes_OperatorPrincipals_OperatorPrincipalId",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorRestaurantScopes_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorActionAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorPrincipalId = table.Column<int>(type: "INTEGER", nullable: false),
                    OperatorAgentCredentialId = table.Column<int>(type: "INTEGER", nullable: true),
                    RestaurantId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookingId = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorActionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_OperatorAgentCredentials_OperatorAgentCredentialId",
                        column: x => x.OperatorAgentCredentialId,
                        principalTable: "OperatorAgentCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                        column: x => x.OperatorPrincipalId,
                        principalTable: "OperatorPrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedByOperatorId_RestaurantId_Date",
                table: "Bookings",
                columns: new[] { "CreatedByOperatorId", "RestaurantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_BookingId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "BookingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_OperatorAgentCredentialId",
                table: "OperatorActionAudits",
                column: "OperatorAgentCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_OperatorPrincipalId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "OperatorPrincipalId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorActionAudits_RestaurantId_CreatedAt",
                table: "OperatorActionAudits",
                columns: new[] { "RestaurantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_CredentialKeyId",
                table: "OperatorAgentCredentials",
                column: "CredentialKeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_ExpiresAt_RevokedAt",
                table: "OperatorAgentCredentials",
                columns: new[] { "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorAgentCredentials_OperatorPrincipalId",
                table: "OperatorAgentCredentials",
                column: "OperatorPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorPrincipals_NormalizedIdentifier",
                table: "OperatorPrincipals",
                column: "NormalizedIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorRestaurantScopes_OperatorPrincipalId_RestaurantId",
                table: "OperatorRestaurantScopes",
                columns: new[] { "OperatorPrincipalId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorRestaurantScopes_RestaurantId",
                table: "OperatorRestaurantScopes",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_OperatorPrincipals_CreatedByOperatorId",
                table: "Bookings",
                column: "CreatedByOperatorId",
                principalTable: "OperatorPrincipals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_OperatorPrincipals_CreatedByOperatorId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "OperatorActionAudits");

            migrationBuilder.DropTable(
                name: "OperatorRestaurantScopes");

            migrationBuilder.DropTable(
                name: "OperatorAgentCredentials");

            migrationBuilder.DropTable(
                name: "OperatorPrincipals");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedByOperatorId_RestaurantId_Date",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedByOperatorId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedViaChannel",
                table: "Bookings");
        }
    }
}
