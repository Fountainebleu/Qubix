using Qubix.Core.Common;

namespace Qubix.Core.Entities;

public sealed class Participant : Entity
{
    public const int MaxDisplayNameLength = 50;

    private Participant()
    {
    }

    public Participant(
        Guid id,
        Guid quizSessionId,
        Guid userId,
        string displayName,
        DateTimeOffset joinedAtUtc)
        : base(id)
    {
        QuizSessionId = Guard.NotEmpty(quizSessionId, nameof(quizSessionId));
        UserId = Guard.NotEmpty(userId, nameof(userId));
        DisplayName = Guard.Required(displayName, nameof(displayName), MaxDisplayNameLength);
        JoinedAtUtc = Guard.Utc(joinedAtUtc, nameof(joinedAtUtc));
    }

    public Guid QuizSessionId { get; private set; }

    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public int Score { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public void Rename(string displayName)
    {
        DisplayName = Guard.Required(displayName, nameof(displayName), MaxDisplayNameLength);
    }

    public void AwardPoints(int points)
    {
        Guard.NonNegative(points, nameof(points));
        Score = checked(Score + points);
    }
}
