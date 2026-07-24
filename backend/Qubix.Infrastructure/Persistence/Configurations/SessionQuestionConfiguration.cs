using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class SessionQuestionConfiguration : IEntityTypeConfiguration<SessionQuestion>
{
    public void Configure(EntityTypeBuilder<SessionQuestion> builder)
    {
        builder.ToTable(
            "SessionQuestions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Content",
                    "NULLIF(btrim(\"Text\"), '') IS NOT NULL " +
                    "OR NULLIF(btrim(\"ImageUrl\"), '') IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Position",
                    "\"Position\" >= 0");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_TimeLimitSeconds",
                    $"\"TimeLimitSeconds\" BETWEEN {Question.MinimumTimeLimitSeconds} " +
                    $"AND {Question.MaximumTimeLimitSeconds}");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Points",
                    $"\"Points\" BETWEEN {Question.MinimumPoints} AND {Question.MaximumPoints}");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Type",
                    "\"Type\" IN ('SingleChoice', 'MultipleChoice')");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Status",
                    "\"Status\" IN ('Pending', 'Open', 'Closed')");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_Lifecycle",
                    "(\"Status\" = 'Pending' AND \"OpensAtUtc\" IS NULL " +
                    "AND \"ClosesAtUtc\" IS NULL AND \"ClosedAtUtc\" IS NULL) OR " +
                    "(\"Status\" = 'Open' AND \"OpensAtUtc\" IS NOT NULL " +
                    "AND \"ClosesAtUtc\" IS NOT NULL AND \"ClosedAtUtc\" IS NULL) OR " +
                    "(\"Status\" = 'Closed' AND \"OpensAtUtc\" IS NOT NULL " +
                    "AND \"ClosesAtUtc\" IS NOT NULL AND \"ClosedAtUtc\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_DeadlineAfterOpen",
                    "\"ClosesAtUtc\" IS NULL OR \"ClosesAtUtc\" >= \"OpensAtUtc\"");
                table.HasCheckConstraint(
                    "CK_SessionQuestions_ClosedAfterOpen",
                    "\"ClosedAtUtc\" IS NULL OR \"ClosedAtUtc\" >= \"OpensAtUtc\"");
            });

        builder.HasKey(question => question.Id);

        builder.Property(question => question.Id)
            .ValueGeneratedNever();

        builder.Property(question => question.QuizSessionId)
            .IsRequired();

        builder.Property(question => question.SourceQuestionId)
            .IsRequired();

        builder.Property(question => question.Text)
            .HasMaxLength(Question.MaxTextLength);

        builder.Property(question => question.ImageUrl)
            .HasMaxLength(Question.MaxImageUrlLength);

        builder.Property(question => question.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(question => question.Position)
            .IsRequired();

        builder.Property(question => question.TimeLimitSeconds)
            .IsRequired();

        builder.Property(question => question.Points)
            .IsRequired();

        builder.Property(question => question.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasOne<QuizSession>()
            .WithMany(session => session.Questions)
            .HasForeignKey(question => question.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(question => question.SourceQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(question => new
            {
                question.QuizSessionId,
                question.Position
            })
            .IsUnique()
            .HasDatabaseName("UX_SessionQuestions_QuizSessionId_Position");

        builder.HasIndex(question => new
            {
                question.QuizSessionId,
                question.SourceQuestionId
            })
            .IsUnique()
            .HasDatabaseName("UX_SessionQuestions_QuizSessionId_SourceQuestionId");

        builder.Navigation(question => question.AnswerOptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
