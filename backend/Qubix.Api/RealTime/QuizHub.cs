using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Api.RealTime;

[Authorize]
public sealed class QuizHub(AppDbContext dbContext) : Hub<IQuizClient>
{
    public async Task JoinRoom(Guid sessionId)
    {
        var userIdValue = Context.User?.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new HubException("Authentication is required.");
        }

        var canJoin = await dbContext.QuizSessions.AnyAsync(
            session =>
                session.Id == sessionId &&
                (session.OrganizerId == userId ||
                 session.Participants.Any(
                     participant => participant.UserId == userId)),
            Context.ConnectionAborted);

        if (!canJoin)
        {
            throw new HubException(
                "Only the organizer or a joined participant can join this room group.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetRoomGroup(sessionId),
            Context.ConnectionAborted);
    }

    public static string GetRoomGroup(Guid sessionId)
    {
        return $"quiz:{sessionId}";
    }
}
