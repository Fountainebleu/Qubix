using Microsoft.AspNetCore.SignalR;
using Qubix.Api.Contracts.Sessions;
using Qubix.Api.RealTime;

namespace Qubix.IntegrationTests;

public sealed record RecordedQuizEvent(
    string GroupName,
    string EventName,
    QuizSessionStateResponse State);

public sealed class RecordingQuizHubContext :
    IHubContext<QuizHub, IQuizClient>
{
    private readonly List<RecordedQuizEvent> _events = [];

    public IHubClients<IQuizClient> Clients =>
        new RecordingQuizHubClients(this);

    public IGroupManager Groups { get; } = new NoOpGroupManager();

    public IReadOnlyCollection<RecordedQuizEvent> Events
    {
        get
        {
            lock (_events)
            {
                return _events.ToArray();
            }
        }
    }

    public void Clear()
    {
        lock (_events)
        {
            _events.Clear();
        }
    }

    private void Record(
        string groupName,
        string eventName,
        QuizSessionStateResponse state)
    {
        lock (_events)
        {
            _events.Add(new RecordedQuizEvent(
                groupName,
                eventName,
                state));
        }
    }

    private sealed class RecordingQuizHubClients(
        RecordingQuizHubContext context) : IHubClients<IQuizClient>
    {
        public IQuizClient All => CreateClient("all");

        public IQuizClient AllExcept(
            IReadOnlyList<string> excludedConnectionIds)
        {
            return CreateClient("all");
        }

        public IQuizClient Client(string connectionId)
        {
            return CreateClient($"client:{connectionId}");
        }

        public IQuizClient Clients(IReadOnlyList<string> connectionIds)
        {
            return CreateClient("clients");
        }

        public IQuizClient Group(string groupName)
        {
            return CreateClient(groupName);
        }

        public IQuizClient GroupExcept(
            string groupName,
            IReadOnlyList<string> excludedConnectionIds)
        {
            return CreateClient(groupName);
        }

        public IQuizClient Groups(IReadOnlyList<string> groupNames)
        {
            return CreateClient(string.Join(",", groupNames));
        }

        public IQuizClient User(string userId)
        {
            return CreateClient($"user:{userId}");
        }

        public IQuizClient Users(IReadOnlyList<string> userIds)
        {
            return CreateClient("users");
        }

        private IQuizClient CreateClient(string target)
        {
            return new RecordingQuizClient(context, target);
        }
    }

    private sealed class RecordingQuizClient(
        RecordingQuizHubContext context,
        string target) : IQuizClient
    {
        public Task ParticipantJoined(QuizSessionStateResponse state)
        {
            return Record(nameof(ParticipantJoined), state);
        }

        public Task SessionStarted(QuizSessionStateResponse state)
        {
            return Record(nameof(SessionStarted), state);
        }

        public Task QuestionOpened(QuizSessionStateResponse state)
        {
            return Record(nameof(QuestionOpened), state);
        }

        public Task QuestionClosed(QuizSessionStateResponse state)
        {
            return Record(nameof(QuestionClosed), state);
        }

        public Task LeaderboardUpdated(QuizSessionStateResponse state)
        {
            return Record(nameof(LeaderboardUpdated), state);
        }

        public Task SessionFinished(QuizSessionStateResponse state)
        {
            return Record(nameof(SessionFinished), state);
        }

        private Task Record(
            string eventName,
            QuizSessionStateResponse state)
        {
            context.Record(target, eventName, state);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
