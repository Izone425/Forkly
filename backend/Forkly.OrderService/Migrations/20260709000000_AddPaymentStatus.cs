using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forkly.OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                schema: "order",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unpaid");

            // Backfill: any order that had reached Paid or beyond was paid, so mark it
            // Paid. Order matters — set PaymentStatus from the old Status first, THEN
            // demote the old fulfilment "Paid" to "Pending" (payment now carries it).
            migrationBuilder.Sql(
                @"UPDATE ""order"".""Orders"" SET ""PaymentStatus"" = 'Paid'
                  WHERE ""Status"" IN ('Paid','Preparing','Completed','OutForDelivery','Delivered');");
            migrationBuilder.Sql(
                @"UPDATE ""order"".""Orders"" SET ""Status"" = 'Pending' WHERE ""Status"" = 'Paid';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "order",
                table: "Orders");
        }
    }
}
