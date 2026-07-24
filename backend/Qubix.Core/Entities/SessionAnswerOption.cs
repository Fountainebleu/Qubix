using Qubix.Core.Common;

namespace Qubix.Core.Entities;

public sealed class SessionAnswerOption : Entity
{
    private SessionAnswerOption()
    {
    }

    public SessionAnswerOption(
        Guid id,
        Guid sessionQuestionId,
        Guid sourceAnswerOptionId,
        string text,
        bool isCorrect,
        int position)
        : base(id)
    {
        SessionQuestionId = Guard.NotEmpty(sessionQuestionId, nameof(sessionQuestionId));
        SourceAnswerOptionId = Guard.NotEmpty(sourceAnswerOptionId, nameof(sourceAnswerOptionId));
        Text = Guard.Required(text, nameof(text), AnswerOption.MaxTextLength);
        IsCorrect = isCorrect;
        Position = Guard.NonNegative(position, nameof(position));
    }

    public Guid SessionQuestionId { get; private set; }

    public Guid SourceAnswerOptionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int Position { get; private set; }
}
