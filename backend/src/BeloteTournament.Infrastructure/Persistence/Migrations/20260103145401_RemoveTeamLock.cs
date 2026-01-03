using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloteTournament.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeamLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
