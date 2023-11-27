using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlopCrew.Server.Migrations
{
    /// <inheritdoc />
    public partial class Crews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RepresentingCrewId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Crews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    InviteCodes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrewUser",
                columns: table => new
                {
                    CrewsId = table.Column<string>(type: "TEXT", nullable: false),
                    MembersDiscordId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewUser", x => new { x.CrewsId, x.MembersDiscordId });
                    table.ForeignKey(
                        name: "FK_CrewUser_Crews_CrewsId",
                        column: x => x.CrewsId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewUser_Users_MembersDiscordId",
                        column: x => x.MembersDiscordId,
                        principalTable: "Users",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewUser1",
                columns: table => new
                {
                    OwnedCrewsId = table.Column<string>(type: "TEXT", nullable: false),
                    OwnersDiscordId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewUser1", x => new { x.OwnedCrewsId, x.OwnersDiscordId });
                    table.ForeignKey(
                        name: "FK_CrewUser1_Crews_OwnedCrewsId",
                        column: x => x.OwnedCrewsId,
                        principalTable: "Crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewUser1_Users_OwnersDiscordId",
                        column: x => x.OwnersDiscordId,
                        principalTable: "Users",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RepresentingCrewId",
                table: "Users",
                column: "RepresentingCrewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crews_Tag",
                table: "Crews",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrewUser_MembersDiscordId",
                table: "CrewUser",
                column: "MembersDiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_CrewUser1_OwnersDiscordId",
                table: "CrewUser1",
                column: "OwnersDiscordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Crews_RepresentingCrewId",
                table: "Users",
                column: "RepresentingCrewId",
                principalTable: "Crews",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Crews_RepresentingCrewId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "CrewUser");

            migrationBuilder.DropTable(
                name: "CrewUser1");

            migrationBuilder.DropTable(
                name: "Crews");

            migrationBuilder.DropIndex(
                name: "IX_Users_RepresentingCrewId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RepresentingCrewId",
                table: "Users");
        }
    }
}
