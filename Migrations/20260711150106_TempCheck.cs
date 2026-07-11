using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmarShowsBook.Migrations
{
    /// <inheritdoc />
    public partial class TempCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "activity_logs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Module",
                table: "activity_logs",
                newName: "module");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "activity_logs",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "activity_logs",
                newName: "action");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "activity_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "activity_logs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsError",
                table: "activity_logs",
                newName: "is_error");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "activity_logs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "ErrorCode",
                table: "activity_logs",
                newName: "error_code");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "activity_logs",
                newName: "entity_type");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "activity_logs",
                newName: "entity_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "activity_logs",
                newName: "created_at");

            migrationBuilder.AddColumn<string>(
                name: "endpoint",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "error_source",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "activity_logs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                table: "activity_logs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                table: "activity_logs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_method",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "stack_trace",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "activity_logs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "endpoint",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "error_source",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "new_value",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "old_value",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "request_method",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "stack_trace",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "activity_logs");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "activity_logs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "module",
                table: "activity_logs",
                newName: "Module");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "activity_logs",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "activity_logs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "activity_logs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "activity_logs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_error",
                table: "activity_logs",
                newName: "IsError");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "activity_logs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "error_code",
                table: "activity_logs",
                newName: "ErrorCode");

            migrationBuilder.RenameColumn(
                name: "entity_type",
                table: "activity_logs",
                newName: "EntityType");

            migrationBuilder.RenameColumn(
                name: "entity_id",
                table: "activity_logs",
                newName: "EntityId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "activity_logs",
                newName: "CreatedAt");
        }
    }
}
