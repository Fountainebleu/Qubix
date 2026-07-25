using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Questions;

public sealed record AnswerOptionEditorResponse(
    Guid Id,
    string Text,
    bool IsCorrect,
    int Position)
{
    public static AnswerOptionEditorResponse FromEntity(
        AnswerOption answerOption)
    {
        return new AnswerOptionEditorResponse(
            answerOption.Id,
            answerOption.Text,
            answerOption.IsCorrect,
            answerOption.Position);
    }
}
