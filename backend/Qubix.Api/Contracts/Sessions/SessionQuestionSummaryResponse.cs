namespace Qubix.Api.Contracts.Sessions;

public sealed record SessionQuestionSummaryResponse(
    Guid Id,
    int Position,
    string Status);
