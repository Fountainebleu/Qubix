using Qubix.Core.Common;
using Qubix.Core.Enums;

namespace Qubix.Core.Entities;

public sealed class QuizSession : Entity
{
    public const int RoomCodeLength = 6;

    private readonly List<SessionQuestion> _questions = [];
    private readonly List<Participant> _participants = [];

    private QuizSession()
    {
    }

    public QuizSession(
        Guid id,
        Guid quizId,
        Guid organizerId,
        string roomCode,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        QuizId = Guard.NotEmpty(quizId, nameof(quizId));
        OrganizerId = Guard.NotEmpty(organizerId, nameof(organizerId));
        RoomCode = NormalizeRoomCode(roomCode);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        Status = QuizSessionStatus.Waiting;
    }

    public Guid QuizId { get; private set; }

    public Quiz Quiz { get; private set; } = null!;

    public Guid OrganizerId { get; private set; }

    public string RoomCode { get; private set; } = string.Empty;

    public QuizSessionStatus Status { get; private set; }

    public Guid? CurrentQuestionId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? FinishedAtUtc { get; private set; }

    public IReadOnlyCollection<SessionQuestion> Questions => _questions.AsReadOnly();

    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();

    public static QuizSession CreateForQuiz(
        Guid id,
        Quiz quiz,
        Guid organizerId,
        string roomCode,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(quiz);

        return new QuizSession(
            id,
            quiz.Id,
            organizerId,
            roomCode,
            createdAtUtc)
        {
            Quiz = quiz
        };
    }

    public void AddQuestion(SessionQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        EnsureWaiting();

        if (question.QuizSessionId != Id)
        {
            throw new DomainException("The question belongs to a different quiz session.");
        }

        if (_questions.Any(existing => existing.Id == question.Id))
        {
            throw new DomainException("The question has already been added to the session.");
        }

        if (_questions.Any(existing => existing.Position == question.Position))
        {
            throw new DomainException(
                "Question positions must be unique within a quiz session.");
        }

        _questions.Add(question);
    }

    public void AddParticipant(Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        EnsureWaiting();

        if (participant.QuizSessionId != Id)
        {
            throw new DomainException("The participant belongs to a different quiz session.");
        }

        if (_participants.Any(existing => existing.Id == participant.Id))
        {
            throw new DomainException("The participant has already joined the session.");
        }

        if (_participants.Any(existing => existing.UserId == participant.UserId))
        {
            throw new DomainException("The user has already joined the session.");
        }

        _participants.Add(participant);
    }

    public void RemoveParticipant(Guid participantId)
    {
        EnsureWaiting();
        Guard.NotEmpty(participantId, nameof(participantId));

        var participant = _participants.SingleOrDefault(existing => existing.Id == participantId)
            ?? throw new DomainException("The participant was not found in the session.");

        _participants.Remove(participant);
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        EnsureWaiting();

        if (_questions.Count == 0)
        {
            throw new DomainException("A quiz session cannot start without questions.");
        }

        foreach (var question in _questions)
        {
            question.EnsureReadyForOpening();
        }

        var timestamp = Guard.Utc(startedAtUtc, nameof(startedAtUtc));
        EnsureNotBeforeCreation(timestamp, nameof(startedAtUtc));

        StartedAtUtc = timestamp;
        Status = QuizSessionStatus.Running;
    }

    public void OpenQuestion(Guid questionId, DateTimeOffset opensAtUtc)
    {
        EnsureRunning();
        Guard.NotEmpty(questionId, nameof(questionId));

        if (_questions.Any(question => question.Status == SessionQuestionStatus.Open))
        {
            throw new DomainException("Another question is already open.");
        }

        var question = _questions.SingleOrDefault(existing => existing.Id == questionId)
            ?? throw new DomainException("The question was not found in the session.");

        question.Open(opensAtUtc);
        CurrentQuestionId = question.Id;
    }

    public void CloseCurrentQuestion(DateTimeOffset closedAtUtc)
    {
        EnsureRunning();

        var currentQuestion = CurrentQuestionId is null
            ? null
            : _questions.SingleOrDefault(question => question.Id == CurrentQuestionId.Value);

        if (currentQuestion is null || currentQuestion.Status != SessionQuestionStatus.Open)
        {
            throw new DomainException("There is no open current question.");
        }

        currentQuestion.Close(closedAtUtc);
    }

    public void Finish(DateTimeOffset finishedAtUtc)
    {
        EnsureRunning();

        var timestamp = Guard.Utc(finishedAtUtc, nameof(finishedAtUtc));

        if (StartedAtUtc is not null && timestamp < StartedAtUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finishedAtUtc),
                timestamp,
                "A quiz session cannot finish before it starts.");
        }

        var openQuestion = _questions.SingleOrDefault(
            question => question.Status == SessionQuestionStatus.Open);
        openQuestion?.Close(timestamp);

        FinishedAtUtc = timestamp;
        Status = QuizSessionStatus.Finished;
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var normalized = Guard.Required(roomCode, nameof(roomCode), RoomCodeLength)
            .ToUpperInvariant();

        if (normalized.Length != RoomCodeLength || normalized.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException(
                $"Room code must contain exactly {RoomCodeLength} ASCII letters or digits.",
                nameof(roomCode));
        }

        return normalized;
    }

    private void EnsureWaiting()
    {
        if (Status != QuizSessionStatus.Waiting)
        {
            throw new DomainException("The quiz session is not waiting.");
        }
    }

    private void EnsureRunning()
    {
        if (Status != QuizSessionStatus.Running)
        {
            throw new DomainException("The quiz session is not running.");
        }
    }

    private void EnsureNotBeforeCreation(DateTimeOffset timestamp, string parameterName)
    {
        if (timestamp < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "Timestamp cannot be earlier than the session creation time.");
        }
    }
}
