using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class AnswerSubmissionOptionConfiguration :
    IEntityTypeConfiguration<AnswerSubmissionOption>
{
    public void Configure(EntityTypeBuilder<AnswerSubmissionOption> builder)
    {
        builder.ToTable("AnswerSubmissionOptions");

        builder.HasKey(answerOption => new
        {
            answerOption.AnswerSubmissionId,
            answerOption.SessionAnswerOptionId
        });

        builder.Property(answerOption => answerOption.AnswerSubmissionId)
            .ValueGeneratedNever();

        builder.Property(answerOption => answerOption.SessionAnswerOptionId)
            .ValueGeneratedNever();

        builder.HasOne<AnswerSubmission>()
            .WithMany(submission => submission.SelectedOptions)
            .HasForeignKey(answerOption => answerOption.AnswerSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SessionAnswerOption>()
            .WithMany()
            .HasForeignKey(answerOption => answerOption.SessionAnswerOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(answerOption => answerOption.SessionAnswerOptionId)
            .HasDatabaseName("IX_AnswerSubmissionOptions_SessionAnswerOptionId");
    }
}
