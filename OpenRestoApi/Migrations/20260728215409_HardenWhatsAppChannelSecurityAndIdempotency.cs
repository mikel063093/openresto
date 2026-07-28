using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class HardenWhatsAppChannelSecurityAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "ChannelMutationIdempotencyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "ChannelMutationIdempotencyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplayKey",
                table: "ChannelMutationIdempotencyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultJson",
                table: "ChannelMutationIdempotencyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "ChannelMutationIdempotencyRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyToken",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMutationIdempotencyRecords_Channel_ReplayKey",
                table: "ChannelMutationIdempotencyRecords",
                columns: new[] { "Channel", "ReplayKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelMutationIdempotencyRecords_Channel_ReplayKey",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ReplayKey",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "State",
                table: "ChannelMutationIdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "Bookings");
        }
    }
}
