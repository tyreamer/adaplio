using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Handle PostgreSQL TEXT to decimal conversion manually
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE transcript
                    ALTER COLUMN confidence_score TYPE decimal(5,4)
                    USING CASE
                        WHEN confidence_score ~ '^[0-9]*\.?[0-9]+$'
                        THEN confidence_score::decimal(5,4)
                        ELSE NULL
                    END;
                ");

                migrationBuilder.Sql(@"
                    ALTER TABLE extraction_result
                    ALTER COLUMN confidence_score TYPE decimal(5,4)
                    USING CASE
                        WHEN confidence_score ~ '^[0-9]*\.?[0-9]+$'
                        THEN confidence_score::decimal(5,4)
                        ELSE NULL
                    END;
                ");

                migrationBuilder.Sql(@"
                    ALTER TABLE adherence_week
                    ALTER COLUMN average_pain_level TYPE decimal(3,1)
                    USING CASE
                        WHEN average_pain_level ~ '^[0-9]*\.?[0-9]+$'
                        THEN average_pain_level::decimal(3,1)
                        ELSE NULL
                    END;
                ");

                migrationBuilder.Sql(@"
                    ALTER TABLE adherence_week
                    ALTER COLUMN average_difficulty_rating TYPE decimal(3,1)
                    USING CASE
                        WHEN average_difficulty_rating ~ '^[0-9]*\.?[0-9]+$'
                        THEN average_difficulty_rating::decimal(3,1)
                        ELSE NULL
                    END;
                ");

                migrationBuilder.Sql(@"
                    ALTER TABLE adherence_week
                    ALTER COLUMN adherence_percentage TYPE decimal(5,2)
                    USING CASE
                        WHEN adherence_percentage ~ '^[0-9]*\.?[0-9]+$'
                        THEN adherence_percentage::decimal(5,2)
                        ELSE 0
                    END;
                ");
            }
            migrationBuilder.DropColumn(
                name: "milestones_reached",
                table: "gamification");

            migrationBuilder.DropColumn(
                name: "total_exercises_completed",
                table: "gamification");

            migrationBuilder.DropColumn(
                name: "total_hold_seconds",
                table: "gamification");

            migrationBuilder.RenameColumn(
                name: "weekly_goals_met",
                table: "gamification",
                newName: "weekly_streaks");

            migrationBuilder.RenameColumn(
                name: "total_sessions",
                table: "gamification",
                newName: "longest_weekly_streak");

            // Handled manually above for PostgreSQL
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.AlterColumn<decimal>(
                    name: "confidence_score",
                    table: "transcript",
                    type: "decimal(5,4)",
                    precision: 5,
                    scale: 4,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "TEXT",
                    oldPrecision: 5,
                    oldScale: 4,
                    oldNullable: true);
            }

            migrationBuilder.AddColumn<string>(
                name: "availability_json",
                table: "trainer_profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credentials",
                table: "trainer_profile",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_reminder_time",
                table: "trainer_profile",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "trainer_profile",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "trainer_profile",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialties_json",
                table: "trainer_profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "trainer_profile",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "badges_earned",
                table: "gamification",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            // Handled manually above for PostgreSQL
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.AlterColumn<decimal>(
                    name: "confidence_score",
                    table: "extraction_result",
                    type: "decimal(5,4)",
                    precision: 5,
                    scale: 4,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "TEXT",
                    oldPrecision: 5,
                    oldScale: 4,
                    oldNullable: true);
            }

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                table: "app_user",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "app_user",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "app_user",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            // Handled manually above for PostgreSQL
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.AlterColumn<decimal>(
                    name: "average_pain_level",
                    table: "adherence_week",
                    type: "decimal(3,1)",
                    precision: 3,
                    scale: 1,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "TEXT",
                    oldPrecision: 3,
                    oldScale: 1,
                    oldNullable: true);

                migrationBuilder.AlterColumn<decimal>(
                    name: "average_difficulty_rating",
                    table: "adherence_week",
                    type: "decimal(3,1)",
                    precision: 3,
                    scale: 1,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "TEXT",
                    oldPrecision: 3,
                    oldScale: 1,
                    oldNullable: true);

                migrationBuilder.AlterColumn<decimal>(
                    name: "adherence_percentage",
                    table: "adherence_week",
                    type: "decimal(5,2)",
                    precision: 5,
                    scale: 2,
                    nullable: false,
                    oldClrType: typeof(decimal),
                    oldType: "TEXT",
                    oldPrecision: 5,
                    oldScale: 2);
            }

            migrationBuilder.CreateTable(
                name: "invite_token",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    token = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    grant_code_id = table.Column<int>(type: "INTEGER", nullable: true),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    used_by_client_profile_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invite_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_invite_token_client_profile_used_by_client_profile_id",
                        column: x => x.used_by_client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_invite_token_grant_code_grant_code_id",
                        column: x => x.grant_code_id,
                        principalTable: "grant_code",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "xp_award",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    progress_event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    xp_awarded = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_award", x => x.id);
                    table.ForeignKey(
                        name: "FK_xp_award_client_profile_user_id",
                        column: x => x.user_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_xp_award_progress_event_progress_event_id",
                        column: x => x.progress_event_id,
                        principalTable: "progress_event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invite_token_grant_code_id",
                table: "invite_token",
                column: "grant_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_invite_token_used_by_client_profile_id",
                table: "invite_token",
                column: "used_by_client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_xp_award_progress_event_id",
                table: "xp_award",
                column: "progress_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xp_award_user_id",
                table: "xp_award",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invite_token");

            migrationBuilder.DropTable(
                name: "xp_award");

            migrationBuilder.DropColumn(
                name: "availability_json",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "credentials",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "default_reminder_time",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "location",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "specialties_json",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "website",
                table: "trainer_profile");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "app_user");

            migrationBuilder.RenameColumn(
                name: "weekly_streaks",
                table: "gamification",
                newName: "weekly_goals_met");

            migrationBuilder.RenameColumn(
                name: "longest_weekly_streak",
                table: "gamification",
                newName: "total_sessions");

            migrationBuilder.AlterColumn<decimal>(
                name: "confidence_score",
                table: "transcript",
                type: "TEXT",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "badges_earned",
                table: "gamification",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "milestones_reached",
                table: "gamification",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_exercises_completed",
                table: "gamification",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_hold_seconds",
                table: "gamification",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "confidence_score",
                table: "extraction_result",
                type: "TEXT",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "average_pain_level",
                table: "adherence_week",
                type: "TEXT",
                precision: 3,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,1)",
                oldPrecision: 3,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "average_difficulty_rating",
                table: "adherence_week",
                type: "TEXT",
                precision: 3,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,1)",
                oldPrecision: 3,
                oldScale: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "adherence_percentage",
                table: "adherence_week",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
