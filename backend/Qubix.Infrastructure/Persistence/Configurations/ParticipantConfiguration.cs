using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable(
            "Participants",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Participants_DisplayName",
                    "char_length(btrim(\"DisplayName\")) > 0");
                table.HasCheckConstraint(
                    "CK_Participants_Score",
                    "\"Score\" >= 0");
            });

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id)
            .ValueGeneratedNever();

        builder.Property(participant => participant.QuizSessionId)
            .IsRequired();

        builder.Property(participant => participant.UserId)
            .IsRequired();

        builder.Property(participant => participant.DisplayName)
            .HasMaxLength(Participant.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(participant => participant.Score)
            .IsRequired();

        builder.Property(participant => participant.JoinedAtUtc)
            .IsRequired();

        builder.HasOne<QuizSession>()
            .WithMany(session => session.Participants)
            .HasForeignKey(participant => participant.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(participant => new
            {
                participant.QuizSessionId,
                participant.UserId
            })
            .IsUnique()
            .HasDatabaseName("UX_Participants_QuizSessionId_UserId");

        builder.HasIndex(participant => new
            {
                participant.QuizSessionId,
                participant.Score
            })
            .HasDatabaseName("IX_Participants_QuizSessionId_Score");
    }
}
