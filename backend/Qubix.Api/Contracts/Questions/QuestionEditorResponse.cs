using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Questions;

public sealed record QuestionEditorResponse(
    Guid Id,
    string? Text,
    string? ImageUrl,
    string Type,
    int Position,
    int TimeLimitSeconds,
    int Points,
    IReadOnlyCollection<AnswerOptionEditorResponse> AnswerOptions)
{
    public static QuestionEditorResponse FromEntity(Question question)
    {
        return new QuestionEditorResponse(
            question.Id,
            question.Text,
            question.ImageUrl,
            question.Type.ToString(),
            question.Position,
            question.TimeLimitSeconds,
            question.Points,
            question.AnswerOptions
                .OrderBy(answerOption => answerOption.Position)
                .Select(AnswerOptionEditorResponse.FromEntity)
                .ToArray());
    }
}
