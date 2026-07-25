using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Quizzes;

public sealed record QuizResponse(
    Guid Id,
    string Title,
    string? Description,
    string? Category,
    string? Rules,
    int DefaultQuestionTimeSeconds,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static QuizResponse FromEntity(Quiz quiz)
    {
        return new QuizResponse(
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Category,
            quiz.Rules,
            quiz.DefaultQuestionTimeSeconds,
            quiz.Status.ToString(),
            quiz.CreatedAtUtc,
            quiz.UpdatedAtUtc);
    }
}
