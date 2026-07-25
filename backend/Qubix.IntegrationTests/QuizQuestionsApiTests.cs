using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Qubix.Core.Authorization;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class QuizQuestionsApiTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task QuestionEditor_ParticipantReceivesForbidden()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Participant);

        var response = await client.GetAsync(
            $"/api/quizzes/{Guid.NewGuid()}/questions");
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task QuestionEditor_NonOwnerReceivesForbidden()
    {
        using var ownerClient = CreateClient();
        using var otherClient = CreateClient();
        await RegisterAsync(ownerClient, ApplicationRoles.Organizer);
        await RegisterAsync(otherClient, ApplicationRoles.Organizer);
        var quizId = await CreateQuizAsync(ownerClient, "Owner quiz");

        var response = await otherClient.GetAsync(
            $"/api/quizzes/{quizId}/questions");
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Organizer_CanManageQuestionsAndAnswerOptions()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);
        var quizId = await CreateQuizAsync(client, "Editor quiz");
        var question = await CreateQuestionAsync(
            client,
            quizId,
            type: "SingleChoice",
            text: "Initial text");
        var questionId = question.GetProperty("id").GetGuid();

        var updateQuestionResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}",
            new
            {
                text = (string?)null,
                imageUrl = "https://example.com/question.png",
                type = "MultipleChoice",
                position = 0,
                timeLimitSeconds = 45,
                points = 200
            });
        var updatedQuestion = await ReadJsonAsync(updateQuestionResponse);

        var firstOption = await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "First",
            isCorrect: true,
            position: 0);
        var secondOption = await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "Second",
            isCorrect: false,
            position: 1);
        var secondOptionId = secondOption.GetProperty("id").GetGuid();

        var updateOptionResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}/options/{secondOptionId}",
            new
            {
                text = "Updated second",
                isCorrect = true,
                position = 1
            });
        var updatedOption = await ReadJsonAsync(updateOptionResponse);
        var listResponse = await client.GetAsync(
            $"/api/quizzes/{quizId}/questions");
        var questions = await ReadJsonAsync(listResponse);

        var deleteOptionResponse = await client.DeleteAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}/options/{secondOptionId}");
        var deleteQuestionResponse = await client.DeleteAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}");
        var emptyListResponse = await client.GetAsync(
            $"/api/quizzes/{quizId}/questions");
        var emptyList = await ReadJsonAsync(emptyListResponse);

        Assert.Equal(HttpStatusCode.OK, updateQuestionResponse.StatusCode);
        Assert.Equal(
            "https://example.com/question.png",
            updatedQuestion.GetProperty("imageUrl").GetString());
        Assert.Equal(
            "MultipleChoice",
            updatedQuestion.GetProperty("type").GetString());
        Assert.True(firstOption.GetProperty("isCorrect").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, updateOptionResponse.StatusCode);
        Assert.True(updatedOption.GetProperty("isCorrect").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(2, questions[0].GetProperty("answerOptions").GetArrayLength());
        Assert.True(
            questions[0]
                .GetProperty("answerOptions")[0]
                .TryGetProperty("isCorrect", out _));
        Assert.Equal(HttpStatusCode.NoContent, deleteOptionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteQuestionResponse.StatusCode);
        Assert.Empty(emptyList.EnumerateArray());
    }

    [Fact]
    public async Task Publish_SingleChoiceRequiresExactlyOneCorrectAnswer()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);
        var quizId = await CreateQuizAsync(client, "Single choice quiz");
        var question = await CreateQuestionAsync(
            client,
            quizId,
            type: "SingleChoice",
            text: "Choose one");
        var questionId = question.GetProperty("id").GetGuid();
        var firstOption = await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "First",
            isCorrect: false,
            position: 0);
        await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "Second",
            isCorrect: false,
            position: 1);

        var invalidPublishResponse = await client.PostAsync(
            $"/api/quizzes/{quizId}/publish",
            content: null);

        var firstOptionId = firstOption.GetProperty("id").GetGuid();
        var updateOptionResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}/options/{firstOptionId}",
            new
            {
                text = "First",
                isCorrect = true,
                position = 0
            });
        var publishResponse = await client.PostAsync(
            $"/api/quizzes/{quizId}/publish",
            content: null);
        var published = await ReadJsonAsync(publishResponse);
        var updatePublishedResponse = await client.PutAsJsonAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}",
            QuestionRequest("SingleChoice", "Cannot edit"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            invalidPublishResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateOptionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        Assert.Equal("Published", published.GetProperty("status").GetString());
        Assert.Equal(
            HttpStatusCode.Conflict,
            updatePublishedResponse.StatusCode);
    }

    [Fact]
    public async Task Publish_MultipleChoiceAllowsOneCorrectAnswer()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);
        var quizId = await CreateQuizAsync(client, "Multiple choice quiz");
        var question = await CreateQuestionAsync(
            client,
            quizId,
            type: "MultipleChoice",
            text: "Choose all suitable answers");
        var questionId = question.GetProperty("id").GetGuid();
        await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "Correct",
            isCorrect: true,
            position: 0);
        await CreateAnswerOptionAsync(
            client,
            quizId,
            questionId,
            "Incorrect",
            isCorrect: false,
            position: 1);

        var response = await client.PostAsync(
            $"/api/quizzes/{quizId}/publish",
            content: null);
        var published = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Published", published.GetProperty("status").GetString());
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
                displayName = "Question API Test",
                role
            });
        var user = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return user.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateQuizAsync(
        HttpClient client,
        string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/quizzes",
            new
            {
                title,
                defaultQuestionTimeSeconds = 30
            });
        var quiz = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return quiz.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> CreateQuestionAsync(
        HttpClient client,
        Guid quizId,
        string type,
        string? text)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/quizzes/{quizId}/questions",
            QuestionRequest(type, text));
        var question = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return question;
    }

    private static async Task<JsonElement> CreateAnswerOptionAsync(
        HttpClient client,
        Guid quizId,
        Guid questionId,
        string text,
        bool isCorrect,
        int position)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/quizzes/{quizId}/questions/{questionId}/options",
            new
            {
                text,
                isCorrect,
                position
            });
        var answerOption = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return answerOption;
    }

    private static object QuestionRequest(
        string type,
        string? text)
    {
        return new
        {
            text,
            imageUrl = (string?)null,
            type,
            position = 0,
            timeLimitSeconds = 30,
            points = 100
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
