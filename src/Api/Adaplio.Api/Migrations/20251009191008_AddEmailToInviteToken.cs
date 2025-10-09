using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToInviteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "invite_token",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "invite_token");
        }
    }
}
