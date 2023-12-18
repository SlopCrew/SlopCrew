using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlopCrew.Server.Migrations
{
    /// <inheritdoc />
    public partial class StoreDiscordAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordAvatar",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordAvatar",
                table: "Users");
        }
    }
}
