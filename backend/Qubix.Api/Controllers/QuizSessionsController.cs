using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Qubix.Api.Authorization;
using Qubix.Api.Contracts.Sessions;
using Qubix.Api.RealTime;
using Qubix.Core.Authorization;
using Qubix.Core.Entities;
using Qubix.Core.Enums;
using Qubix.Core.Exceptions;
using Qubix.Infrastructure.Identity;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class QuizSessionsController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider,
    IHubContext<QuizHub, IQuizClient> quizHubContext) : ControllerBase
{
    private const string RoomCodeAlphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaximumRoomCodeAttempts = 10;

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost]
    [ProducesResponseType<QuizSessionStateResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizSessionStateResponse>> Create(
        CreateQuizSessionRequest request,
        CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(existing => existing.Questions)
            .ThenInclude(question => question.AnswerOptions)
            .SingleOrDefaultAsync(
                existing => existing.Id == request.QuizId,
                cancellationToken)
            ?? throw new NotFoundException("Quiz was not found.");

        User.EnsureOwner(quiz.OwnerId);

        if (quiz.Status != QuizStatus.Published)
        {
            throw new ConflictException(
                "Only a published quiz can be used to create a room.");
        }

        var timestamp = timeProvider.GetUtcNow();
        var session = new QuizSession(
            Guid.NewGuid(),
            quiz.Id,
            User.GetRequiredUserId(),
            await GenerateRoomCodeAsync(cancellationToken),
            timestamp);

        foreach (var sourceQuestion in quiz.Questions.OrderBy(
                     question => question.Position))
        {
            var sessionQuestion = new SessionQuestion(
                Guid.NewGuid(),
                session.Id,
                sourceQuestion.Id,
                sourceQuestion.Type,
                sourceQuestion.Position,
                sourceQuestion.TimeLimitSeconds,
                sourceQuestion.Points,
                sourceQuestion.Text,
                sourceQuestion.ImageUrl);

            foreach (var sourceAnswerOption in sourceQuestion.AnswerOptions
                         .OrderBy(answerOption => answerOption.Position))
            {
                sessionQuestion.AddAnswerOption(
                    new SessionAnswerOption(
                        Guid.NewGuid(),
                        sessionQuestion.Id,
                        sourceAnswerOption.Id,
                        sourceAnswerOption.Text,
                        sourceAnswerOption.IsCorrect,
                        sourceAnswerOption.Position));
            }

            session.AddQuestion(sessionQuestion);
        }

        dbContext.QuizSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            CreateStateResponse(session));
    }

    [Authorize]
    [HttpPost("join/{roomCode}")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuizSessionStateResponse>> Join(
        string roomCode,
        CancellationToken cancellationToken)
    {
        var normalizedRoomCode = NormalizeRoomCode(roomCode);
        var session = await FindSessionByRoomCodeAsync(
            normalizedRoomCode,
            cancellationToken);
        var user = await userManager.GetUserAsync(User)
            ?? throw new AuthenticationRequiredException(
                "The authenticated account no longer exists.");
        var participant = new Participant(
            Guid.NewGuid(),
            session.Id,
            user.Id,
            user.DisplayName,
            timeProvider.GetUtcNow());

        session.AddParticipant(participant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var state = CreateStateResponse(session);
        await quizHubContext.Clients
            .Group(QuizHub.GetRoomGroup(session.Id))
            .ParticipantJoined(state);

        return Ok(state);
    }

    [Authorize]
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuizSessionStateResponse>> GetState(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        EnsureSessionMember(session);

        return Ok(CreateStateResponse(session));
    }

    [Authorize]
    [HttpPost("{sessionId:guid}/questions/{questionId:guid}/answers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitAnswer(
        Guid sessionId,
        Guid questionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        EnsureSessionMember(session);

        var userId = User.GetRequiredUserId();
        var participant = session.Participants.SingleOrDefault(
            existing => existing.UserId == userId)
            ?? throw new AccessDeniedException(
                "Only a joined participant can submit an answer.");
        var question = session.Questions.SingleOrDefault(
            existing => existing.Id == questionId)
            ?? throw new NotFoundException(
                "Session question was not found.");

        var alreadySubmitted = await dbContext.AnswerSubmissions.AnyAsync(
            submission =>
                submission.ParticipantId == participant.Id &&
                submission.SessionQuestionId == question.Id,
            cancellationToken);

        if (alreadySubmitted)
        {
            throw new ConflictException(
                "The participant has already answered this question.");
        }

        var submission = new AnswerSubmission(
            Guid.NewGuid(),
            participant,
            question,
            request.SelectedOptionIds,
            timeProvider.GetUtcNow());
        var selectedOptionIds = submission.SelectedOptions
            .Select(option => option.SessionAnswerOptionId)
            .ToHashSet();
        var correctOptionIds = question.AnswerOptions
            .Where(option => option.IsCorrect)
            .Select(option => option.Id)
            .ToHashSet();
        var isCorrect = selectedOptionIds.SetEquals(correctOptionIds);
        var awardedPoints = isCorrect ? question.Points : 0;

        submission.Grade(isCorrect, awardedPoints);
        participant.AwardPoints(awardedPoints);
        dbContext.AnswerSubmissions.Add(submission);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(
                "The participant has already answered this question.");
        }

        return NoContent();
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizSessionStateResponse>> Start(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await FindOwnedSessionAsync(
            sessionId,
            cancellationToken);

        session.Start(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        var state = CreateStateResponse(session);
        await quizHubContext.Clients
            .Group(QuizHub.GetRoomGroup(session.Id))
            .SessionStarted(state);

        return Ok(state);
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost("{sessionId:guid}/questions/{questionId:guid}/open")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizSessionStateResponse>> OpenQuestion(
        Guid sessionId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var session = await FindOwnedSessionAsync(
            sessionId,
            cancellationToken);

        session.OpenQuestion(questionId, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        var state = CreateStateResponse(session);
        await quizHubContext.Clients
            .Group(QuizHub.GetRoomGroup(session.Id))
            .QuestionOpened(state);

        return Ok(state);
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost("{sessionId:guid}/current-question/close")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizSessionStateResponse>>
        CloseCurrentQuestion(
            Guid sessionId,
            CancellationToken cancellationToken)
    {
        var session = await FindOwnedSessionAsync(
            sessionId,
            cancellationToken);

        session.CloseCurrentQuestion(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        var state = CreateStateResponse(session);
        var clients = quizHubContext.Clients.Group(
            QuizHub.GetRoomGroup(session.Id));
        await clients.QuestionClosed(state);
        await clients.LeaderboardUpdated(state);

        return Ok(state);
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost("{sessionId:guid}/finish")]
    [ProducesResponseType<QuizSessionStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuizSessionStateResponse>> Finish(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await FindOwnedSessionAsync(
            sessionId,
            cancellationToken);

        session.Finish(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        var state = CreateStateResponse(session);
        await quizHubContext.Clients
            .Group(QuizHub.GetRoomGroup(session.Id))
            .SessionFinished(state);

        return Ok(state);
    }

    private async Task<QuizSession> FindOwnedSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        User.EnsureOwner(session.OrganizerId);

        return session;
    }

    private async Task<QuizSession> FindSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return await SessionGraph()
            .SingleOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken)
            ?? throw new NotFoundException("Quiz session was not found.");
    }

    private async Task<QuizSession> FindSessionByRoomCodeAsync(
        string roomCode,
        CancellationToken cancellationToken)
    {
        return await SessionGraph()
            .SingleOrDefaultAsync(
                session => session.RoomCode == roomCode,
                cancellationToken)
            ?? throw new NotFoundException("Quiz room was not found.");
    }

    private IQueryable<QuizSession> SessionGraph()
    {
        return dbContext.QuizSessions
            .Include(session => session.Questions)
            .ThenInclude(question => question.AnswerOptions)
            .Include(session => session.Participants);
    }

    private void EnsureSessionMember(QuizSession session)
    {
        var userId = User.GetRequiredUserId();

        if (session.OrganizerId != userId &&
            session.Participants.All(
                participant => participant.UserId != userId))
        {
            throw new AccessDeniedException(
                "Only the organizer or a joined participant can view this room.");
        }
    }

    private QuizSessionStateResponse CreateStateResponse(
        QuizSession session)
    {
        return QuizSessionStateResponse.FromEntity(
            session,
            timeProvider.GetUtcNow());
    }

    private async Task<string> GenerateRoomCodeAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumRoomCodeAttempts; attempt++)
        {
            var roomCode = RandomNumberGenerator.GetString(
                RoomCodeAlphabet,
                QuizSession.RoomCodeLength);
            var alreadyExists = await dbContext.QuizSessions.AnyAsync(
                session =>
                    session.RoomCode == roomCode &&
                    (session.Status == QuizSessionStatus.Waiting ||
                     session.Status == QuizSessionStatus.Running),
                cancellationToken);

            if (!alreadyExists)
            {
                return roomCode;
            }
        }

        throw new ConflictException(
            "Could not generate a unique room code. Try again.");
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var normalized = roomCode.Trim().ToUpperInvariant();

        if (normalized.Length != QuizSession.RoomCodeLength ||
            normalized.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new RequestValidationException(
                "roomCode",
                $"Room code must contain exactly {QuizSession.RoomCodeLength} letters or digits.");
        }

        return normalized;
    }
}
