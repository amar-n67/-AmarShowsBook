using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
{
}

protected override void Down(MigrationBuilder migrationBuilder)
{
}
    }
}
        /// <inheritdoc />
//         protected override void Up(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.CreateTable(
//                 name: "activity_logs",
//                 columns: table => new
//                 {
//                     Id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     UserId = table.Column<long>(type: "bigint", nullable: true),
//                     Action = table.Column<string>(type: "text", nullable: false),
//                     Module = table.Column<string>(type: "text", nullable: false),
//                     EntityType = table.Column<string>(type: "text", nullable: false),
//                     EntityId = table.Column<long>(type: "bigint", nullable: true),
//                     Description = table.Column<string>(type: "text", nullable: false),
//                     Status = table.Column<string>(type: "text", nullable: false),
//                     IsError = table.Column<int>(type: "integer", nullable: false),
//                     ErrorCode = table.Column<string>(type: "text", nullable: false),
//                     ErrorMessage = table.Column<string>(type: "text", nullable: false),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_activity_logs", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "booking_drafts",
//                 columns: table => new
//                 {
//                     Id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     UserId = table.Column<long>(type: "bigint", nullable: false),
//                     ScheduleId = table.Column<int>(type: "integer", nullable: false),
//                     SeatNumbers = table.Column<string>(type: "text", nullable: false),
//                     TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
//                     Status = table.Column<string>(type: "text", nullable: false),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_booking_drafts", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "booking_transactions",
//                 columns: table => new
//                 {
//                     Id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     BookingId = table.Column<long>(type: "bigint", nullable: false),
//                     TransactionRef = table.Column<string>(type: "text", nullable: false),
//                     PaymentMethod = table.Column<string>(type: "text", nullable: false),
//                     Amount = table.Column<decimal>(type: "numeric", nullable: false),
//                     PaymentStatus = table.Column<string>(type: "text", nullable: false),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_booking_transactions", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Countries",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Code = table.Column<string>(type: "text", nullable: false),
//                     Name = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Countries", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "DeletedUsers",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     original_user_id = table.Column<long>(type: "bigint", nullable: false),
//                     name = table.Column<string>(type: "text", nullable: true),
//                     email = table.Column<string>(type: "text", nullable: true),
//                     mobile = table.Column<string>(type: "text", nullable: true),
//                     address = table.Column<string>(type: "text", nullable: true),
//                     country = table.Column<string>(type: "text", nullable: true),
//                     state = table.Column<string>(type: "text", nullable: true),
//                     district = table.Column<string>(type: "text", nullable: true),
//                     pincode = table.Column<string>(type: "text", nullable: true),
//                     language = table.Column<string>(type: "text", nullable: true),
//                     genre = table.Column<string>(type: "text", nullable: true),
//                     profile_image_path = table.Column<string>(type: "text", nullable: true),
//                     created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     deleted_by = table.Column<string>(type: "text", nullable: true),
//                     revoke_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     revoked_by = table.Column<string>(type: "text", nullable: true),
//                     is_revoked = table.Column<bool>(type: "boolean", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_DeletedUsers", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "dummy_cards",
//                 columns: table => new
//                 {
//                     Id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     CardNo = table.Column<string>(type: "text", nullable: false),
//                     HolderName = table.Column<string>(type: "text", nullable: false),
//                     CVV = table.Column<string>(type: "text", nullable: false),
//                     Expiry = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_dummy_cards", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "LiveStreams",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Title = table.Column<string>(type: "text", nullable: false),
//                     Host = table.Column<string>(type: "text", nullable: false),
//                     Duration = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_LiveStreams", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Locations",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Country = table.Column<string>(type: "text", nullable: false),
//                     State = table.Column<string>(type: "text", nullable: false),
//                     Area = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Locations", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Movies",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Title = table.Column<string>(type: "text", nullable: false),
//                     Director = table.Column<string>(type: "text", nullable: true),
//                     Producer = table.Column<string>(type: "text", nullable: false),
//                     Cast = table.Column<string>(type: "text", nullable: false),
//                     Duration = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Movies", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "permissions",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     permission_code = table.Column<string>(type: "text", nullable: false),
//                     permission_name = table.Column<string>(type: "text", nullable: false),
//                     module_id = table.Column<long>(type: "bigint", nullable: false),
//                     action_type = table.Column<string>(type: "text", nullable: false),
//                     description = table.Column<string>(type: "text", nullable: false),
//                     created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_permissions", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "refund_action_logs",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     refund_id = table.Column<long>(type: "bigint", nullable: false),
//                     refund_ref = table.Column<string>(type: "text", nullable: true),
//                     action_name = table.Column<string>(type: "text", nullable: false),
//                     action_by = table.Column<string>(type: "text", nullable: true),
//                     action_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     action_notes = table.Column<string>(type: "text", nullable: true),
//                     ip_address = table.Column<string>(type: "text", nullable: true),
//                     created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_refund_action_logs", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "refunds",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     booking_id = table.Column<long>(type: "bigint", nullable: false),
//                     transaction_id = table.Column<long>(type: "bigint", nullable: false),
//                     user_id = table.Column<long>(type: "bigint", nullable: false),
//                     refund_ref = table.Column<string>(type: "text", nullable: false),
//                     refund_amount = table.Column<decimal>(type: "numeric", nullable: false),
//                     refund_reason = table.Column<string>(type: "text", nullable: false),
//                     refund_status = table.Column<string>(type: "text", nullable: false),
//                     refund_method = table.Column<string>(type: "text", nullable: false),
//                     gateway_refund_id = table.Column<string>(type: "text", nullable: true),
//                     failure_reason = table.Column<string>(type: "text", nullable: true),
//                     requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     workflow_action = table.Column<string>(type: "text", nullable: true),
//                     approved_by = table.Column<string>(type: "text", nullable: true),
//                     approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     rejected_by = table.Column<string>(type: "text", nullable: true),
//                     rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     retried_by = table.Column<string>(type: "text", nullable: true),
//                     retried_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     admin_notes = table.Column<string>(type: "text", nullable: true)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_refunds", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Regions",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     CountryId = table.Column<int>(type: "integer", nullable: false),
//                     StateId = table.Column<int>(type: "integer", nullable: false),
//                     DistrictId = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Regions", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "roles",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     role_code = table.Column<string>(type: "text", nullable: false),
//                     role_name = table.Column<string>(type: "text", nullable: false),
//                     role_description = table.Column<string>(type: "text", nullable: true),
//                     is_system_role = table.Column<bool>(type: "boolean", nullable: false),
//                     is_active = table.Column<bool>(type: "boolean", nullable: false),
//                     created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     created_by = table.Column<string>(type: "text", nullable: true),
//                     updated_by = table.Column<string>(type: "text", nullable: true)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_roles", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "screen_seats",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     screen_id = table.Column<long>(type: "bigint", nullable: false),
//                     seat_row = table.Column<string>(type: "text", nullable: false),
//                     seat_number = table.Column<string>(type: "text", nullable: false),
//                     seat_category = table.Column<string>(type: "text", nullable: false),
//                     seat_price = table.Column<decimal>(type: "numeric", nullable: false),
//                     is_active = table.Column<bool>(type: "boolean", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_screen_seats", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "seat_locks",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     user_id = table.Column<long>(type: "bigint", nullable: true),
//                     schedule_id = table.Column<int>(type: "integer", nullable: false),
//                     screen_seat_id = table.Column<long>(type: "bigint", nullable: false),
//                     locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     lock_status = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_seat_locks", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "StandupShows",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Title = table.Column<string>(type: "text", nullable: false),
//                     Comedian = table.Column<string>(type: "text", nullable: false),
//                     Duration = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_StandupShows", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "user_role_mappings",
//                 columns: table => new
//                 {
//                     id = table.Column<long>(type: "bigint", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     user_id = table.Column<long>(type: "bigint", nullable: false),
//                     role_id = table.Column<long>(type: "bigint", nullable: false),
//                     assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     assigned_by = table.Column<long>(type: "bigint", nullable: true),
//                     is_active = table.Column<bool>(type: "boolean", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_user_role_mappings", x => x.id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Users",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Name = table.Column<string>(type: "text", nullable: false),
//                     Address = table.Column<string>(type: "text", nullable: true),
//                     Genre = table.Column<string>(type: "text", nullable: true),
//                     Language = table.Column<string>(type: "text", nullable: true),
//                     ProfileImagePath = table.Column<string>(type: "text", nullable: true),
//                     Country = table.Column<string>(type: "text", nullable: true),
//                     State = table.Column<string>(type: "text", nullable: true),
//                     District = table.Column<string>(type: "text", nullable: true),
//                     Pincode = table.Column<string>(type: "text", nullable: true),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     CreatedBy = table.Column<string>(type: "text", nullable: true),
//                     UpdatedBy = table.Column<string>(type: "text", nullable: true),
//                     is_active = table.Column<bool>(type: "boolean", nullable: false),
//                     is_deleted = table.Column<bool>(type: "boolean", nullable: false),
//                     Email = table.Column<string>(type: "text", nullable: false),
//                     Password = table.Column<string>(type: "text", nullable: false),
//                     Mobile = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Users", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "vw_wallet_summary",
//                 columns: table => new
//                 {
//                     wallet_id = table.Column<long>(type: "bigint", nullable: false),
//                     user_id = table.Column<long>(type: "bigint", nullable: false),
//                     user_name = table.Column<string>(type: "text", nullable: true),
//                     user_email = table.Column<string>(type: "text", nullable: true),
//                     wallet_balance = table.Column<decimal>(type: "numeric", nullable: false),
//                     blocked_balance = table.Column<decimal>(type: "numeric", nullable: false),
//                     total_credits = table.Column<decimal>(type: "numeric", nullable: false),
//                     total_debits = table.Column<decimal>(type: "numeric", nullable: false),
//                     wallet_status = table.Column<string>(type: "text", nullable: true),
//                     loyalty_points = table.Column<int>(type: "integer", nullable: false),
//                     total_wallet_transactions = table.Column<int>(type: "integer", nullable: false),
//                     last_transaction_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
//                 },
//                 constraints: table =>
//                 {
//                 });

