using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class FinalSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_screen_seats_ShowSchedules_ShowScheduleId",
                table: "screen_seats");

            migrationBuilder.DropIndex(
                name: "IX_screen_seats_ShowScheduleId",
                table: "screen_seats");

            migrationBuilder.DropColumn(
                name: "ShowScheduleId",
                table: "screen_seats");

            migrationBuilder.CreateTable(
                name: "booking_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_type = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    attendee_name = table.Column<string>(type: "text", nullable: true),
                    attendee_mobile = table.Column<string>(type: "text", nullable: true),
                    attendee_email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "booking_seats",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    screen_seat_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_item_id = table.Column<long>(type: "bigint", nullable: true),
                    seat_price = table.Column<decimal>(type: "numeric", nullable: true),
                    booking_status = table.Column<string>(type: "text", nullable: true),
                    qr_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_seats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_ref = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    schedule_id = table.Column<int>(type: "integer", nullable: false),
                    booking_status = table.Column<string>(type: "text", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tickets = table.Column<int>(type: "integer", nullable: false),
                    booking_source = table.Column<string>(type: "text", nullable: true),
                    booked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    original_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    payable_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    convenience_fee = table.Column<decimal>(type: "numeric", nullable: true),
                    wallet_amount_used = table.Column<decimal>(type: "numeric", nullable: true),
                    transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_status = table.Column<string>(type: "text", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<string>(type: "text", nullable: false),
                    attendee_name = table.Column<string>(type: "text", nullable: true),
                    seat_number = table.Column<string>(type: "text", nullable: true),
                    qr_code = table.Column<string>(type: "text", nullable: true),
                    ticket_status = table.Column<string>(type: "text", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    qr_generated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    validation_status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_ref = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: true),
                    payment_method = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    gateway_name = table.Column<string>(type: "text", nullable: true),
                    gateway_transaction_id = table.Column<string>(type: "text", nullable: true),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    initiated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    gateway_status_code = table.Column<string>(type: "text", nullable: true),
                    refunded_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    refund_status = table.Column<string>(type: "text", nullable: true),
                    reconciliation_status = table.Column<string>(type: "text", nullable: true),
                    fraud_score = table.Column<decimal>(type: "numeric", nullable: true),
                    is_suspicious = table.Column<bool>(type: "boolean", nullable: true),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    payment_source = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_items");

            migrationBuilder.DropTable(
                name: "booking_seats");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.AddColumn<int>(
                name: "ShowScheduleId",
                table: "screen_seats",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_screen_seats_ShowScheduleId",
                table: "screen_seats",
                column: "ShowScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_screen_seats_ShowSchedules_ShowScheduleId",
                table: "screen_seats",
                column: "ShowScheduleId",
                principalTable: "ShowSchedules",
                principalColumn: "Id");
        }
    }
}
