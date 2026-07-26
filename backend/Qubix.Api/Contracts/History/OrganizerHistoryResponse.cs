namespace Qubix.Api.Contracts.History;

public sealed record OrganizerHistoryResponse(
    string QuizTitle,
    DateTimeOffset CompletedAtUtc,
    int ParticipantCount);
