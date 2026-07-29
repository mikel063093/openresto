using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppAuthorityBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandoffWhatsAppE164",
                table: "Restaurants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWhatsAppTestEnabled",
                table: "Restaurants",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WhatsAppHandoffAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RestaurantId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookingId = table.Column<int>(type: "INTEGER", nullable: true),
                    VerifiedPhoneE164 = table.Column<string>(type: "TEXT", nullable: false),
                    VerifiedPhoneNormalized = table.Column<string>(type: "TEXT", nullable: false),
                    SummarySnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    HandoffDestinationSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppHandoffAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppHandoffAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhatsAppHandoffAudits_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_BookingId",
                table: "WhatsAppHandoffAudits",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_RestaurantId_CreatedAtUtc",
                table: "WhatsAppHandoffAudits",
                columns: new[] { "RestaurantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppHandoffAudits_VerifiedPhoneNormalized_CreatedAtUtc",
                table: "WhatsAppHandoffAudits",
                columns: new[] { "VerifiedPhoneNormalized", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppHandoffAudits");

            migrationBuilder.DropColumn(
                name: "HandoffWhatsAppE164",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "IsWhatsAppTestEnabled",
                table: "Restaurants");
        }
    }
}
