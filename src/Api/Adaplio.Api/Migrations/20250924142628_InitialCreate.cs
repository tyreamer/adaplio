using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adaplio.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    user_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    is_verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exercise",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    default_sets = table.Column<int>(type: "INTEGER", nullable: true),
                    default_reps = table.Column<int>(type: "INTEGER", nullable: true),
                    default_hold_seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    instructions = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    alias = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    timezone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    preferences_json = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_profile_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    practice_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    license_number = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    bio = table.Column<string>(type: "TEXT", nullable: true),
                    mfa_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    mfa_secret = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_trainer_profile_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gamification",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    total_xp = table.Column<int>(type: "INTEGER", nullable: false),
                    current_level = table.Column<int>(type: "INTEGER", nullable: false),
                    current_streak = table.Column<int>(type: "INTEGER", nullable: false),
                    longest_streak = table.Column<int>(type: "INTEGER", nullable: false),
                    last_activity_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    badges_earned = table.Column<string>(type: "TEXT", nullable: true),
                    milestones_reached = table.Column<string>(type: "TEXT", nullable: true),
                    weekly_goals_met = table.Column<int>(type: "INTEGER", nullable: false),
                    total_sessions = table.Column<int>(type: "INTEGER", nullable: false),
                    total_exercises_completed = table.Column<int>(type: "INTEGER", nullable: false),
                    total_hold_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gamification", x => x.id);
                    table.ForeignKey(
                        name: "FK_gamification_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_asset",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: true),
                    filename = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    file_size = table.Column<long>(type: "INTEGER", nullable: false),
                    storage_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    metadata_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_asset", x => x.id);
                    table.ForeignKey(
                        name: "FK_media_asset_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "consent_grant",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    trainer_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    scope = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_grant", x => x.id);
                    table.ForeignKey(
                        name: "FK_consent_grant_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_consent_grant_trainer_profile_trainer_profile_id",
                        column: x => x.trainer_profile_id,
                        principalTable: "trainer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    trainer_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    duration_weeks = table.Column<int>(type: "INTEGER", nullable: true),
                    is_public = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_template_trainer_profile_trainer_profile_id",
                        column: x => x.trainer_profile_id,
                        principalTable: "trainer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "extraction_result",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    media_asset_id = table.Column<int>(type: "INTEGER", nullable: false),
                    extraction_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    extracted_data_json = table.Column<string>(type: "TEXT", nullable: false),
                    confidence_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    is_confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extraction_result", x => x.id);
                    table.ForeignKey(
                        name: "FK_extraction_result_media_asset_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_asset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transcript",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    media_asset_id = table.Column<int>(type: "INTEGER", nullable: false),
                    text_content = table.Column<string>(type: "TEXT", nullable: false),
                    language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    confidence_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                    processing_time_ms = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    segments_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transcript", x => x.id);
                    table.ForeignKey(
                        name: "FK_transcript_media_asset_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_asset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_proposal",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    trainer_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    plan_template_id = table.Column<int>(type: "INTEGER", nullable: true),
                    proposal_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    proposed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    custom_plan_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_proposal", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_proposal_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plan_proposal_plan_template_plan_template_id",
                        column: x => x.plan_template_id,
                        principalTable: "plan_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_proposal_trainer_profile_trainer_profile_id",
                        column: x => x.trainer_profile_id,
                        principalTable: "trainer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_template_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    plan_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    exercise_id = table.Column<int>(type: "INTEGER", nullable: false),
                    order_index = table.Column<int>(type: "INTEGER", nullable: false),
                    sets = table.Column<int>(type: "INTEGER", nullable: true),
                    reps = table.Column<int>(type: "INTEGER", nullable: true),
                    hold_seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    frequency_per_week = table.Column<int>(type: "INTEGER", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_template_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_template_item_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plan_template_item_plan_template_plan_template_id",
                        column: x => x.plan_template_id,
                        principalTable: "plan_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_instance",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    plan_proposal_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    start_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    planned_end_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actual_end_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_instance", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_instance_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plan_instance_plan_proposal_plan_proposal_id",
                        column: x => x.plan_proposal_id,
                        principalTable: "plan_proposal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adherence_week",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    plan_instance_id = table.Column<int>(type: "INTEGER", nullable: true),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    week_number = table.Column<int>(type: "INTEGER", nullable: false),
                    week_start_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    total_exercises_planned = table.Column<int>(type: "INTEGER", nullable: false),
                    total_exercises_completed = table.Column<int>(type: "INTEGER", nullable: false),
                    adherence_percentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    total_hold_seconds_planned = table.Column<int>(type: "INTEGER", nullable: false),
                    total_hold_seconds_completed = table.Column<int>(type: "INTEGER", nullable: false),
                    average_difficulty_rating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 1, nullable: true),
                    average_pain_level = table.Column<decimal>(type: "TEXT", precision: 3, scale: 1, nullable: true),
                    calculated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_week", x => x.id);
                    table.ForeignKey(
                        name: "FK_adherence_week_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_adherence_week_plan_instance_plan_instance_id",
                        column: x => x.plan_instance_id,
                        principalTable: "plan_instance",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "exercise_instance",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    plan_instance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    exercise_id = table.Column<int>(type: "INTEGER", nullable: false),
                    week_number = table.Column<int>(type: "INTEGER", nullable: false),
                    order_index = table.Column<int>(type: "INTEGER", nullable: false),
                    target_sets = table.Column<int>(type: "INTEGER", nullable: true),
                    target_reps = table.Column<int>(type: "INTEGER", nullable: true),
                    target_hold_seconds = table.Column<int>(type: "INTEGER", nullable: true),
                    frequency_per_week = table.Column<int>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_instance", x => x.id);
                    table.ForeignKey(
                        name: "FK_exercise_instance_exercise_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_instance_plan_instance_plan_instance_id",
                        column: x => x.plan_instance_id,
                        principalTable: "plan_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_item_acceptance",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    plan_instance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    exercise_instance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    modified_sets = table.Column<int>(type: "INTEGER", nullable: true),
                    modified_reps = table.Column<int>(type: "INTEGER", nullable: true),
                    modified_hold_seconds = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_item_acceptance", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_item_acceptance_exercise_instance_exercise_instance_id",
                        column: x => x.exercise_instance_id,
                        principalTable: "exercise_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plan_item_acceptance_plan_instance_plan_instance_id",
                        column: x => x.plan_instance_id,
                        principalTable: "plan_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "progress_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    exercise_instance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    sets_completed = table.Column<int>(type: "INTEGER", nullable: true),
                    reps_completed = table.Column<int>(type: "INTEGER", nullable: true),
                    hold_seconds_completed = table.Column<int>(type: "INTEGER", nullable: true),
                    difficulty_rating = table.Column<int>(type: "INTEGER", nullable: true),
                    pain_level = table.Column<int>(type: "INTEGER", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    logged_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    session_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_progress_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_progress_event_client_profile_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_progress_event_exercise_instance_exercise_instance_id",
                        column: x => x.exercise_instance_id,
                        principalTable: "exercise_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adherence_week_client_profile_id_year_week_number",
                table: "adherence_week",
                columns: new[] { "client_profile_id", "year", "week_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adherence_week_plan_instance_id",
                table: "adherence_week",
                column: "plan_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_email",
                table: "app_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_profile_alias",
                table: "client_profile",
                column: "alias",
                unique: true,
                filter: "[alias] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_client_profile_user_id",
                table: "client_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consent_grant_client_profile_id_trainer_profile_id_scope",
                table: "consent_grant",
                columns: new[] { "client_profile_id", "trainer_profile_id", "scope" },
                unique: true,
                filter: "[revoked_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_consent_grant_trainer_profile_id",
                table: "consent_grant",
                column: "trainer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_instance_exercise_id",
                table: "exercise_instance",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_instance_plan_instance_id",
                table: "exercise_instance",
                column: "plan_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_extraction_result_media_asset_id",
                table: "extraction_result",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_gamification_client_profile_id",
                table: "gamification",
                column: "client_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_asset_client_profile_id",
                table: "media_asset",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_instance_client_profile_id",
                table: "plan_instance",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_instance_plan_proposal_id",
                table: "plan_instance",
                column: "plan_proposal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_item_acceptance_exercise_instance_id",
                table: "plan_item_acceptance",
                column: "exercise_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_item_acceptance_plan_instance_id",
                table: "plan_item_acceptance",
                column: "plan_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_proposal_client_profile_id",
                table: "plan_proposal",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_proposal_plan_template_id",
                table: "plan_proposal",
                column: "plan_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_proposal_trainer_profile_id",
                table: "plan_proposal",
                column: "trainer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_template_trainer_profile_id",
                table: "plan_template",
                column: "trainer_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_template_item_exercise_id",
                table: "plan_template_item",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_template_item_plan_template_id",
                table: "plan_template_item",
                column: "plan_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_event_client_profile_id",
                table: "progress_event",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_event_exercise_instance_id",
                table: "progress_event",
                column: "exercise_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_profile_user_id",
                table: "trainer_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transcript_media_asset_id",
                table: "transcript",
                column: "media_asset_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adherence_week");

            migrationBuilder.DropTable(
                name: "consent_grant");

            migrationBuilder.DropTable(
                name: "extraction_result");

            migrationBuilder.DropTable(
                name: "gamification");

            migrationBuilder.DropTable(
                name: "plan_item_acceptance");

            migrationBuilder.DropTable(
                name: "plan_template_item");

            migrationBuilder.DropTable(
                name: "progress_event");

            migrationBuilder.DropTable(
                name: "transcript");

            migrationBuilder.DropTable(
                name: "exercise_instance");

            migrationBuilder.DropTable(
                name: "media_asset");

            migrationBuilder.DropTable(
                name: "exercise");

            migrationBuilder.DropTable(
                name: "plan_instance");

            migrationBuilder.DropTable(
                name: "plan_proposal");

            migrationBuilder.DropTable(
                name: "client_profile");

            migrationBuilder.DropTable(
                name: "plan_template");

            migrationBuilder.DropTable(
                name: "trainer_profile");

            migrationBuilder.DropTable(
                name: "app_user");
        }
    }
}
