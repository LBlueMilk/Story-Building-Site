using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendAPI.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    email_verification_token = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    restored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_failed_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_change = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    device_info = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("CK_RefreshToken_ExpiresAt", "expires_at > CURRENT_TIMESTAMP");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reset_password_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reset_password_tokens", x => x.id);
                    table.CheckConstraint("CK_ResetPasswordToken_ExpiresAt", "expires_at > CURRENT_TIMESTAMP");
                    table.ForeignKey(
                        name: "FK_reset_password_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    restored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    share_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    share_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stories", x => x.id);
                    table.ForeignKey(
                        name: "FK_stories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disconnected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_providers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "story_data",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    story_id = table.Column<int>(type: "integer", nullable: false),
                    canvas_json = table.Column<string>(type: "text", nullable: true),
                    character_json = table.Column<string>(type: "text", nullable: true),
                    timeline_json = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_story_data", x => x.id);
                    table.ForeignKey(
                        name: "FK_story_data_stories_story_id",
                        column: x => x.story_id,
                        principalTable: "stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "story_shared_users",
                columns: table => new
                {
                    story_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_story_shared_users", x => new { x.story_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_story_shared_users_stories_story_id",
                        column: x => x.story_id,
                        principalTable: "stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_story_shared_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id_token_hash",
                table: "refresh_tokens",
                columns: new[] { "user_id", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reset_password_tokens_user_id_token",
                table: "reset_password_tokens",
                columns: new[] { "user_id", "token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stories_user_id",
                table: "stories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_story_data_story_id",
                table: "story_data",
                column: "story_id");

            migrationBuilder.CreateIndex(
                name: "IX_story_shared_users_user_id",
                table: "story_shared_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_providers_user_id_provider",
                table: "user_providers",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_at",
                table: "users",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "reset_password_tokens");

            migrationBuilder.DropTable(
                name: "story_data");

            migrationBuilder.DropTable(
                name: "story_shared_users");

            migrationBuilder.DropTable(
                name: "user_providers");

            migrationBuilder.DropTable(
                name: "stories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
