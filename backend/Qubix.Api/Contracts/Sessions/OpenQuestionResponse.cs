using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Sessions;

public sealed record OpenQuestionResponse(
    Guid Id,
    string? Text,
    string? ImageUrl,
    string Type,
    int Position,
    int TimeLimitSeconds,
    int Points,
    DateTimeOffset ClosesAtUtc,
    IReadOnlyCollection<SessionAnswerOptionResponse> AnswerOptions)
{
    public static OpenQuestionResponse FromEntity(
        SessionQuestion question)
    {
        return new OpenQuestionResponse(
            question.Id,
            question.Text,
            question.ImageUrl,
            question.Type.ToString(),
            question.Position,
            question.TimeLimitSeconds,
            question.Points,
            question.ClosesAtUtc!.Value,
            question.AnswerOptions
                .OrderBy(answerOption => answerOption.Position)
                .Select(SessionAnswerOptionResponse.FromEntity)
                .ToArray());
    }
}

public sealed record SessionAnswerOptionResponse(
    Guid Id,
    string Text,
    int Position)
{
    public static SessionAnswerOptionResponse FromEntity(
        SessionAnswerOption answerOption)
    {
        return new SessionAnswerOptionResponse(
            answerOption.Id,
            answerOption.Text,
            answerOption.Position);
    }
}
