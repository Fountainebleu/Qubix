using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Qubix.Core.Authorization;
using Qubix.Core.Entities;
using Qubix.Core.Enums;
using Qubix.Infrastructure.Persistence;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class HistoryApiTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task History_ReturnsFinishedSessionsWithCalculatedValues()
    {
        using var organizerClient = CreateClient();
        using var participantClient = CreateClient();
        using var rivalClient = CreateClient();
        var organizerId = await RegisterAsync(
            organizerClient,
            ApplicationRoles.Organizer,
            "Organizer");
        var participantId = await RegisterAsync(
            participantClient,
            ApplicationRoles.Participant,
            "Participant");
        var rivalId = await RegisterAsync(
            rivalClient,
            ApplicationRoles.Participant,
            "Rival");
        var olderCompletion = new DateTimeOffset(
            2026,
            7,
            23,
            10,
            0,
            0,
            TimeSpan.Zero);
        var newerCompletion = olderCompletion.AddHours(2);

        await SeedHistoryAsync(
            organizerId,
            participantId,
            rivalId,
            olderCompletion,
            newerCompletion);

        var participantResponse = await participantClient.GetAsync(
            "/api/history/participant");
        var participantHistory = await ReadJsonAsync(participantResponse);
        var organizerResponse = await organizerClient.GetAsync(
            "/api/history/organizer");
        var organizerHistory = await ReadJsonAsync(organizerResponse);

        Assert.Equal(HttpStatusCode.OK, participantResponse.StatusCode);
        Assert.Equal(2, participantHistory.GetArrayLength());
        Assert.Equal(
            "Newer quiz",
            participantHistory[0].GetProperty("quizTitle").GetString());
        Assert.Equal(
            newerCompletion,
            participantHistory[0]
                .GetProperty("completedAtUtc")
                .GetDateTimeOffset());
        Assert.Equal(
            50,
            participantHistory[0].GetProperty("score").GetInt32());
        Assert.Equal(
            2,
            participantHistory[0].GetProperty("place").GetInt32());
        Assert.Equal(
            "Older quiz",
            participantHistory[1].GetProperty("quizTitle").GetString());
        Assert.Equal(
            100,
            participantHistory[1].GetProperty("score").GetInt32());
        Assert.Equal(
            1,
            participantHistory[1].GetProperty("place").GetInt32());

        Assert.Equal(HttpStatusCode.OK, organizerResponse.StatusCode);
        Assert.Equal(2, organizerHistory.GetArrayLength());
        Assert.Equal(
            "Newer quiz",
            organizerHistory[0].GetProperty("quizTitle").GetString());
        Assert.Equal(
            newerCompletion,
            organizerHistory[0]
                .GetProperty("completedAtUtc")
                .GetDateTimeOffset());
        Assert.Equal(
            2,
            organizerHistory[0]
                .GetProperty("participantCount")
                .GetInt32());
        Assert.Equal(
            "Older quiz",
            organizerHistory[1].GetProperty("quizTitle").GetString());
        Assert.Equal(
            2,
            organizerHistory[1]
                .GetProperty("participantCount")
                .GetInt32());
    }

    [Fact]
    public async Task History_EndpointsEnforceAuthenticationAndRoles()
    {
        using var organizerClient = CreateClient();
        using var participantClient = CreateClient();
        using var anonymousClient = CreateClient();
        await RegisterAsync(
            organizerClient,
            ApplicationRoles.Organizer,
            "Organizer");
        await RegisterAsync(
            participantClient,
            ApplicationRoles.Participant,
            "Participant");

        var participantAsOrganizer = await organizerClient.GetAsync(
            "/api/history/participant");
        var organizerAsParticipant = await participantClient.GetAsync(
            "/api/history/organizer");
        var anonymousResponse = await anonymousClient.GetAsync(
            "/api/history/participant");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            participantAsOrganizer.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            organizerAsParticipant.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);
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

    private async Task SeedHistoryAsync(
        Guid organizerId,
        Guid participantId,
        Guid rivalId,
        DateTimeOffset olderCompletion,
        DateTimeOffset newerCompletion)
    {
        var olderQuiz = CreatePublishedQuiz(
            organizerId,
            "Older quiz",
            olderCompletion.AddMinutes(-10));
        var newerQuiz = CreatePublishedQuiz(
            organizerId,
            "Newer quiz",
            newerCompletion.AddMinutes(-10));
        var runningQuiz = CreatePublishedQuiz(
            organizerId,
            "Running quiz",
            newerCompletion.AddMinutes(-5));
        var olderSession = CreateSession(
            olderQuiz,
            participantId,
            participantScore: 100,
            rivalId,
            rivalScore: 50,
            "HIS001",
            olderCompletion,
            finish: true);
        var newerSession = CreateSession(
            newerQuiz,
            participantId,
            participantScore: 50,
            rivalId,
            rivalScore: 100,
            "HIS002",
            newerCompletion,
            finish: true);
        var runningSession = CreateSession(
            runningQuiz,
            participantId,
            participantScore: 500,
            rivalId,
            rivalScore: 0,
            "HIS003",
            newerCompletion.AddMinutes(1),
            finish: false);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        dbContext.Quizzes.AddRange(olderQuiz, newerQuiz, runningQuiz);
        dbContext.QuizSessions.AddRange(
            olderSession,
            newerSession,
            runningSession);
        await dbContext.SaveChangesAsync();
    }

    private static Quiz CreatePublishedQuiz(
        Guid organizerId,
        string title,
        DateTimeOffset createdAtUtc)
    {
        var quiz = new Quiz(
            Guid.NewGuid(),
            organizerId,
            title,
            createdAtUtc);
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            QuestionType.SingleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 100,
            text: "Question");
        question.AddAnswerOption(
            new AnswerOption(
                Guid.NewGuid(),
                question.Id,
                "Correct",
                isCorrect: true,
                position: 0));
        question.AddAnswerOption(
            new AnswerOption(
                Guid.NewGuid(),
                question.Id,
                "Incorrect",
                isCorrect: false,
                position: 1));
        quiz.AddQuestion(question, createdAtUtc.AddSeconds(1));
        quiz.Publish(createdAtUtc.AddSeconds(2));

        return quiz;
    }

    private static QuizSession CreateSession(
        Quiz quiz,
        Guid participantId,
        int participantScore,
        Guid rivalId,
        int rivalScore,
        string roomCode,
        DateTimeOffset completedAtUtc,
        bool finish)
    {
        var sourceQuestion = quiz.Questions.Single();
        var session = new QuizSession(
            Guid.NewGuid(),
            quiz.Id,
            quiz.OwnerId,
            roomCode,
            completedAtUtc.AddMinutes(-2));
        var sessionQuestion = new SessionQuestion(
            Guid.NewGuid(),
            session.Id,
            sourceQuestion.Id,
            sourceQuestion.Type,
            sourceQuestion.Position,
            sourceQuestion.TimeLimitSeconds,
            sourceQuestion.Points,
            sourceQuestion.Text,
            sourceQuestion.ImageUrl);

        foreach (var sourceOption in sourceQuestion.AnswerOptions)
        {
            sessionQuestion.AddAnswerOption(
                new SessionAnswerOption(
                    Guid.NewGuid(),
                    sessionQuestion.Id,
                    sourceOption.Id,
                    sourceOption.Text,
                    sourceOption.IsCorrect,
                    sourceOption.Position));
        }

        var participant = new Participant(
            Guid.NewGuid(),
            session.Id,
            participantId,
            "Participant",
            completedAtUtc.AddMinutes(-2));
        participant.AwardPoints(participantScore);
        var rival = new Participant(
            Guid.NewGuid(),
            session.Id,
            rivalId,
            "Rival",
            completedAtUtc.AddMinutes(-2));
        rival.AwardPoints(rivalScore);

        session.AddQuestion(sessionQuestion);
        session.AddParticipant(participant);
        session.AddParticipant(rival);
        session.Start(completedAtUtc.AddMinutes(-1));
        session.OpenQuestion(
            sessionQuestion.Id,
            completedAtUtc.AddSeconds(-10));

        if (finish)
        {
            session.Finish(completedAtUtc);
        }

        return session;
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
