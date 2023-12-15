using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlopCrew.Server.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    DiscordId = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordUsername = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordToken = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordRefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordTokenExpires = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GameToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.DiscordId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
