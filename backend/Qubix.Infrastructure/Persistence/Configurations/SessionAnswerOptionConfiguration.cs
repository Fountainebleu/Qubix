using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class SessionAnswerOptionConfiguration :
    IEntityTypeConfiguration<SessionAnswerOption>
{
    public void Configure(EntityTypeBuilder<SessionAnswerOption> builder)
    {
        builder.ToTable(
            "SessionAnswerOptions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_SessionAnswerOptions_Text",
                    "char_length(btrim(\"Text\")) > 0");
                table.HasCheckConstraint(
                    "CK_SessionAnswerOptions_Position",
                    "\"Position\" >= 0");
            });

        builder.HasKey(answerOption => answerOption.Id);

        builder.Property(answerOption => answerOption.Id)
            .ValueGeneratedNever();

        builder.Property(answerOption => answerOption.SessionQuestionId)
            .IsRequired();

        builder.Property(answerOption => answerOption.SourceAnswerOptionId)
            .IsRequired();

        builder.Property(answerOption => answerOption.Text)
            .HasMaxLength(AnswerOption.MaxTextLength)
            .IsRequired();

        builder.Property(answerOption => answerOption.IsCorrect)
            .IsRequired();

        builder.Property(answerOption => answerOption.Position)
            .IsRequired();

        builder.HasOne<SessionQuestion>()
            .WithMany(question => question.AnswerOptions)
            .HasForeignKey(answerOption => answerOption.SessionQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AnswerOption>()
            .WithMany()
            .HasForeignKey(answerOption => answerOption.SourceAnswerOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(answerOption => new
            {
                answerOption.SessionQuestionId,
                answerOption.Position
            })
            .IsUnique()
            .HasDatabaseName("UX_SessionAnswerOptions_SessionQuestionId_Position");

        builder.HasIndex(answerOption => new
            {
                answerOption.SessionQuestionId,
                answerOption.SourceAnswerOptionId
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_SessionAnswerOptions_SessionQuestionId_SourceAnswerOptionId");
    }
}
