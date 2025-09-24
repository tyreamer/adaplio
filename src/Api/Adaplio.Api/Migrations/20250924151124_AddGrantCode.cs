using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGrantCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grant_code",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    trainer_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    used_by_client_profile_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grant_code", x => x.id);
                    table.ForeignKey(
                        name: "FK_grant_code_client_profile_used_by_client_profile_id",
                        column: x => x.used_by_client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_grant_code_trainer_profile_trainer_profile_id",
                        column: x => x.trainer_profile_id,
                        principalTable: "trainer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grant_code_code",
                table: "grant_code",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grant_code_trainer_profile_id_created_at",
                table: "grant_code",
                columns: new[] { "trainer_profile_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_grant_code_used_by_client_profile_id",
                table: "grant_code",
                column: "used_by_client_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grant_code");
        }
    }
}
