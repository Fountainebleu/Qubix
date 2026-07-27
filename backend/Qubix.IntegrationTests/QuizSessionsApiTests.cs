using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qubix.Api.RealTime;
using Qubix.Core.Authorization;
using Qubix.Core.Entities;
using Qubix.Core.Enums;
using Qubix.Infrastructure.Persistence;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class QuizSessionsApiTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task CreateRoom_ParticipantReceivesForbidden()
    {
        using var client = CreateClient();
        await RegisterAsync(
            client,
            ApplicationRoles.Participant,
            "Participant");

        var response = await client.PostAsJsonAsync(
            "/api/sessions",
            new { quizId = Guid.NewGuid() });
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateRoom_DraftQuizReturnsConflict()
    {
        using var client = CreateClient();
        var ownerId = await RegisterAsync(
            client,
            ApplicationRoles.Organizer,
            "Organizer");
        var quizId = await SeedQuizAsync(ownerId, publish: false);

        var response = await client.PostAsJsonAsync(
            "/api/sessions",
            new { quizId });
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("conflict", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Room_FullLifecycleReturnsSafeSharedState()
    {
        factory.QuizEvents.Clear();
        using var organizerClient = CreateClient();
        using var participantClient = CreateClient();
        using var outsiderClient = CreateClient();
        var organizerId = await RegisterAsync(
            organizerClient,
            ApplicationRoles.Organizer,
            "Quiz Organizer");
        await RegisterAsync(
            participantClient,
            ApplicationRoles.Participant,
            "Participant One");
        await RegisterAsync(
            outsiderClient,
            ApplicationRoles.Participant,
            "Outsider");
        var quizId = await SeedQuizAsync(organizerId, publish: true);

        var createResponse = await organizerClient.PostAsJsonAsync(
            "/api/sessions",
            new { quizId });
        var created = await ReadJsonAsync(createResponse);
        var sessionId = created.GetProperty("id").GetGuid();
        var roomCode = created.GetProperty("roomCode").GetString()!;
        var sessionQuestionId = created
            .GetProperty("questions")[0]
            .GetProperty("id")
            .GetGuid();

        var joinResponse = await participantClient.PostAsync(
            $"/api/sessions/join/{roomCode.ToLowerInvariant()}",
            content: null);
        var joined = await ReadJsonAsync(joinResponse);
        var participantStartResponse = await participantClient.PostAsync(
            $"/api/sessions/{sessionId}/start",
            content: null);
        var outsiderStateResponse = await outsiderClient.GetAsync(
            $"/api/sessions/{sessionId}");
        var participantStateResponse = await participantClient.GetAsync(
            $"/api/sessions/{sessionId}");

        var startResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/start",
            content: null);
        var openResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/questions/{sessionQuestionId}/open",
            content: null);
        var opened = await ReadJsonAsync(openResponse);
        var participantOpenStateResponse = await participantClient.GetAsync(
            $"/api/sessions/{sessionId}");
        var participantOpenState = await ReadJsonAsync(
            participantOpenStateResponse);

        var closeResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/current-question/close",
            content: null);
        var closed = await ReadJsonAsync(closeResponse);
        var finishResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/finish",
            content: null);
        var finished = await ReadJsonAsync(finishResponse);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Matches("^[A-Z0-9]{6}$", roomCode);
        Assert.Equal("Waiting", created.GetProperty("status").GetString());
        Assert.Equal(
            "Session quiz",
            created.GetProperty("quizTitle").GetString());
        Assert.Equal(
            "Quiz for testing a session",
            created.GetProperty("quizDescription").GetString());
        Assert.Equal(
            "Integration tests",
            created.GetProperty("quizCategory").GetString());
        Assert.Equal(
            "Choose every correct answer.",
            created.GetProperty("quizRules").GetString());
        Assert.Equal(
            "Pending",
            created.GetProperty("questions")[0].GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("openQuestion").ValueKind);

        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        Assert.Equal(
            "Participant One",
            joined.GetProperty("participants")[0]
                .GetProperty("displayName")
                .GetString());
        Assert.Equal(
            HttpStatusCode.Forbidden,
            participantStartResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, outsiderStateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, participantStateResponse.StatusCode);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);
        Assert.Equal("Running", opened.GetProperty("status").GetString());
        Assert.Equal(
            "MultipleChoice",
            opened.GetProperty("openQuestion").GetProperty("type").GetString());
        Assert.Equal(
            2,
            opened.GetProperty("openQuestion")
                .GetProperty("answerOptions")
                .GetArrayLength());
        Assert.False(
            opened.GetProperty("openQuestion")
                .GetProperty("answerOptions")[0]
                .TryGetProperty("isCorrect", out _));
        Assert.Equal(HttpStatusCode.OK, participantOpenStateResponse.StatusCode);
        Assert.Equal(
            sessionQuestionId,
            participantOpenState.GetProperty("openQuestion")
                .GetProperty("id")
                .GetGuid());

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        Assert.Equal(JsonValueKind.Null, closed.GetProperty("openQuestion").ValueKind);
        Assert.Equal(
            "Closed",
            closed.GetProperty("questions")[0].GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);
        Assert.Equal("Finished", finished.GetProperty("status").GetString());

        var events = factory.QuizEvents.Events.ToArray();
        Assert.Equal(
            new[]
            {
                "ParticipantJoined",
                "SessionStarted",
                "QuestionOpened",
                "QuestionClosed",
                "LeaderboardUpdated",
                "SessionFinished"
            },
            events.Select(recorded => recorded.EventName));
        Assert.All(
            events,
            recorded =>
            {
                Assert.Equal(
                    QuizHub.GetRoomGroup(sessionId),
                    recorded.GroupName);
                Assert.Equal(sessionId, recorded.State.Id);
            });
    }

    [Fact]
    public async Task QuizHub_NegotiateRequiresAuthentication()
    {
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/hubs/quiz/negotiate?negotiateVersion=1",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitAnswer_GradesExactSelectionAndSortsLeaderboard()
    {
        factory.QuizEvents.Clear();
        using var organizerClient = CreateClient();
        using var winnerClient = CreateClient();
        using var partialClient = CreateClient();
        using var lateClient = CreateClient();
        var organizerId = await RegisterAsync(
            organizerClient,
            ApplicationRoles.Organizer,
            "Organizer");
        await RegisterAsync(
            winnerClient,
            ApplicationRoles.Participant,
            "Winner");
        await RegisterAsync(
            partialClient,
            ApplicationRoles.Participant,
            "Partial");
        await RegisterAsync(
            lateClient,
            ApplicationRoles.Participant,
            "Late");
        var quizId = await SeedQuizAsync(
            organizerId,
            publish: true,
            correctAnswersCount: 2);
        var created = await CreateSessionAsync(organizerClient, quizId);
        var sessionId = created.GetProperty("id").GetGuid();
        var roomCode = created.GetProperty("roomCode").GetString();
        var questionId = created
            .GetProperty("questions")[0]
            .GetProperty("id")
            .GetGuid();

        await winnerClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);
        await partialClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);
        await lateClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);
        await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/start",
            content: null);
        var openResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/questions/{questionId}/open",
            content: null);
        var opened = await ReadJsonAsync(openResponse);
        var options = opened
            .GetProperty("openQuestion")
            .GetProperty("answerOptions");
        var firstCorrectOptionId = options[0].GetProperty("id").GetGuid();
        var secondCorrectOptionId = options[1].GetProperty("id").GetGuid();

        var winnerResponse = await winnerClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/questions/{questionId}/answers",
            new
            {
                selectedOptionIds = new[]
                {
                    firstCorrectOptionId,
                    secondCorrectOptionId
                }
            });
        var partialResponse = await partialClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/questions/{questionId}/answers",
            new
            {
                selectedOptionIds = new[] { firstCorrectOptionId }
            });
        var duplicateResponse = await winnerClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/questions/{questionId}/answers",
            new
            {
                selectedOptionIds = new[]
                {
                    firstCorrectOptionId,
                    secondCorrectOptionId
                }
            });

        var closeResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/current-question/close",
            content: null);
        var closed = await ReadJsonAsync(closeResponse);
        var closedQuestionResponse = await lateClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/questions/{questionId}/answers",
            new
            {
                selectedOptionIds = new[]
                {
                    firstCorrectOptionId,
                    secondCorrectOptionId
                }
            });

        Assert.Equal(HttpStatusCode.NoContent, winnerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, partialResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            closedQuestionResponse.StatusCode);

        var leaderboard = closed.GetProperty("participants");
        Assert.Equal("Winner", leaderboard[0].GetProperty("displayName").GetString());
        Assert.Equal(100, leaderboard[0].GetProperty("score").GetInt32());
        Assert.Equal("Partial", leaderboard[1].GetProperty("displayName").GetString());
        Assert.Equal(0, leaderboard[1].GetProperty("score").GetInt32());
        Assert.Equal("Late", leaderboard[2].GetProperty("displayName").GetString());
        Assert.Equal(0, leaderboard[2].GetProperty("score").GetInt32());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        var submissions = await dbContext.AnswerSubmissions
            .Where(submission => submission.SessionQuestionId == questionId)
            .OrderByDescending(submission => submission.AwardedPoints)
            .ToArrayAsync();

        Assert.Equal(2, submissions.Length);
        Assert.True(submissions[0].IsCorrect);
        Assert.Equal(100, submissions[0].AwardedPoints);
        Assert.False(submissions[1].IsCorrect);
        Assert.Equal(0, submissions[1].AwardedPoints);

        var leaderboardEvent = Assert.Single(
            factory.QuizEvents.Events,
            recorded => recorded.EventName == "LeaderboardUpdated");
        Assert.Equal(
            new[] { "Winner", "Partial", "Late" },
            leaderboardEvent.State.Participants.Select(
                participant => participant.DisplayName));
        Assert.Equal(
            new[] { 100, 0, 0 },
            leaderboardEvent.State.Participants.Select(
                participant => participant.Score));
    }

    [Fact]
    public async Task Join_DuplicateOrStartedRoomReturnsConflict()
    {
        using var organizerClient = CreateClient();
        using var firstParticipantClient = CreateClient();
        using var secondParticipantClient = CreateClient();
        var organizerId = await RegisterAsync(
            organizerClient,
            ApplicationRoles.Organizer,
            "Organizer");
        await RegisterAsync(
            firstParticipantClient,
            ApplicationRoles.Participant,
            "First participant");
        await RegisterAsync(
            secondParticipantClient,
            ApplicationRoles.Participant,
            "Second participant");
        var quizId = await SeedQuizAsync(organizerId, publish: true);
        var session = await CreateSessionAsync(organizerClient, quizId);
        var sessionId = session.GetProperty("id").GetGuid();
        var roomCode = session.GetProperty("roomCode").GetString();

        var firstJoinResponse = await firstParticipantClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);
        var duplicateJoinResponse = await firstParticipantClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);
        var startResponse = await organizerClient.PostAsync(
            $"/api/sessions/{sessionId}/start",
            content: null);
        var lateJoinResponse = await secondParticipantClient.PostAsync(
            $"/api/sessions/join/{roomCode}",
            content: null);

        Assert.Equal(HttpStatusCode.OK, firstJoinResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateJoinResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, lateJoinResponse.StatusCode);
    }

    [Fact]
    public async Task ManageRoom_OtherOrganizerReceivesForbidden()
    {
        using var ownerClient = CreateClient();
        using var otherOrganizerClient = CreateClient();
        var ownerId = await RegisterAsync(
            ownerClient,
            ApplicationRoles.Organizer,
            "Owner");
        await RegisterAsync(
            otherOrganizerClient,
            ApplicationRoles.Organizer,
            "Other organizer");
        var quizId = await SeedQuizAsync(ownerId, publish: true);
        var session = await CreateSessionAsync(ownerClient, quizId);
        var sessionId = session.GetProperty("id").GetGuid();

        var response = await otherOrganizerClient.PostAsync(
            $"/api/sessions/{sessionId}/start",
            content: null);
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });
    }

    private static async Task<Guid> RegisterAsync(
        HttpClient client,
        string role,
        string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "Password1",
                displayName,
                role
            });
        var user = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return user.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> CreateSessionAsync(
        HttpClient organizerClient,
        Guid quizId)
    {
        var response = await organizerClient.PostAsJsonAsync(
            "/api/sessions",
            new { quizId });
        var session = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return session;
    }

    private async Task<Guid> SeedQuizAsync(
        Guid ownerId,
        bool publish,
        int correctAnswersCount = 1)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var quiz = new Quiz(
            Guid.NewGuid(),
            ownerId,
            "Session quiz",
            createdAt,
            description: "Quiz for testing a session",
            category: "Integration tests",
            rules: "Choose every correct answer.");
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            QuestionType.MultipleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 100,
            text: "Select suitable answers");
        for (var position = 0;
             position < correctAnswersCount;
             position++)
        {
            question.AddAnswerOption(
                new AnswerOption(
                    Guid.NewGuid(),
                    question.Id,
                    $"Correct {position + 1}",
                    isCorrect: true,
                    position));
        }

        question.AddAnswerOption(
            new AnswerOption(
                Guid.NewGuid(),
                question.Id,
                "Incorrect",
                isCorrect: false,
                position: correctAnswersCount));
        quiz.AddQuestion(question, createdAt.AddSeconds(1));

        if (publish)
        {
            quiz.Publish(createdAt.AddSeconds(2));
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        return quiz.Id;
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
