using Qubix.Core.Common;
using Qubix.Core.Enums;

namespace Qubix.Core.Entities;

public sealed class Question : Entity
{
    public const int MaxTextLength = 2_000;
    public const int MaxImageUrlLength = 2_048;
    public const int MinimumTimeLimitSeconds = 5;
    public const int MaximumTimeLimitSeconds = 300;
    public const int MinimumPoints = 1;
    public const int MaximumPoints = 10_000;

    private readonly List<AnswerOption> _answerOptions = [];

    private Question()
    {
    }

    public Question(
        Guid id,
        Guid quizId,
        QuestionType type,
        int position,
        int timeLimitSeconds,
        int points,
        string? text = null,
        string? imageUrl = null)
        : base(id)
    {
        QuizId = Guard.NotEmpty(quizId, nameof(quizId));
        Type = Guard.DefinedEnum(type, nameof(type));
        Position = Guard.NonNegative(position, nameof(position));
        TimeLimitSeconds = Guard.InRange(
            timeLimitSeconds,
            MinimumTimeLimitSeconds,
            MaximumTimeLimitSeconds,
            nameof(timeLimitSeconds));
        Points = Guard.InRange(points, MinimumPoints, MaximumPoints, nameof(points));

        SetContent(text, imageUrl);
    }

    public Guid QuizId { get; private set; }

    public string? Text { get; private set; }

    public string? ImageUrl { get; private set; }

    public QuestionType Type { get; private set; }

    public int Position { get; private set; }

    public int TimeLimitSeconds { get; private set; }

    public int Points { get; private set; }

    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();

    public void UpdateContent(string? text, string? imageUrl)
    {
        SetContent(text, imageUrl);
    }

    public void UpdateSettings(
        QuestionType type,
        int position,
        int timeLimitSeconds,
        int points)
    {
        var normalizedType = Guard.DefinedEnum(type, nameof(type));
        var normalizedPosition = Guard.NonNegative(position, nameof(position));
        var normalizedTimeLimit = Guard.InRange(
            timeLimitSeconds,
            MinimumTimeLimitSeconds,
            MaximumTimeLimitSeconds,
            nameof(timeLimitSeconds));
        var normalizedPoints = Guard.InRange(
            points,
            MinimumPoints,
            MaximumPoints,
            nameof(points));

        Type = normalizedType;
        Position = normalizedPosition;
        TimeLimitSeconds = normalizedTimeLimit;
        Points = normalizedPoints;
    }

    public void AddAnswerOption(AnswerOption answerOption)
    {
        ArgumentNullException.ThrowIfNull(answerOption);

        if (answerOption.QuestionId != Id)
        {
            throw new DomainException("The answer option belongs to a different question.");
        }

        if (_answerOptions.Any(existing => existing.Id == answerOption.Id))
        {
            throw new DomainException("The answer option has already been added.");
        }

        if (_answerOptions.Any(existing => existing.Position == answerOption.Position))
        {
            throw new DomainException("Answer option positions must be unique within a question.");
        }

        _answerOptions.Add(answerOption);
    }

    public void RemoveAnswerOption(Guid answerOptionId)
    {
        Guard.NotEmpty(answerOptionId, nameof(answerOptionId));

        var answerOption = _answerOptions.SingleOrDefault(existing => existing.Id == answerOptionId)
            ?? throw new DomainException("The answer option was not found.");

        _answerOptions.Remove(answerOption);
    }

    internal void EnsureReadyForPublication()
    {
        if (_answerOptions.Count < 2)
        {
            throw new DomainException("A question must contain at least two answer options.");
        }

        var correctAnswersCount = _answerOptions.Count(option => option.IsCorrect);

        if (Type == QuestionType.SingleChoice && correctAnswersCount != 1)
        {
            throw new DomainException(
                "A single-choice question must contain exactly one correct answer.");
        }

        if (Type == QuestionType.MultipleChoice && correctAnswersCount < 1)
        {
            throw new DomainException(
                "A multiple-choice question must contain at least one correct answer.");
        }
    }

    private void SetContent(string? text, string? imageUrl)
    {
        var normalizedText = Guard.Optional(text, nameof(text), MaxTextLength);
        var normalizedImageUrl = Guard.Optional(imageUrl, nameof(imageUrl), MaxImageUrlLength);

        if (normalizedText is null && normalizedImageUrl is null)
        {
            throw new ArgumentException("A question must contain text, an image, or both.");
        }

        Text = normalizedText;
        ImageUrl = normalizedImageUrl;
    }
}
