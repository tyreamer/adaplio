using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanSystemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "days_of_week",
                table: "plan_template_item",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "plan_template",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "starts_on",
                table: "plan_proposal",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "day_of_week",
                table: "exercise_instance",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_of_week",
                table: "plan_template_item");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "plan_template");

            migrationBuilder.DropColumn(
                name: "starts_on",
                table: "plan_proposal");

            migrationBuilder.DropColumn(
                name: "day_of_week",
                table: "exercise_instance");
        }
    }
}
