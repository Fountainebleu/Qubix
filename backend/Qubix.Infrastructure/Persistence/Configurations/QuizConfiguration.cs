using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable(
            "Quizzes",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Quizzes_Title",
                    "char_length(btrim(\"Title\")) > 0");
                table.HasCheckConstraint(
                    "CK_Quizzes_DefaultQuestionTimeSeconds",
                    $"\"DefaultQuestionTimeSeconds\" BETWEEN {Quiz.MinimumQuestionTimeSeconds} " +
                    $"AND {Quiz.MaximumQuestionTimeSeconds}");
                table.HasCheckConstraint(
                    "CK_Quizzes_Status",
                    "\"Status\" IN ('Draft', 'Published', 'Archived')");
                table.HasCheckConstraint(
                    "CK_Quizzes_UpdatedAfterCreated",
                    "\"UpdatedAtUtc\" >= \"CreatedAtUtc\"");
            });

        builder.HasKey(quiz => quiz.Id);

        builder.Property(quiz => quiz.Id)
            .ValueGeneratedNever();

        builder.Property(quiz => quiz.OwnerId)
            .IsRequired();

        builder.Property(quiz => quiz.Title)
            .HasMaxLength(Quiz.MaxTitleLength)
            .IsRequired();

        builder.Property(quiz => quiz.Description)
            .HasMaxLength(Quiz.MaxDescriptionLength);

        builder.Property(quiz => quiz.Category)
            .HasMaxLength(Quiz.MaxCategoryLength);

        builder.Property(quiz => quiz.Rules)
            .HasMaxLength(Quiz.MaxRulesLength);

        builder.Property(quiz => quiz.DefaultQuestionTimeSeconds)
            .IsRequired();

        builder.Property(quiz => quiz.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(quiz => quiz.CreatedAtUtc)
            .IsRequired();

        builder.Property(quiz => quiz.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(quiz => new { quiz.OwnerId, quiz.Status })
            .HasDatabaseName("IX_Quizzes_OwnerId_Status");

        builder.Navigation(quiz => quiz.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
