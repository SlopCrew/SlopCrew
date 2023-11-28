using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlopCrew.Server.Migrations
{
    /// <inheritdoc />
    public partial class FuckItSuperOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SuperOwnerDiscordId",
                table: "Crews",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Crews_SuperOwnerDiscordId",
                table: "Crews",
                column: "SuperOwnerDiscordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Crews_Users_SuperOwnerDiscordId",
                table: "Crews",
                column: "SuperOwnerDiscordId",
                principalTable: "Users",
                principalColumn: "DiscordId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Crews_Users_SuperOwnerDiscordId",
                table: "Crews");

            migrationBuilder.DropIndex(
                name: "IX_Crews_SuperOwnerDiscordId",
                table: "Crews");

            migrationBuilder.DropColumn(
                name: "SuperOwnerDiscordId",
                table: "Crews");
        }
    }
}