//             migrationBuilder.CreateTable(
//                 name: "States",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Name = table.Column<string>(type: "text", nullable: false),
//                     CountryId = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_States", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_States_Countries_CountryId",
//                         column: x => x.CountryId,
//                         principalTable: "Countries",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "ShowSchedules",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     MovieId = table.Column<int>(type: "integer", nullable: true),
//                     StandupShowId = table.Column<int>(type: "integer", nullable: true),
//                     LiveStreamId = table.Column<int>(type: "integer", nullable: true),
//                     LocationId = table.Column<int>(type: "integer", nullable: false),
//                     StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     Type = table.Column<string>(type: "text", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_ShowSchedules", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_ShowSchedules_LiveStreams_LiveStreamId",
//                         column: x => x.LiveStreamId,
//                         principalTable: "LiveStreams",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Restrict);
//                     table.ForeignKey(
//                         name: "FK_ShowSchedules_Locations_LocationId",
//                         column: x => x.LocationId,
//                         principalTable: "Locations",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                     table.ForeignKey(
//                         name: "FK_ShowSchedules_Movies_MovieId",
//                         column: x => x.MovieId,
//                         principalTable: "Movies",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Restrict);
//                     table.ForeignKey(
//                         name: "FK_ShowSchedules_StandupShows_StandupShowId",
//                         column: x => x.StandupShowId,
//                         principalTable: "StandupShows",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Restrict);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "Districts",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Name = table.Column<string>(type: "text", nullable: false),
//                     StateId = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Districts", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_Districts_States_StateId",
//                         column: x => x.StateId,
//                         principalTable: "States",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                 });

