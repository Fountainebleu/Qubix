using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ToTable(
            "AnswerOptions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_AnswerOptions_Text",
                    "char_length(btrim(\"Text\")) > 0");
                table.HasCheckConstraint(
                    "CK_AnswerOptions_Position",
                    "\"Position\" >= 0");
            });

        builder.HasKey(answerOption => answerOption.Id);

        builder.Property(answerOption => answerOption.Id)
            .ValueGeneratedNever();

        builder.Property(answerOption => answerOption.QuestionId)
            .IsRequired();

        builder.Property(answerOption => answerOption.Text)
            .HasMaxLength(AnswerOption.MaxTextLength)
            .IsRequired();

        builder.Property(answerOption => answerOption.IsCorrect)
            .IsRequired();

        builder.Property(answerOption => answerOption.Position)
            .IsRequired();

        builder.HasOne<Question>()
            .WithMany(question => question.AnswerOptions)
            .HasForeignKey(answerOption => answerOption.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(answerOption => new
            {
                answerOption.QuestionId,
                answerOption.Position
            })
            .IsUnique()
            .HasDatabaseName("UX_AnswerOptions_QuestionId_Position");
    }
}
