using Qubix.Core.Common;
using Qubix.Core.Enums;

namespace Qubix.Core.Entities;

public sealed class AnswerSubmission : Entity
{
    private readonly List<AnswerSubmissionOption> _selectedOptions = [];

    private AnswerSubmission()
    {
    }

    public AnswerSubmission(
        Guid id,
        Participant participant,
        SessionQuestion question,
        IEnumerable<Guid> selectedOptionIds,
        DateTimeOffset submittedAtUtc)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(selectedOptionIds);

        if (participant.QuizSessionId != question.QuizSessionId)
        {
            throw new DomainException(
                "The participant and question belong to different quiz sessions.");
        }

        var timestamp = Guard.Utc(submittedAtUtc, nameof(submittedAtUtc));

        if (!question.AcceptsAnswersAt(timestamp))
        {
            throw new DomainException("The question is not accepting answers.");
        }

        var selectedIds = selectedOptionIds.Distinct().ToArray();

        if (selectedIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one answer option must be selected.",
                nameof(selectedOptionIds));
        }

        if (question.Type == QuestionType.SingleChoice && selectedIds.Length != 1)
        {
            throw new DomainException(
                "Exactly one option must be selected for a single-choice question.");
        }

        var availableOptionIds = question.AnswerOptions
            .Select(option => option.Id)
            .ToHashSet();

        if (selectedIds.Any(selectedId => !availableOptionIds.Contains(selectedId)))
        {
            throw new DomainException(
                "At least one selected option does not belong to the session question.");
        }

        ParticipantId = participant.Id;
        SessionQuestionId = question.Id;
        SubmittedAtUtc = timestamp;
        ResponseTimeMilliseconds = checked(
            (long)(timestamp - question.OpensAtUtc!.Value).TotalMilliseconds);

        _selectedOptions.AddRange(
            selectedIds.Select(selectedId => new AnswerSubmissionOption(Id, selectedId)));
    }

    public Guid ParticipantId { get; private set; }

    public Guid SessionQuestionId { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public long ResponseTimeMilliseconds { get; private set; }

    public bool? IsCorrect { get; private set; }

    public int AwardedPoints { get; private set; }

    public bool IsGraded => IsCorrect.HasValue;

    public IReadOnlyCollection<AnswerSubmissionOption> SelectedOptions =>
        _selectedOptions.AsReadOnly();

    public void Grade(bool isCorrect, int awardedPoints)
    {
        if (IsGraded)
        {
            throw new DomainException("The answer submission has already been graded.");
        }

        Guard.NonNegative(awardedPoints, nameof(awardedPoints));

        if (!isCorrect && awardedPoints != 0)
        {
            throw new ArgumentException(
                "An incorrect answer cannot receive points.",
                nameof(awardedPoints));
        }

        IsCorrect = isCorrect;
        AwardedPoints = awardedPoints;
    }
}
