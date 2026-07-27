using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubix.Api.Authorization;
using Qubix.Api.Contracts.Quizzes;
using Qubix.Core.Authorization;
using Qubix.Core.Entities;
using Qubix.Core.Exceptions;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Organizer)]
[Route("api/quizzes")]
public sealed class QuizzesController(
    AppDbContext dbContext,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<QuizResponse>> Create(
        CreateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = new Quiz(
            Guid.NewGuid(),
            User.GetRequiredUserId(),
            request.Title,
            timeProvider.GetUtcNow(),
            request.Description,
            request.Category,
            request.Rules,
            request.DefaultQuestionTimeSeconds);

        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = QuizResponse.FromEntity(quiz);

        return CreatedAtAction(
            nameof(GetById),
            new { id = quiz.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<QuizResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<QuizResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var quizzes = await dbContext.Quizzes
            .AsNoTracking()
            .Where(quiz => quiz.OwnerId == ownerId)
            .OrderByDescending(quiz => quiz.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return Ok(quizzes.Select(QuizResponse.FromEntity).ToArray());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quiz = await FindQuizAsync(id, includeQuestions: false, cancellationToken);
        User.EnsureOwner(quiz.OwnerId);

        return Ok(QuizResponse.FromEntity(quiz));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizResponse>> Update(
        Guid id,
        UpdateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await FindQuizAsync(id, includeQuestions: false, cancellationToken);
        User.EnsureOwner(quiz.OwnerId);

        quiz.UpdateDetails(
            request.Title,
            request.Description,
            request.Category,
            request.Rules,
            request.DefaultQuestionTimeSeconds,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(QuizResponse.FromEntity(quiz));
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizResponse>> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quiz = await FindQuizAsync(id, includeQuestions: true, cancellationToken);
        User.EnsureOwner(quiz.OwnerId);

        quiz.Publish(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(QuizResponse.FromEntity(quiz));
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizResponse>> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quiz = await FindQuizAsync(id, includeQuestions: false, cancellationToken);
        User.EnsureOwner(quiz.OwnerId);

        quiz.Archive(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(QuizResponse.FromEntity(quiz));
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<QuizResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizResponse>> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quiz = await FindQuizAsync(id, includeQuestions: false, cancellationToken);
        User.EnsureOwner(quiz.OwnerId);

        quiz.RestoreToDraft(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(QuizResponse.FromEntity(quiz));
    }

    private async Task<Quiz> FindQuizAsync(
        Guid id,
        bool includeQuestions,
        CancellationToken cancellationToken)
    {
        IQueryable<Quiz> query = dbContext.Quizzes;

        if (includeQuestions)
        {
            query = query
                .Include(quiz => quiz.Questions)
                .ThenInclude(question => question.AnswerOptions);
        }

        return await query.SingleOrDefaultAsync(
                quiz => quiz.Id == id,
                cancellationToken)
            ?? throw new NotFoundException("Quiz was not found.");
    }
}
