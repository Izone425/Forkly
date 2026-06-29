using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forkly.OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                schema: "order",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Reference",
                schema: "order",
                table: "Orders",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Reference",
                schema: "order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Reference",
                schema: "order",
                table: "Orders");
        }
    }
}
