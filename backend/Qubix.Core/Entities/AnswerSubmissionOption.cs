using Qubix.Core.Common;

namespace Qubix.Core.Entities;

public sealed class AnswerSubmissionOption
{
    private AnswerSubmissionOption()
    {
    }

    internal AnswerSubmissionOption(
        Guid answerSubmissionId,
        Guid sessionAnswerOptionId)
    {
        AnswerSubmissionId = Guard.NotEmpty(answerSubmissionId, nameof(answerSubmissionId));
        SessionAnswerOptionId = Guard.NotEmpty(
            sessionAnswerOptionId,
            nameof(sessionAnswerOptionId));
    }

    public Guid AnswerSubmissionId { get; private set; }

    public Guid SessionAnswerOptionId { get; private set; }
}
