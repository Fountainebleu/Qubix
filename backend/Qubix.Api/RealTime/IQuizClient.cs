using Qubix.Api.Contracts.Sessions;

namespace Qubix.Api.RealTime;

public interface IQuizClient
{
    Task ParticipantJoined(QuizSessionStateResponse state);

    Task SessionStarted(QuizSessionStateResponse state);

    Task QuestionOpened(QuizSessionStateResponse state);

    Task QuestionClosed(QuizSessionStateResponse state);

    Task LeaderboardUpdated(QuizSessionStateResponse state);

    Task SessionFinished(QuizSessionStateResponse state);
}
