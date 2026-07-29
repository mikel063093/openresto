using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppHandoffBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_WhatsAppHandoffAudits_HandoffDestinationSnapshot_MaxLength",
                table: "WhatsAppHandoffAudits",
                sql: "length(\"HandoffDestinationSnapshot\") <= 32");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WhatsAppHandoffAudits_SummarySnapshot_MaxLength",
                table: "WhatsAppHandoffAudits",
                sql: "length(\"SummarySnapshot\") <= 1024");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WhatsAppHandoffAudits_HandoffDestinationSnapshot_MaxLength",
                table: "WhatsAppHandoffAudits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WhatsAppHandoffAudits_SummarySnapshot_MaxLength",
                table: "WhatsAppHandoffAudits");
        }
    }
}
