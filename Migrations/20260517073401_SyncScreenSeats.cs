using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class SyncScreenSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "screen_seats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShowScheduleId",
                table: "screen_seats",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "screens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venue_id = table.Column<long>(type: "bigint", nullable: false),
                    screen_code = table.Column<string>(type: "text", nullable: false),
                    screen_name = table.Column<string>(type: "text", nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    screen_type = table.Column<string>(type: "text", nullable: false),
                    audio_system = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_screens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_screen_seats_ScheduleId",
                table: "screen_seats",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_screen_seats_screen_id",
                table: "screen_seats",
                column: "screen_id");

            migrationBuilder.CreateIndex(
                name: "IX_screen_seats_ShowScheduleId",
                table: "screen_seats",
                column: "ShowScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_screen_seats_ShowSchedules_ScheduleId",
                table: "screen_seats",
                column: "ScheduleId",
                principalTable: "ShowSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_screen_seats_ShowSchedules_ShowScheduleId",
                table: "screen_seats",
                column: "ShowScheduleId",
                principalTable: "ShowSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_screen_seats_screens_screen_id",
                table: "screen_seats",
                column: "screen_id",
                principalTable: "screens",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_screen_seats_ShowSchedules_ScheduleId",
                table: "screen_seats");

            migrationBuilder.DropForeignKey(
                name: "FK_screen_seats_ShowSchedules_ShowScheduleId",
                table: "screen_seats");

            migrationBuilder.DropForeignKey(
                name: "FK_screen_seats_screens_screen_id",
                table: "screen_seats");

            migrationBuilder.DropTable(
                name: "screens");

            migrationBuilder.DropIndex(
                name: "IX_screen_seats_ScheduleId",
                table: "screen_seats");

            migrationBuilder.DropIndex(
                name: "IX_screen_seats_screen_id",
                table: "screen_seats");

            migrationBuilder.DropIndex(
                name: "IX_screen_seats_ShowScheduleId",
                table: "screen_seats");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "screen_seats");

            migrationBuilder.DropColumn(
                name: "ShowScheduleId",
                table: "screen_seats");
        }
    }
}
