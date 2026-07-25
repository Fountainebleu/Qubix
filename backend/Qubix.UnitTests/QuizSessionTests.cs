using Qubix.Core.Common;
using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.UnitTests;

public sealed class QuizSessionTests
{
    [Fact]
    public void Constructor_NormalizesRoomCode()
    {
        var session = new QuizSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ab12cd",
            DomainTestFactory.Now);

        Assert.Equal("AB12CD", session.RoomCode);
        Assert.Equal(QuizSessionStatus.Waiting, session.Status);
    }

    [Fact]
    public void AddParticipant_RejectsDuplicateUser()
    {
        var session = CreateSession();
        var userId = Guid.NewGuid();
        session.AddParticipant(
            new Participant(
                Guid.NewGuid(),
                session.Id,
                userId,
                "First",
                DomainTestFactory.Now));

        var duplicate = new Participant(
            Guid.NewGuid(),
            session.Id,
            userId,
            "Second",
            DomainTestFactory.Now);

        Assert.Throws<DomainException>(() => session.AddParticipant(duplicate));
    }

    [Fact]
    public void Start_RequiresAtLeastOneQuestion()
    {
        var session = CreateSession();

        Assert.Throws<DomainException>(
            () => session.Start(DomainTestFactory.Now.AddSeconds(1)));
    }

    [Fact]
    public void OpenAndCloseQuestion_PerformsValidStateTransitions()
    {
        var session = CreateSession();
        var question = DomainTestFactory.CreateReadySessionQuestion(session.Id);
        session.AddQuestion(question);
        session.Start(DomainTestFactory.Now.AddSeconds(1));

        session.OpenQuestion(question.Id, DomainTestFactory.Now.AddSeconds(2));

        Assert.Equal(QuizSessionStatus.Running, session.Status);
        Assert.Equal(SessionQuestionStatus.Open, question.Status);
        Assert.Equal(
            DomainTestFactory.Now.AddSeconds(32),
            question.ClosesAtUtc);

        session.CloseCurrentQuestion(DomainTestFactory.Now.AddSeconds(10));

        Assert.Equal(SessionQuestionStatus.Closed, question.Status);
    }

    [Fact]
    public void OpenQuestion_RejectsSecondConcurrentQuestion()
    {
        var session = CreateSession();
        var first = DomainTestFactory.CreateReadySessionQuestion(session.Id, position: 0);
        var second = DomainTestFactory.CreateReadySessionQuestion(session.Id, position: 1);
        session.AddQuestion(first);
        session.AddQuestion(second);
        session.Start(DomainTestFactory.Now.AddSeconds(1));
        session.OpenQuestion(first.Id, DomainTestFactory.Now.AddSeconds(2));

        Assert.Throws<DomainException>(
            () => session.OpenQuestion(second.Id, DomainTestFactory.Now.AddSeconds(3)));
    }

    [Fact]
    public void Start_MultipleChoiceWithOneCorrectAnswer_Succeeds()
    {
        var session = CreateSession();
        var question = new SessionQuestion(
            Guid.NewGuid(),
            session.Id,
            Guid.NewGuid(),
            QuestionType.MultipleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 1_000,
            text: "Question");
        question.AddAnswerOption(
            new SessionAnswerOption(
                Guid.NewGuid(),
                question.Id,
                Guid.NewGuid(),
                "Correct",
                true,
                0));
        question.AddAnswerOption(
            new SessionAnswerOption(
                Guid.NewGuid(),
                question.Id,
                Guid.NewGuid(),
                "Incorrect",
                false,
                1));
        session.AddQuestion(question);

        session.Start(DomainTestFactory.Now.AddSeconds(1));

        Assert.Equal(QuizSessionStatus.Running, session.Status);
    }

    private static QuizSession CreateSession()
    {
        return new QuizSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ABC123",
            DomainTestFactory.Now);
    }
}
