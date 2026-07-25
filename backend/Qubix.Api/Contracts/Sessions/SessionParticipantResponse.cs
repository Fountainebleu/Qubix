using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Sessions;

public sealed record SessionParticipantResponse(
    Guid Id,
    string DisplayName,
    int Score)
{
    public static SessionParticipantResponse FromEntity(
        Participant participant)
    {
        return new SessionParticipantResponse(
            participant.Id,
            participant.DisplayName,
            participant.Score);
    }
}
