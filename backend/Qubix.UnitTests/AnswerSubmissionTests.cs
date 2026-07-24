using Qubix.Core.Common;
using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.UnitTests;

public sealed class AnswerSubmissionTests
{
    [Fact]
    public void Constructor_ValidSelection_CapturesAnswerAndResponseTime()
    {
        var (_, question, participant) = DomainTestFactory.CreateRunningSession();
        var selectedOptionId = question.AnswerOptions.First().Id;

        var submission = new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            [selectedOptionId],
            DomainTestFactory.Now.AddSeconds(7));

        Assert.Equal(participant.Id, submission.ParticipantId);
        Assert.Equal(question.Id, submission.SessionQuestionId);
        Assert.Equal(5_000, submission.ResponseTimeMilliseconds);
        Assert.Equal(selectedOptionId, submission.SelectedOptions.Single().SessionAnswerOptionId);
    }

    [Fact]
    public void Constructor_AfterDeadline_RejectsAnswer()
    {
        var (_, question, participant) = DomainTestFactory.CreateRunningSession();
        var selectedOptionId = question.AnswerOptions.First().Id;

        var action = () => new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            [selectedOptionId],
            question.ClosesAtUtc!.Value.AddMilliseconds(1));

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Constructor_SingleChoiceWithMultipleOptions_RejectsAnswer()
    {
        var (_, question, participant) = DomainTestFactory.CreateRunningSession();

        var action = () => new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            question.AnswerOptions.Select(option => option.Id),
            DomainTestFactory.Now.AddSeconds(5));

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Constructor_OptionFromAnotherQuestion_RejectsAnswer()
    {
        var (session, question, participant) = DomainTestFactory.CreateRunningSession();
        var otherQuestion = DomainTestFactory.CreateReadySessionQuestion(
            session.Id,
            QuestionType.SingleChoice,
            position: 1);
        var foreignOptionId = otherQuestion.AnswerOptions.First().Id;

        var action = () => new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            [foreignOptionId],
            DomainTestFactory.Now.AddSeconds(5));

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Grade_CanOnlyBePerformedOnce()
    {
        var (_, question, participant) = DomainTestFactory.CreateRunningSession();
        var submission = new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            [question.AnswerOptions.First().Id],
            DomainTestFactory.Now.AddSeconds(5));

        submission.Grade(isCorrect: true, awardedPoints: 900);

        Assert.True(submission.IsCorrect);
        Assert.Equal(900, submission.AwardedPoints);
        Assert.Throws<DomainException>(
            () => submission.Grade(isCorrect: true, awardedPoints: 900));
    }

    [Fact]
    public void Grade_IncorrectAnswerCannotReceivePoints()
    {
        var (_, question, participant) = DomainTestFactory.CreateRunningSession();
        var submission = new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            [question.AnswerOptions.Last().Id],
            DomainTestFactory.Now.AddSeconds(5));

        Assert.Throws<ArgumentException>(
            () => submission.Grade(isCorrect: false, awardedPoints: 1));
    }
}
