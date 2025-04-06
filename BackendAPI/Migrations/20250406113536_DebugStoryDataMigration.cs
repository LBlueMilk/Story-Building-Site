using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendAPI.Migrations
{
    /// <inheritdoc />
    public partial class DebugStoryDataMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token",
                table: "refresh_tokens",
                newName: "token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user_id_token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user_id_token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_token_hash");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "email_verification_token",
                table: "users",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_code",
                table: "users",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "story_shared_users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "stories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "restored_at",
                table: "stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "share_token",
                table: "stories",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "share_token_expires_at",
                table: "stories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_info",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revoked_at",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "story_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    CanvasJson = table.Column<string>(type: "text", nullable: true),
                    CharacterJson = table.Column<string>(type: "text", nullable: true),
                    TimelineJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_story_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_story_data_stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_at",
                table: "users",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_story_data_StoryId",
                table: "story_data",
                column: "StoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "story_data");

            migrationBuilder.DropIndex(
                name: "IX_users_deleted_at",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_user_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "restored_at",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "share_token",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "share_token_expires_at",
                table: "stories");

            migrationBuilder.DropColumn(
                name: "device_info",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "refresh_tokens",
                newName: "token");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user_id_token_hash",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user_id_token");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_token");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "story_shared_users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "stories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
