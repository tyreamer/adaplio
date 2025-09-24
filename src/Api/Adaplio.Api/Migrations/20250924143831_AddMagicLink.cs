using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "magic_link",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_magic_link", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_code",
                table: "magic_link",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_email_created_at",
                table: "magic_link",
                columns: new[] { "email", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "magic_link");
        }
    }
}
