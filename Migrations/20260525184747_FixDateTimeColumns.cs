using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class FixDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSessions_bookings_BookingId",
                table: "PaymentSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ShowSchedules_Locations_LocationId",
                table: "ShowSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PaymentSessions_BookingId",
                table: "PaymentSessions");

            migrationBuilder.AddColumn<long>(
                name: "screen_id",
                table: "ShowSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "coupon_id",
                table: "bookings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_status",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "application_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    menu_code = table.Column<string>(type: "text", nullable: false),
                    menu_name = table.Column<string>(type: "text", nullable: false),
                    parent_menu_id = table.Column<long>(type: "bigint", nullable: true),
                    module_id = table.Column<long>(type: "bigint", nullable: true),
                    route_path = table.Column<string>(type: "text", nullable: false),
                    icon_name = table.Column<string>(type: "text", nullable: false),
                    menu_level = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "application_modules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    module_code = table.Column<string>(type: "text", nullable: false),
                    module_name = table.Column<string>(type: "text", nullable: false),
                    module_description = table.Column<string>(type: "text", nullable: true),
                    route_path = table.Column<string>(type: "text", nullable: true),
                    icon_name = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    granted_by = table.Column<long>(type: "bigint", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venue_code = table.Column<string>(type: "text", nullable: false),
                    venue_name = table.Column<string>(type: "text", nullable: false),
                    venue_type = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_mobile = table.Column<string>(type: "text", nullable: true),
                    total_screens = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venues", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowSchedules_screen_id",
                table: "ShowSchedules",
                column: "screen_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShowSchedules_Locations_LocationId",
                table: "ShowSchedules",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShowSchedules_screens_screen_id",
                table: "ShowSchedules",
                column: "screen_id",
                principalTable: "screens",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShowSchedules_Locations_LocationId",
                table: "ShowSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ShowSchedules_screens_screen_id",
                table: "ShowSchedules");

            migrationBuilder.DropTable(
                name: "application_menus");

            migrationBuilder.DropTable(
                name: "application_modules");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "venues");

            migrationBuilder.DropIndex(
                name: "IX_ShowSchedules_screen_id",
                table: "ShowSchedules");

            migrationBuilder.DropColumn(
                name: "screen_id",
                table: "ShowSchedules");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "coupon_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_status",
                table: "bookings");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ShowSchedules_Locations_LocationId",
                table: "ShowSchedules",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
