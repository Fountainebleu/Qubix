using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class AnswerSubmissionConfiguration :
    IEntityTypeConfiguration<AnswerSubmission>
{
    public void Configure(EntityTypeBuilder<AnswerSubmission> builder)
    {
        builder.ToTable(
            "AnswerSubmissions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_AnswerSubmissions_ResponseTimeMilliseconds",
                    "\"ResponseTimeMilliseconds\" >= 0");
                table.HasCheckConstraint(
                    "CK_AnswerSubmissions_AwardedPoints",
                    "\"AwardedPoints\" >= 0");
                table.HasCheckConstraint(
                    "CK_AnswerSubmissions_IncorrectHasNoPoints",
                    "\"IsCorrect\" IS DISTINCT FROM FALSE OR \"AwardedPoints\" = 0");
                table.HasCheckConstraint(
                    "CK_AnswerSubmissions_UngradedHasNoPoints",
                    "\"IsCorrect\" IS NOT NULL OR \"AwardedPoints\" = 0");
            });

        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.Id)
            .ValueGeneratedNever();

        builder.Property(submission => submission.ParticipantId)
            .IsRequired();

        builder.Property(submission => submission.SessionQuestionId)
            .IsRequired();

        builder.Property(submission => submission.SubmittedAtUtc)
            .IsRequired();

        builder.Property(submission => submission.ResponseTimeMilliseconds)
            .IsRequired();

        builder.Property(submission => submission.AwardedPoints)
            .IsRequired();

        builder.Ignore(submission => submission.IsGraded);

        builder.HasOne<Participant>()
            .WithMany()
            .HasForeignKey(submission => submission.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SessionQuestion>()
            .WithMany()
            .HasForeignKey(submission => submission.SessionQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(submission => new
            {
                submission.ParticipantId,
                submission.SessionQuestionId
            })
            .IsUnique()
            .HasDatabaseName("UX_AnswerSubmissions_ParticipantId_SessionQuestionId");

        builder.HasIndex(submission => submission.SessionQuestionId)
            .HasDatabaseName("IX_AnswerSubmissions_SessionQuestionId");

        builder.Navigation(submission => submission.SelectedOptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