//             migrationBuilder.CreateIndex(
//                 name: "IX_Districts_StateId",
//                 table: "Districts",
//                 column: "StateId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_ShowSchedules_LiveStreamId",
//                 table: "ShowSchedules",
//                 column: "LiveStreamId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_ShowSchedules_LocationId",
//                 table: "ShowSchedules",
//                 column: "LocationId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_ShowSchedules_MovieId",
//                 table: "ShowSchedules",
//                 column: "MovieId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_ShowSchedules_StandupShowId",
//                 table: "ShowSchedules",
//                 column: "StandupShowId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_States_CountryId",
//                 table: "States",
//                 column: "CountryId");
//         }

//         /// <inheritdoc />
//         protected override void Down(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropTable(
//                 name: "activity_logs");

//             migrationBuilder.DropTable(
//                 name: "booking_drafts");

//             migrationBuilder.DropTable(
//                 name: "booking_transactions");

//             migrationBuilder.DropTable(
//                 name: "DeletedUsers");

//             migrationBuilder.DropTable(
//                 name: "Districts");

//             migrationBuilder.DropTable(
//                 name: "dummy_cards");

//             migrationBuilder.DropTable(
//                 name: "permissions");

//             migrationBuilder.DropTable(
//                 name: "refund_action_logs");

//             migrationBuilder.DropTable(
//                 name: "refunds");

//             migrationBuilder.DropTable(
//                 name: "Regions");

//             migrationBuilder.DropTable(
//                 name: "roles");

//             migrationBuilder.DropTable(
//                 name: "screen_seats");

//             migrationBuilder.DropTable(
//                 name: "seat_locks");

//             migrationBuilder.DropTable(
//                 name: "ShowSchedules");

//             migrationBuilder.DropTable(
//                 name: "user_role_mappings");

//             migrationBuilder.DropTable(
//                 name: "Users");

//             migrationBuilder.DropTable(
//                 name: "vw_wallet_summary");

//             migrationBuilder.DropTable(
//                 name: "States");

//             migrationBuilder.DropTable(
//                 name: "LiveStreams");

//             migrationBuilder.DropTable(
//                 name: "Locations");

//             migrationBuilder.DropTable(
//                 name: "Movies");

//             migrationBuilder.DropTable(
//                 name: "StandupShows");

//             migrationBuilder.DropTable(
//                 name: "Countries");
//         }
//     }
// }
