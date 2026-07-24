using Qubix.Core.Common;
using Qubix.Core.Enums;

namespace Qubix.Core.Entities;

public sealed class Quiz : Entity
{
    public const int MaxTitleLength = 150;
    public const int MaxDescriptionLength = 1_000;
    public const int MaxCategoryLength = 100;
    public const int MaxRulesLength = 2_000;
    public const int MinimumQuestionTimeSeconds = 5;
    public const int MaximumQuestionTimeSeconds = 300;

    private readonly List<Question> _questions = [];

    private Quiz()
    {
    }

    public Quiz(
        Guid id,
        Guid ownerId,
        string title,
        DateTimeOffset createdAtUtc,
        string? description = null,
        string? category = null,
        string? rules = null,
        int defaultQuestionTimeSeconds = 30)
        : base(id)
    {
        OwnerId = Guard.NotEmpty(ownerId, nameof(ownerId));
        Title = Guard.Required(title, nameof(title), MaxTitleLength);
        Description = Guard.Optional(description, nameof(description), MaxDescriptionLength);
        Category = Guard.Optional(category, nameof(category), MaxCategoryLength);
        Rules = Guard.Optional(rules, nameof(rules), MaxRulesLength);
        DefaultQuestionTimeSeconds = Guard.InRange(
            defaultQuestionTimeSeconds,
            MinimumQuestionTimeSeconds,
            MaximumQuestionTimeSeconds,
            nameof(defaultQuestionTimeSeconds));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Status = QuizStatus.Draft;
    }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? Category { get; private set; }

    public string? Rules { get; private set; }

    public int DefaultQuestionTimeSeconds { get; private set; }

    public QuizStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public void UpdateDetails(
        string title,
        string? description,
        string? category,
        string? rules,
        int defaultQuestionTimeSeconds,
        DateTimeOffset updatedAtUtc)
    {
        EnsureDraft();

        var timestamp = Guard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(updatedAtUtc));

        var normalizedTitle = Guard.Required(title, nameof(title), MaxTitleLength);
        var normalizedDescription = Guard.Optional(
            description,
            nameof(description),
            MaxDescriptionLength);
        var normalizedCategory = Guard.Optional(category, nameof(category), MaxCategoryLength);
        var normalizedRules = Guard.Optional(rules, nameof(rules), MaxRulesLength);
        var normalizedQuestionTime = Guard.InRange(
            defaultQuestionTimeSeconds,
            MinimumQuestionTimeSeconds,
            MaximumQuestionTimeSeconds,
            nameof(defaultQuestionTimeSeconds));

        Title = normalizedTitle;
        Description = normalizedDescription;
        Category = normalizedCategory;
        Rules = normalizedRules;
        DefaultQuestionTimeSeconds = normalizedQuestionTime;
        UpdatedAtUtc = timestamp;
    }

    public void AddQuestion(Question question, DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(question);
        EnsureDraft();

        if (question.QuizId != Id)
        {
            throw new DomainException("The question belongs to a different quiz.");
        }

        if (_questions.Any(existing => existing.Id == question.Id))
        {
            throw new DomainException("The question has already been added to the quiz.");
        }

        if (_questions.Any(existing => existing.Position == question.Position))
        {
            throw new DomainException("Question positions must be unique within a quiz.");
        }

        var timestamp = Guard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(updatedAtUtc));

        _questions.Add(question);
        UpdatedAtUtc = timestamp;
    }

    public void RemoveQuestion(Guid questionId, DateTimeOffset updatedAtUtc)
    {
        EnsureDraft();
        Guard.NotEmpty(questionId, nameof(questionId));

        var question = _questions.SingleOrDefault(existing => existing.Id == questionId)
            ?? throw new DomainException("The question was not found in the quiz.");
        var timestamp = Guard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(updatedAtUtc));

        _questions.Remove(question);
        UpdatedAtUtc = timestamp;
    }

    public void Publish(DateTimeOffset publishedAtUtc)
    {
        EnsureDraft();

        if (_questions.Count == 0)
        {
            throw new DomainException("A quiz must contain at least one question before publication.");
        }

        foreach (var question in _questions)
        {
            question.EnsureReadyForPublication();
        }

        var timestamp = Guard.Utc(publishedAtUtc, nameof(publishedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(publishedAtUtc));

        Status = QuizStatus.Published;
        UpdatedAtUtc = timestamp;
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        if (Status == QuizStatus.Archived)
        {
            return;
        }

        var timestamp = Guard.Utc(archivedAtUtc, nameof(archivedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(archivedAtUtc));

        Status = QuizStatus.Archived;
        UpdatedAtUtc = timestamp;
    }

    private void EnsureDraft()
    {
        if (Status != QuizStatus.Draft)
        {
            throw new DomainException("Only a draft quiz can be modified.");
        }
    }

    private void EnsureNotBeforeCreation(DateTimeOffset timestamp, string parameterName)
    {
        if (timestamp < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "Timestamp cannot be earlier than the quiz creation time.");
        }
    }
}
