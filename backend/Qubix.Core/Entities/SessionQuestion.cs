using Qubix.Core.Common;
using Qubix.Core.Enums;

namespace Qubix.Core.Entities;

public sealed class SessionQuestion : Entity
{
    private readonly List<SessionAnswerOption> _answerOptions = [];

    private SessionQuestion()
    {
    }

    public SessionQuestion(
        Guid id,
        Guid quizSessionId,
        Guid sourceQuestionId,
        QuestionType type,
        int position,
        int timeLimitSeconds,
        int points,
        string? text = null,
        string? imageUrl = null)
        : base(id)
    {
        QuizSessionId = Guard.NotEmpty(quizSessionId, nameof(quizSessionId));
        SourceQuestionId = Guard.NotEmpty(sourceQuestionId, nameof(sourceQuestionId));
        Type = Guard.DefinedEnum(type, nameof(type));
        Position = Guard.NonNegative(position, nameof(position));
        TimeLimitSeconds = Guard.InRange(
            timeLimitSeconds,
            Question.MinimumTimeLimitSeconds,
            Question.MaximumTimeLimitSeconds,
            nameof(timeLimitSeconds));
        Points = Guard.InRange(
            points,
            Question.MinimumPoints,
            Question.MaximumPoints,
            nameof(points));
        Status = SessionQuestionStatus.Pending;

        SetContent(text, imageUrl);
    }

    public Guid QuizSessionId { get; private set; }

    public Guid SourceQuestionId { get; private set; }

    public string? Text { get; private set; }

    public string? ImageUrl { get; private set; }

    public QuestionType Type { get; private set; }

    public int Position { get; private set; }

    public int TimeLimitSeconds { get; private set; }

    public int Points { get; private set; }

    public SessionQuestionStatus Status { get; private set; }

    public DateTimeOffset? OpensAtUtc { get; private set; }

    public DateTimeOffset? ClosesAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<SessionAnswerOption> AnswerOptions => _answerOptions.AsReadOnly();

    public void AddAnswerOption(SessionAnswerOption answerOption)
    {
        ArgumentNullException.ThrowIfNull(answerOption);

        if (Status != SessionQuestionStatus.Pending)
        {
            throw new DomainException("Answer options cannot be changed after a question is opened.");
        }

        if (answerOption.SessionQuestionId != Id)
        {
            throw new DomainException("The answer option belongs to a different session question.");
        }

        if (_answerOptions.Any(existing => existing.Id == answerOption.Id))
        {
            throw new DomainException("The answer option has already been added.");
        }

        if (_answerOptions.Any(existing => existing.Position == answerOption.Position))
        {
            throw new DomainException(
                "Answer option positions must be unique within a session question.");
        }

        _answerOptions.Add(answerOption);
    }

    public void Open(DateTimeOffset opensAtUtc)
    {
        if (Status != SessionQuestionStatus.Pending)
        {
            throw new DomainException("Only a pending question can be opened.");
        }

        EnsureReadyForOpening();

        var timestamp = Guard.Utc(opensAtUtc, nameof(opensAtUtc));
        OpensAtUtc = timestamp;
        ClosesAtUtc = timestamp.AddSeconds(TimeLimitSeconds);
        Status = SessionQuestionStatus.Open;
    }

    public void Close(DateTimeOffset closedAtUtc)
    {
        if (Status != SessionQuestionStatus.Open || OpensAtUtc is null)
        {
            throw new DomainException("Only an open question can be closed.");
        }

        var timestamp = Guard.Utc(closedAtUtc, nameof(closedAtUtc));

        if (timestamp < OpensAtUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(closedAtUtc),
                timestamp,
                "A question cannot be closed before it is opened.");
        }

        ClosedAtUtc = timestamp;
        Status = SessionQuestionStatus.Closed;
    }

    public bool AcceptsAnswersAt(DateTimeOffset submittedAtUtc)
    {
        var timestamp = Guard.Utc(submittedAtUtc, nameof(submittedAtUtc));

        return Status == SessionQuestionStatus.Open
            && OpensAtUtc is not null
            && ClosesAtUtc is not null
            && timestamp >= OpensAtUtc.Value
            && timestamp <= ClosesAtUtc.Value;
    }

    internal void EnsureReadyForOpening()
    {
        if (_answerOptions.Count < 2)
        {
            throw new DomainException(
                "A session question must contain at least two answer options.");
        }

        var correctAnswersCount = _answerOptions.Count(option => option.IsCorrect);

        if (Type == QuestionType.SingleChoice && correctAnswersCount != 1)
        {
            throw new DomainException(
                "A single-choice session question must contain exactly one correct answer.");
        }

        if (Type == QuestionType.MultipleChoice && correctAnswersCount < 1)
        {
            throw new DomainException(
                "A multiple-choice session question must contain at least one correct answer.");
        }
    }

    private void SetContent(string? text, string? imageUrl)
    {
        var normalizedText = Guard.Optional(text, nameof(text), Question.MaxTextLength);
        var normalizedImageUrl = Guard.Optional(
            imageUrl,
            nameof(imageUrl),
            Question.MaxImageUrlLength);

        if (normalizedText is null && normalizedImageUrl is null)
        {
            throw new ArgumentException(
                "A session question must contain text, an image, or both.");
        }

        Text = normalizedText;
        ImageUrl = normalizedImageUrl;
    }
}
