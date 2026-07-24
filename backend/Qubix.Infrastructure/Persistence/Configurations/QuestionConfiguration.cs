using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable(
            "Questions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Questions_Content",
                    "NULLIF(btrim(\"Text\"), '') IS NOT NULL " +
                    "OR NULLIF(btrim(\"ImageUrl\"), '') IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_Questions_Position",
                    "\"Position\" >= 0");
                table.HasCheckConstraint(
                    "CK_Questions_TimeLimitSeconds",
                    $"\"TimeLimitSeconds\" BETWEEN {Question.MinimumTimeLimitSeconds} " +
                    $"AND {Question.MaximumTimeLimitSeconds}");
                table.HasCheckConstraint(
                    "CK_Questions_Points",
                    $"\"Points\" BETWEEN {Question.MinimumPoints} AND {Question.MaximumPoints}");
                table.HasCheckConstraint(
                    "CK_Questions_Type",
                    "\"Type\" IN ('SingleChoice', 'MultipleChoice')");
            });

        builder.HasKey(question => question.Id);

        builder.Property(question => question.Id)
            .ValueGeneratedNever();

        builder.Property(question => question.QuizId)
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

        builder.HasOne<Quiz>()
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(question => question.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(question => new { question.QuizId, question.Position })
            .IsUnique()
            .HasDatabaseName("UX_Questions_QuizId_Position");

        builder.Navigation(question => question.AnswerOptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
