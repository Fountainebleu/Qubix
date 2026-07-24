using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qubix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rules = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultQuestionTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.CheckConstraint("CK_Quizzes_DefaultQuestionTimeSeconds", "\"DefaultQuestionTimeSeconds\" BETWEEN 5 AND 300");
                    table.CheckConstraint("CK_Quizzes_Status", "\"Status\" IN ('Draft', 'Published', 'Archived')");
                    table.CheckConstraint("CK_Quizzes_Title", "char_length(btrim(\"Title\")) > 0");
                    table.CheckConstraint("CK_Quizzes_UpdatedAfterCreated", "\"UpdatedAtUtc\" >= \"CreatedAtUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.CheckConstraint("CK_Questions_Content", "NULLIF(btrim(\"Text\"), '') IS NOT NULL OR NULLIF(btrim(\"ImageUrl\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_Questions_Points", "\"Points\" BETWEEN 1 AND 10000");
                    table.CheckConstraint("CK_Questions_Position", "\"Position\" >= 0");
                    table.CheckConstraint("CK_Questions_TimeLimitSeconds", "\"TimeLimitSeconds\" BETWEEN 5 AND 300");
                    table.CheckConstraint("CK_Questions_Type", "\"Type\" IN ('SingleChoice', 'MultipleChoice')");
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.CheckConstraint("CK_AnswerOptions_Position", "\"Position\" >= 0");
                    table.CheckConstraint("CK_AnswerOptions_Text", "char_length(btrim(\"Text\")) > 0");
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerSubmissionOptions",
                columns: table => new
                {
                    AnswerSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionAnswerOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerSubmissionOptions", x => new { x.AnswerSubmissionId, x.SessionAnswerOptionId });
                });

            migrationBuilder.CreateTable(
                name: "AnswerSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseTimeMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    AwardedPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerSubmissions", x => x.Id);
                    table.CheckConstraint("CK_AnswerSubmissions_AwardedPoints", "\"AwardedPoints\" >= 0");
                    table.CheckConstraint("CK_AnswerSubmissions_IncorrectHasNoPoints", "\"IsCorrect\" IS DISTINCT FROM FALSE OR \"AwardedPoints\" = 0");
                    table.CheckConstraint("CK_AnswerSubmissions_ResponseTimeMilliseconds", "\"ResponseTimeMilliseconds\" >= 0");
                    table.CheckConstraint("CK_AnswerSubmissions_UngradedHasNoPoints", "\"IsCorrect\" IS NOT NULL OR \"AwardedPoints\" = 0");
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.CheckConstraint("CK_Participants_DisplayName", "char_length(btrim(\"DisplayName\")) > 0");
                    table.CheckConstraint("CK_Participants_Score", "\"Score\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "QuizSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomCode = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrentQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizSessions", x => x.Id);
                    table.CheckConstraint("CK_QuizSessions_FinishedAfterStart", "\"FinishedAtUtc\" IS NULL OR \"FinishedAtUtc\" >= \"StartedAtUtc\"");
                    table.CheckConstraint("CK_QuizSessions_Lifecycle", "(\"Status\" = 'Waiting' AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL) OR (\"Status\" = 'Running' AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NULL) OR (\"Status\" = 'Finished' AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_QuizSessions_RoomCode", "char_length(\"RoomCode\") = 6 AND \"RoomCode\" ~ '^[A-Z0-9]+$'");
                    table.CheckConstraint("CK_QuizSessions_Status", "\"Status\" IN ('Waiting', 'Running', 'Finished')");
                    table.ForeignKey(
                        name: "FK_QuizSessions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OpensAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosesAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionQuestions", x => x.Id);
                    table.CheckConstraint("CK_SessionQuestions_ClosedAfterOpen", "\"ClosedAtUtc\" IS NULL OR \"ClosedAtUtc\" >= \"OpensAtUtc\"");
                    table.CheckConstraint("CK_SessionQuestions_Content", "NULLIF(btrim(\"Text\"), '') IS NOT NULL OR NULLIF(btrim(\"ImageUrl\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_SessionQuestions_DeadlineAfterOpen", "\"ClosesAtUtc\" IS NULL OR \"ClosesAtUtc\" >= \"OpensAtUtc\"");
                    table.CheckConstraint("CK_SessionQuestions_Lifecycle", "(\"Status\" = 'Pending' AND \"OpensAtUtc\" IS NULL AND \"ClosesAtUtc\" IS NULL AND \"ClosedAtUtc\" IS NULL) OR (\"Status\" = 'Open' AND \"OpensAtUtc\" IS NOT NULL AND \"ClosesAtUtc\" IS NOT NULL AND \"ClosedAtUtc\" IS NULL) OR (\"Status\" = 'Closed' AND \"OpensAtUtc\" IS NOT NULL AND \"ClosesAtUtc\" IS NOT NULL AND \"ClosedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_SessionQuestions_Points", "\"Points\" BETWEEN 1 AND 10000");
                    table.CheckConstraint("CK_SessionQuestions_Position", "\"Position\" >= 0");
                    table.CheckConstraint("CK_SessionQuestions_Status", "\"Status\" IN ('Pending', 'Open', 'Closed')");
                    table.CheckConstraint("CK_SessionQuestions_TimeLimitSeconds", "\"TimeLimitSeconds\" BETWEEN 5 AND 300");
                    table.CheckConstraint("CK_SessionQuestions_Type", "\"Type\" IN ('SingleChoice', 'MultipleChoice')");
                    table.ForeignKey(
                        name: "FK_SessionQuestions_Questions_SourceQuestionId",
                        column: x => x.SourceQuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionQuestions_QuizSessions_QuizSessionId",
                        column: x => x.QuizSessionId,
                        principalTable: "QuizSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAnswerOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAnswerOptions", x => x.Id);
                    table.CheckConstraint("CK_SessionAnswerOptions_Position", "\"Position\" >= 0");
                    table.CheckConstraint("CK_SessionAnswerOptions_Text", "char_length(btrim(\"Text\")) > 0");
                    table.ForeignKey(
                        name: "FK_SessionAnswerOptions_AnswerOptions_SourceAnswerOptionId",
                        column: x => x.SourceAnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAnswerOptions_SessionQuestions_SessionQuestionId",
                        column: x => x.SessionQuestionId,
                        principalTable: "SessionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AnswerOptions_QuestionId_Position",
                table: "AnswerOptions",
                columns: new[] { "QuestionId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSubmissionOptions_SessionAnswerOptionId",
                table: "AnswerSubmissionOptions",
                column: "SessionAnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSubmissions_SessionQuestionId",
                table: "AnswerSubmissions",
                column: "SessionQuestionId");

            migrationBuilder.CreateIndex(
                name: "UX_AnswerSubmissions_ParticipantId_SessionQuestionId",
                table: "AnswerSubmissions",
                columns: new[] { "ParticipantId", "SessionQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_QuizSessionId_Score",
                table: "Participants",
                columns: new[] { "QuizSessionId", "Score" });

            migrationBuilder.CreateIndex(
                name: "UX_Participants_QuizSessionId_UserId",
                table: "Participants",
                columns: new[] { "QuizSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Questions_QuizId_Position",
                table: "Questions",
                columns: new[] { "QuizId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_CurrentQuestionId",
                table: "QuizSessions",
                column: "CurrentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_OrganizerId_CreatedAtUtc",
                table: "QuizSessions",
                columns: new[] { "OrganizerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_QuizId",
                table: "QuizSessions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "UX_QuizSessions_ActiveRoomCode",
                table: "QuizSessions",
                column: "RoomCode",
                unique: true,
                filter: "\"Status\" IN ('Waiting', 'Running')");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_OwnerId_Status",
                table: "Quizzes",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswerOptions_SourceAnswerOptionId",
                table: "SessionAnswerOptions",
                column: "SourceAnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionAnswerOptions_SessionQuestionId_Position",
                table: "SessionAnswerOptions",
                columns: new[] { "SessionQuestionId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SessionAnswerOptions_SessionQuestionId_SourceAnswerOptionId",
                table: "SessionAnswerOptions",
                columns: new[] { "SessionQuestionId", "SourceAnswerOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestions_SourceQuestionId",
                table: "SessionQuestions",
                column: "SourceQuestionId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionQuestions_QuizSessionId_Position",
                table: "SessionQuestions",
                columns: new[] { "QuizSessionId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SessionQuestions_QuizSessionId_SourceQuestionId",
                table: "SessionQuestions",
                columns: new[] { "QuizSessionId", "SourceQuestionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerSubmissionOptions_AnswerSubmissions_AnswerSubmissionId",
                table: "AnswerSubmissionOptions",
                column: "AnswerSubmissionId",
                principalTable: "AnswerSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerSubmissionOptions_SessionAnswerOptions_SessionAnswerO~",
                table: "AnswerSubmissionOptions",
                column: "SessionAnswerOptionId",
                principalTable: "SessionAnswerOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerSubmissions_Participants_ParticipantId",
                table: "AnswerSubmissions",
                column: "ParticipantId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerSubmissions_SessionQuestions_SessionQuestionId",
                table: "AnswerSubmissions",
                column: "SessionQuestionId",
                principalTable: "SessionQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_QuizSessions_QuizSessionId",
                table: "Participants",
                column: "QuizSessionId",
                principalTable: "QuizSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSessions_SessionQuestions_CurrentQuestionId",
                table: "QuizSessions",
                column: "CurrentQuestionId",
                principalTable: "SessionQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionQuestions_Questions_SourceQuestionId",
                table: "SessionQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSessions_SessionQuestions_CurrentQuestionId",
                table: "QuizSessions");

            migrationBuilder.DropTable(
                name: "AnswerSubmissionOptions");

            migrationBuilder.DropTable(
                name: "AnswerSubmissions");

            migrationBuilder.DropTable(
                name: "SessionAnswerOptions");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "SessionQuestions");

            migrationBuilder.DropTable(
                name: "QuizSessions");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
