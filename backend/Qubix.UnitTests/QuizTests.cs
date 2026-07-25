using Qubix.Core.Common;
using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.UnitTests;

public sealed class QuizTests
{
    [Fact]
    public void Question_RequiresTextOrImage()
    {
        var action = () => new Question(
            Guid.NewGuid(),
            Guid.NewGuid(),
            QuestionType.SingleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 1_000);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Publish_RequiresAtLeastOneQuestion()
    {
        var quiz = CreateQuiz();

        var action = () => quiz.Publish(DomainTestFactory.Now.AddMinutes(1));

        Assert.Throws<DomainException>(action);
        Assert.Equal(QuizStatus.Draft, quiz.Status);
    }

    [Fact]
    public void Publish_RejectsSingleChoiceQuestionWithMultipleCorrectAnswers()
    {
        var quiz = CreateQuiz();
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            QuestionType.SingleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 1_000,
            text: "Question");
        question.AddAnswerOption(
            new AnswerOption(Guid.NewGuid(), question.Id, "First", true, 0));
        question.AddAnswerOption(
            new AnswerOption(Guid.NewGuid(), question.Id, "Second", true, 1));
        quiz.AddQuestion(question, DomainTestFactory.Now.AddSeconds(1));

        var action = () => quiz.Publish(DomainTestFactory.Now.AddMinutes(1));

        Assert.Throws<DomainException>(action);
        Assert.Equal(QuizStatus.Draft, quiz.Status);
    }

    [Fact]
    public void Publish_MultipleChoiceQuestionWithOneCorrectAnswer_Succeeds()
    {
        var quiz = CreateQuiz();
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            QuestionType.MultipleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 1_000,
            text: "Question");
        question.AddAnswerOption(
            new AnswerOption(Guid.NewGuid(), question.Id, "Correct", true, 0));
        question.AddAnswerOption(
            new AnswerOption(Guid.NewGuid(), question.Id, "Incorrect", false, 1));
        quiz.AddQuestion(question, DomainTestFactory.Now.AddSeconds(1));

        quiz.Publish(DomainTestFactory.Now.AddMinutes(1));

        Assert.Equal(QuizStatus.Published, quiz.Status);
    }

    [Fact]
    public void Publish_ValidQuiz_ChangesStatusAndPreventsFurtherModification()
    {
        var quiz = CreateQuiz();
        var question = DomainTestFactory.CreateReadyQuestion(quiz.Id);
        quiz.AddQuestion(question, DomainTestFactory.Now.AddSeconds(1));

        quiz.Publish(DomainTestFactory.Now.AddMinutes(1));

        Assert.Equal(QuizStatus.Published, quiz.Status);
        Assert.Throws<DomainException>(
            () => quiz.RemoveQuestion(question.Id, DomainTestFactory.Now.AddMinutes(2)));
    }

    [Fact]
    public void AddQuestion_RejectsDuplicatePosition()
    {
        var quiz = CreateQuiz();
        quiz.AddQuestion(
            DomainTestFactory.CreateReadyQuestion(quiz.Id),
            DomainTestFactory.Now.AddSeconds(1));

        var duplicatePosition = DomainTestFactory.CreateReadyQuestion(quiz.Id);

        Assert.Throws<DomainException>(
            () => quiz.AddQuestion(
                duplicatePosition,
                DomainTestFactory.Now.AddSeconds(2)));
    }

    private static Quiz CreateQuiz()
    {
        return new Quiz(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Quiz",
            DomainTestFactory.Now);
    }
}
