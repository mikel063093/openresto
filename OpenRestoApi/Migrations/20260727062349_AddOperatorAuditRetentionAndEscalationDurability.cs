using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenRestoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorAuditRetentionAndEscalationDurability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                table: "OperatorActionAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                table: "OperatorActionAudits");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "OperatorPrincipalId",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "OperatorPrincipalIdSnapshot",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantIdSnapshot",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RestaurantNameSnapshot",
                table: "OperatorActionAudits",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                table: "OperatorActionAudits",
                column: "OperatorPrincipalId",
                principalTable: "OperatorPrincipals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                table: "OperatorActionAudits",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                table: "OperatorActionAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                table: "OperatorActionAudits");

            migrationBuilder.DropColumn(
                name: "OperatorPrincipalIdSnapshot",
                table: "OperatorActionAudits");

            migrationBuilder.DropColumn(
                name: "RestaurantIdSnapshot",
                table: "OperatorActionAudits");

            migrationBuilder.DropColumn(
                name: "RestaurantNameSnapshot",
                table: "OperatorActionAudits");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OperatorPrincipalId",
                table: "OperatorActionAudits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorActionAudits_OperatorPrincipals_OperatorPrincipalId",
                table: "OperatorActionAudits",
                column: "OperatorPrincipalId",
                principalTable: "OperatorPrincipals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorActionAudits_Restaurants_RestaurantId",
                table: "OperatorActionAudits",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
