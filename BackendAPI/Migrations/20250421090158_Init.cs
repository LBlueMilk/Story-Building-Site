using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendAPI.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_story_data_stories_StoryId",
                table: "story_data");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "story_data",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "story_data",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TimelineJson",
                table: "story_data",
                newName: "timeline_json");

            migrationBuilder.RenameColumn(
                name: "StoryId",
                table: "story_data",
                newName: "story_id");

            migrationBuilder.RenameColumn(
                name: "CharacterJson",
                table: "story_data",
                newName: "character_json");

            migrationBuilder.RenameColumn(
                name: "CanvasJson",
                table: "story_data",
                newName: "canvas_json");

            migrationBuilder.RenameIndex(
                name: "IX_story_data_StoryId",
                table: "story_data",
                newName: "IX_story_data_story_id");

            migrationBuilder.AddForeignKey(
                name: "FK_story_data_stories_story_id",
                table: "story_data",
                column: "story_id",
                principalTable: "stories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_story_data_stories_story_id",
                table: "story_data");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "story_data",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "story_data",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "timeline_json",
                table: "story_data",
                newName: "TimelineJson");

            migrationBuilder.RenameColumn(
                name: "story_id",
                table: "story_data",
                newName: "StoryId");

            migrationBuilder.RenameColumn(
                name: "character_json",
                table: "story_data",
                newName: "CharacterJson");

            migrationBuilder.RenameColumn(
                name: "canvas_json",
                table: "story_data",
                newName: "CanvasJson");

            migrationBuilder.RenameIndex(
                name: "IX_story_data_story_id",
                table: "story_data",
                newName: "IX_story_data_StoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_story_data_stories_StoryId",
                table: "story_data",
                column: "StoryId",
                principalTable: "stories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
