using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubix.Api.Authorization;
using Qubix.Api.Contracts.Questions;
using Qubix.Core.Authorization;
using Qubix.Core.Entities;
using Qubix.Core.Exceptions;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Organizer)]
[Route("api/quizzes/{quizId:guid}/questions")]
public sealed class QuizQuestionsController(
    AppDbContext dbContext,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<QuestionEditorResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<QuestionEditorResponse>>>
        GetQuestions(
            Guid quizId,
            CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var questions = quiz.Questions
            .OrderBy(question => question.Position)
            .Select(QuestionEditorResponse.FromEntity)
            .ToArray();

        return Ok(questions);
    }

    [HttpPost]
    [ProducesResponseType<QuestionEditorResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionEditorResponse>> CreateQuestion(
        Guid quizId,
        CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            request.Type,
            request.Position,
            request.TimeLimitSeconds,
            request.Points,
            request.Text,
            request.ImageUrl);

        quiz.AddQuestion(question, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            QuestionEditorResponse.FromEntity(question));
    }

    [HttpPut("{questionId:guid}")]
    [ProducesResponseType<QuestionEditorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuestionEditorResponse>> UpdateQuestion(
        Guid quizId,
        Guid questionId,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var question = FindQuestion(quiz, questionId);

        if (quiz.Questions.Any(existing =>
                existing.Id != question.Id &&
                existing.Position == request.Position))
        {
            throw new ConflictException(
                "Question positions must be unique within a quiz.");
        }

        quiz.MarkContentUpdated(timeProvider.GetUtcNow());
        question.UpdateContent(request.Text, request.ImageUrl);
        question.UpdateSettings(
            request.Type,
            request.Position,
            request.TimeLimitSeconds,
            request.Points);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(QuestionEditorResponse.FromEntity(question));
    }

    [HttpDelete("{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteQuestion(
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        quiz.RemoveQuestion(questionId, timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{questionId:guid}/options")]
    [ProducesResponseType<AnswerOptionEditorResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnswerOptionEditorResponse>>
        CreateAnswerOption(
            Guid quizId,
            Guid questionId,
            CreateAnswerOptionRequest request,
            CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var question = FindQuestion(quiz, questionId);
        var answerOption = new AnswerOption(
            Guid.NewGuid(),
            question.Id,
            request.Text,
            request.IsCorrect,
            request.Position);

        quiz.MarkContentUpdated(timeProvider.GetUtcNow());
        question.AddAnswerOption(answerOption);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            AnswerOptionEditorResponse.FromEntity(answerOption));
    }

    [HttpPut("{questionId:guid}/options/{answerOptionId:guid}")]
    [ProducesResponseType<AnswerOptionEditorResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnswerOptionEditorResponse>>
        UpdateAnswerOption(
            Guid quizId,
            Guid questionId,
            Guid answerOptionId,
            UpdateAnswerOptionRequest request,
            CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var question = FindQuestion(quiz, questionId);
        var answerOption = FindAnswerOption(question, answerOptionId);

        if (question.AnswerOptions.Any(existing =>
                existing.Id != answerOption.Id &&
                existing.Position == request.Position))
        {
            throw new ConflictException(
                "Answer option positions must be unique within a question.");
        }

        quiz.MarkContentUpdated(timeProvider.GetUtcNow());
        answerOption.Update(
            request.Text,
            request.IsCorrect,
            request.Position);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(AnswerOptionEditorResponse.FromEntity(answerOption));
    }

    [HttpDelete("{questionId:guid}/options/{answerOptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAnswerOption(
        Guid quizId,
        Guid questionId,
        Guid answerOptionId,
        CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(quizId, cancellationToken);
        var question = FindQuestion(quiz, questionId);

        quiz.MarkContentUpdated(timeProvider.GetUtcNow());
        question.RemoveAnswerOption(answerOptionId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<Quiz> FindOwnedQuizAsync(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(existing => existing.Questions)
            .ThenInclude(question => question.AnswerOptions)
            .SingleOrDefaultAsync(
                existing => existing.Id == quizId,
                cancellationToken)
            ?? throw new NotFoundException("Quiz was not found.");

        User.EnsureOwner(quiz.OwnerId);

        return quiz;
    }

    private static Question FindQuestion(Quiz quiz, Guid questionId)
    {
        return quiz.Questions.SingleOrDefault(
                question => question.Id == questionId)
            ?? throw new NotFoundException("Question was not found.");
    }

    private static AnswerOption FindAnswerOption(
        Question question,
        Guid answerOptionId)
    {
        return question.AnswerOptions.SingleOrDefault(
                answerOption => answerOption.Id == answerOptionId)
            ?? throw new NotFoundException("Answer option was not found.");
    }
}
