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
public sealed class QuizzesApiTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task Create_AnonymousUserReceivesUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/quizzes",
            CreateQuizRequest("Anonymous quiz"));
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "authentication_required",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_ParticipantReceivesForbidden()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Participant);

        var response = await client.PostAsJsonAsync(
            "/api/quizzes",
            CreateQuizRequest("Participant quiz"));
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Organizer_CanCreateListViewUpdateAndArchiveOwnQuiz()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);

        var created = await CreateQuizAsync(client, "Original title");
        var quizId = created.GetProperty("id").GetGuid();

        var listResponse = await client.GetAsync("/api/quizzes");
        var list = await ReadJsonAsync(listResponse);
        var getResponse = await client.GetAsync($"/api/quizzes/{quizId}");
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}",
            CreateQuizRequest("Updated title", defaultQuestionTimeSeconds: 45));
        var updated = await ReadJsonAsync(updateResponse);
        var archiveResponse = await client.PostAsync(
            $"/api/quizzes/{quizId}/archive",
            content: null);
        var archived = await ReadJsonAsync(archiveResponse);
        var updateArchivedResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}",
            CreateQuizRequest("Cannot update archived"));

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Single(list.EnumerateArray());
        Assert.Equal(quizId, list[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Updated title", updated.GetProperty("title").GetString());
        Assert.Equal(
            45,
            updated.GetProperty("defaultQuestionTimeSeconds").GetInt32());
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        Assert.Equal("Archived", archived.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, updateArchivedResponse.StatusCode);
    }

    [Fact]
    public async Task QuizOwnedByAnotherOrganizer_ReturnsForbidden()
    {
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        await RegisterAsync(ownerClient, ApplicationRoles.Organizer);
        await RegisterAsync(otherClient, ApplicationRoles.Organizer);

        var quiz = await CreateQuizAsync(ownerClient, "Private quiz");
        var quizId = quiz.GetProperty("id").GetGuid();

        var getResponse = await otherClient.GetAsync(
            $"/api/quizzes/{quizId}");
        var archiveResponse = await otherClient.PostAsync(
            $"/api/quizzes/{quizId}/archive",
            content: null);
        var problem = await ReadJsonAsync(getResponse);

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, archiveResponse.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Publish_EmptyQuizReturnsConflict()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);
        var quiz = await CreateQuizAsync(client, "Empty quiz");
        var quizId = quiz.GetProperty("id").GetGuid();

        var response = await client.PostAsync(
            $"/api/quizzes/{quizId}/publish",
            content: null);
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("conflict", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Publish_ReadyQuizChangesStatusToPublished()
    {
        using var client = CreateClient();
        var ownerId = await RegisterAsync(
            client,
            ApplicationRoles.Organizer);
        var quizId = await SeedReadyQuizAsync(ownerId);

        var response = await client.PostAsync(
            $"/api/quizzes/{quizId}/publish",
            content: null);
        var published = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Published", published.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_MissingQuizReturnsNotFound()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);

        var response = await client.GetAsync(
            $"/api/quizzes/{Guid.NewGuid()}");
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "resource_not_found",
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
        string role)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "Password1",
                displayName = "Quiz API Test",
                role
            });
        var user = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return user.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> CreateQuizAsync(
        HttpClient client,
        string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/quizzes",
            CreateQuizRequest(title));
        var quiz = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Draft", quiz.GetProperty("status").GetString());

        return quiz;
    }

    private async Task<Guid> SeedReadyQuizAsync(Guid ownerId)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var quiz = new Quiz(
            Guid.NewGuid(),
            ownerId,
            "Ready quiz",
            timestamp);
        var question = new Question(
            Guid.NewGuid(),
            quiz.Id,
            QuestionType.SingleChoice,
            position: 0,
            timeLimitSeconds: 30,
            points: 100,
            text: "Which answer is correct?");

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
        quiz.AddQuestion(question, timestamp);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        return quiz.Id;
    }

    private static object CreateQuizRequest(
        string title,
        int defaultQuestionTimeSeconds = 30)
    {
        return new
        {
            title,
            description = "Quiz description",
            category = "General",
            rules = "Choose the correct answer.",
            defaultQuestionTimeSeconds
        };
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
