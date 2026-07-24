using Qubix.Core.Common;

namespace Qubix.Core.Entities;

public sealed class AnswerOption : Entity
{
    public const int MaxTextLength = 500;

    private AnswerOption()
    {
    }

    public AnswerOption(
        Guid id,
        Guid questionId,
        string text,
        bool isCorrect,
        int position)
        : base(id)
    {
        QuestionId = Guard.NotEmpty(questionId, nameof(questionId));
        Text = Guard.Required(text, nameof(text), MaxTextLength);
        IsCorrect = isCorrect;
        Position = Guard.NonNegative(position, nameof(position));
    }

    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int Position { get; private set; }

    public void Update(string text, bool isCorrect, int position)
    {
        var normalizedText = Guard.Required(text, nameof(text), MaxTextLength);
        var normalizedPosition = Guard.NonNegative(position, nameof(position));

        Text = normalizedText;
        IsCorrect = isCorrect;
        Position = normalizedPosition;
    }
}
