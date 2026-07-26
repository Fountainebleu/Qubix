namespace Qubix.Api.Contracts.Sessions;

public sealed record SubmitAnswerRequest(
    IReadOnlyCollection<Guid> SelectedOptionIds);
