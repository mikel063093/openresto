using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class HardenBookingOccasionSnapshotUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId",
                table: "BookingOccasionSnapshots",
                columns: new[] { "BookingId", "RestaurantOccasionCatalogItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingOccasionSnapshots_BookingId_RestaurantOccasionCatalogItemId",
                table: "BookingOccasionSnapshots");
        }
    }
}
