using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.Api.Contracts.Sessions;

public sealed record QuizSessionStateResponse(
    Guid Id,
    Guid QuizId,
    string RoomCode,
    string Status,
    Guid? CurrentQuestionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    IReadOnlyCollection<SessionParticipantResponse> Participants,
    IReadOnlyCollection<SessionQuestionSummaryResponse> Questions,
    OpenQuestionResponse? OpenQuestion)
{
    public static QuizSessionStateResponse FromEntity(
        QuizSession session,
        DateTimeOffset currentTime)
    {
        var openQuestion = session.CurrentQuestionId is null
            ? null
            : session.Questions.SingleOrDefault(
                question =>
                    question.Id == session.CurrentQuestionId.Value &&
                    question.AcceptsAnswersAt(currentTime));

        return new QuizSessionStateResponse(
            session.Id,
            session.QuizId,
            session.RoomCode,
            session.Status.ToString(),
            session.CurrentQuestionId,
            session.CreatedAtUtc,
            session.StartedAtUtc,
            session.FinishedAtUtc,
            session.Participants
                .OrderBy(participant => participant.JoinedAtUtc)
                .Select(SessionParticipantResponse.FromEntity)
                .ToArray(),
            session.Questions
                .OrderBy(question => question.Position)
                .Select(question => new SessionQuestionSummaryResponse(
                    question.Id,
                    question.Position,
                    GetEffectiveStatus(question, currentTime)))
                .ToArray(),
            openQuestion is null
                ? null
                : OpenQuestionResponse.FromEntity(openQuestion));
    }

    private static string GetEffectiveStatus(
        SessionQuestion question,
        DateTimeOffset currentTime)
    {
        if (question.Status == SessionQuestionStatus.Open &&
            question.ClosesAtUtc < currentTime)
        {
            return SessionQuestionStatus.Closed.ToString();
        }

        return question.Status.ToString();
    }
}
