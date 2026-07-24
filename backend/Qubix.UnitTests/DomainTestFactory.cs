using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.UnitTests;

internal static class DomainTestFactory
{
    internal static readonly DateTimeOffset Now =
        new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    internal static Question CreateReadyQuestion(
        Guid quizId,
        QuestionType type = QuestionType.SingleChoice)
    {
        var question = new Question(
            Guid.NewGuid(),
            quizId,
            type,
            position: 0,
            timeLimitSeconds: 30,
            points: 1_000,
            text: "Question");

        question.AddAnswerOption(
            new AnswerOption(Guid.NewGuid(), question.Id, "Correct", true, 0));
        question.AddAnswerOption(
            new AnswerOption(
                Guid.NewGuid(),
                question.Id,
                "Second",
                type == QuestionType.MultipleChoice,
                1));

        return question;
    }

    internal static SessionQuestion CreateReadySessionQuestion(
        Guid sessionId,
        QuestionType type = QuestionType.SingleChoice,
        int position = 0)
    {
        var question = new SessionQuestion(
            Guid.NewGuid(),
            sessionId,
            Guid.NewGuid(),
            type,
            position,
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
                "Second",
                type == QuestionType.MultipleChoice,
                1));

        return question;
    }

    internal static (
        QuizSession Session,
        SessionQuestion Question,
        Participant Participant) CreateRunningSession(
        QuestionType type = QuestionType.SingleChoice)
    {
        var session = new QuizSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ABC123",
            Now);
        var question = CreateReadySessionQuestion(session.Id, type);
        var participant = new Participant(
            Guid.NewGuid(),
            session.Id,
            Guid.NewGuid(),
            "Player",
            Now);

        session.AddQuestion(question);
        session.AddParticipant(participant);
        session.Start(Now.AddSeconds(1));
        session.OpenQuestion(question.Id, Now.AddSeconds(2));

        return (session, question, participant);
    }
}
