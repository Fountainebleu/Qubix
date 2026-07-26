using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubix.Api.Authorization;
using Qubix.Api.Contracts.History;
using Qubix.Core.Authorization;
using Qubix.Core.Enums;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/history")]
public sealed class HistoryController(
    AppDbContext dbContext) : ControllerBase
{
    [Authorize(Roles = ApplicationRoles.Participant)]
    [HttpGet("participant")]
    [ProducesResponseType<IReadOnlyCollection<ParticipantHistoryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<ParticipantHistoryResponse>>>
        GetParticipantHistory(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var history = await (
                from participant in dbContext.Participants.AsNoTracking()
                join session in dbContext.QuizSessions.AsNoTracking()
                    on participant.QuizSessionId equals session.Id
                join quiz in dbContext.Quizzes.AsNoTracking()
                    on session.QuizId equals quiz.Id
                where participant.UserId == userId
                    && session.Status == QuizSessionStatus.Finished
                orderby session.FinishedAtUtc descending
                select new ParticipantHistoryResponse(
                    quiz.Title,
                    session.FinishedAtUtc!.Value,
                    participant.Score,
                    dbContext.Participants.Count(other =>
                        other.QuizSessionId == session.Id &&
                        other.Score > participant.Score) + 1))
            .ToArrayAsync(cancellationToken);

        return Ok(history);
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpGet("organizer")]
    [ProducesResponseType<IReadOnlyCollection<OrganizerHistoryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<OrganizerHistoryResponse>>>
        GetOrganizerHistory(CancellationToken cancellationToken)
    {
        var organizerId = User.GetRequiredUserId();
        var history = await (
                from session in dbContext.QuizSessions.AsNoTracking()
                join quiz in dbContext.Quizzes.AsNoTracking()
                    on session.QuizId equals quiz.Id
                where session.OrganizerId == organizerId
                    && session.Status == QuizSessionStatus.Finished
                orderby session.FinishedAtUtc descending
                select new OrganizerHistoryResponse(
                    quiz.Title,
                    session.FinishedAtUtc!.Value,
                    dbContext.Participants.Count(participant =>
                        participant.QuizSessionId == session.Id)))
            .ToArrayAsync(cancellationToken);

        return Ok(history);
    }
}
