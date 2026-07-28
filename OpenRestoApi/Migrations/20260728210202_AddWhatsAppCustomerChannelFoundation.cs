using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppCustomerChannelFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneE164",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneNormalized",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelMutationIdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Channel = table.Column<string>(type: "TEXT", nullable: false),
                    MutationScope = table.Column<string>(type: "TEXT", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMutationIdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerPhoneNormalized_RestaurantId_Date",
                table: "Bookings",
                columns: new[] { "CustomerPhoneNormalized", "RestaurantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMutationIdempotencyRecords_Channel_MutationScope_IdempotencyKey",
                table: "ChannelMutationIdempotencyRecords",
                columns: new[] { "Channel", "MutationScope", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerPhoneNormalized_RestaurantId_Date",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneE164",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneNormalized",
                table: "Bookings");
        }
    }
}
