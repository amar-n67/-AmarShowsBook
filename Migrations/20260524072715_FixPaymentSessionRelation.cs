using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentSessionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentSessions_BookingId",
                table: "PaymentSessions",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSessions_bookings_BookingId",
                table: "PaymentSessions",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSessions_bookings_BookingId",
                table: "PaymentSessions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentSessions_BookingId",
                table: "PaymentSessions");
        }
    }
}
