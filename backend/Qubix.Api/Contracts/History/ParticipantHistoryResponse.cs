namespace Qubix.Api.Contracts.History;

public sealed record ParticipantHistoryResponse(
    string QuizTitle,
    DateTimeOffset CompletedAtUtc,
    int Score,
    int Place);
