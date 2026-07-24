using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Core.Entities;
using Qubix.Infrastructure.Identity;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class QuizSessionConfiguration : IEntityTypeConfiguration<QuizSession>
{
    public void Configure(EntityTypeBuilder<QuizSession> builder)
    {
        builder.ToTable(
            "QuizSessions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_QuizSessions_RoomCode",
                    $"char_length(\"RoomCode\") = {QuizSession.RoomCodeLength} " +
                    "AND \"RoomCode\" ~ '^[A-Z0-9]+$'");
                table.HasCheckConstraint(
                    "CK_QuizSessions_Status",
                    "\"Status\" IN ('Waiting', 'Running', 'Finished')");
                table.HasCheckConstraint(
                    "CK_QuizSessions_Lifecycle",
                    "(\"Status\" = 'Waiting' AND \"StartedAtUtc\" IS NULL " +
                    "AND \"FinishedAtUtc\" IS NULL) OR " +
                    "(\"Status\" = 'Running' AND \"StartedAtUtc\" IS NOT NULL " +
                    "AND \"FinishedAtUtc\" IS NULL) OR " +
                    "(\"Status\" = 'Finished' AND \"StartedAtUtc\" IS NOT NULL " +
                    "AND \"FinishedAtUtc\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_QuizSessions_FinishedAfterStart",
                    "\"FinishedAtUtc\" IS NULL OR \"FinishedAtUtc\" >= \"StartedAtUtc\"");
            });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .ValueGeneratedNever();

        builder.Property(session => session.QuizId)
            .IsRequired();

        builder.Property(session => session.OrganizerId)
            .IsRequired();

        builder.Property(session => session.RoomCode)
            .HasMaxLength(QuizSession.RoomCodeLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(session => session.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Quiz>()
            .WithMany()
            .HasForeignKey(session => session.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(session => session.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SessionQuestion>()
            .WithMany()
            .HasForeignKey(session => session.CurrentQuestionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(session => session.RoomCode)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Waiting', 'Running')")
            .HasDatabaseName("UX_QuizSessions_ActiveRoomCode");

        builder.HasIndex(session => new
            {
                session.OrganizerId,
                session.CreatedAtUtc
            })
            .HasDatabaseName("IX_QuizSessions_OrganizerId_CreatedAtUtc");

        builder.HasIndex(session => session.QuizId)
            .HasDatabaseName("IX_QuizSessions_QuizId");

        builder.Navigation(session => session.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(session => session.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
