using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlopCrew.Server.Migrations
{
    /// <inheritdoc />
    public partial class CommunityContributor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommunityContributor",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommunityContributor",
                table: "Users");
        }
    }
}
