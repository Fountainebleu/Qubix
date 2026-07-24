using Microsoft.EntityFrameworkCore;
using Qubix.Core.Entities;
using Qubix.Infrastructure.Identity;
using Qubix.Infrastructure.Persistence;

namespace Qubix.IntegrationTests;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_ContainsAllDomainEntityTypes()
    {
        using var context = CreateContext();

        Type[] expectedEntityTypes =
        [
            typeof(Quiz),
            typeof(Question),
            typeof(AnswerOption),
            typeof(QuizSession),
            typeof(SessionQuestion),
            typeof(SessionAnswerOption),
            typeof(Participant),
            typeof(AnswerSubmission),
            typeof(AnswerSubmissionOption),
            typeof(ApplicationUser)
        ];

        var mappedEntityTypes = context.Model
            .GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToHashSet();

        Assert.All(expectedEntityTypes, type => Assert.Contains(type, mappedEntityTypes));
    }

    [Fact]
    public void QuizSession_ActiveRoomCodeIndex_IsUniqueAndFiltered()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(QuizSession))!;
        var index = entityType.GetIndexes().Single(
            candidate => candidate.GetDatabaseName() == "UX_QuizSessions_ActiveRoomCode");

        Assert.True(index.IsUnique);
        Assert.Equal("\"Status\" IN ('Waiting', 'Running')", index.GetFilter());
    }

    [Fact]
    public void AnswerSubmission_ParticipantAndQuestionIndex_IsUnique()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(AnswerSubmission))!;
        var index = entityType.GetIndexes().Single(
            candidate =>
                candidate.GetDatabaseName() ==
                "UX_AnswerSubmissions_ParticipantId_SessionQuestionId");

        Assert.True(index.IsUnique);
        Assert.Equal(
            [nameof(AnswerSubmission.ParticipantId), nameof(AnswerSubmission.SessionQuestionId)],
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Relationships_UseConfiguredDeleteBehaviors()
    {
        using var context = CreateContext();

        var questionForeignKey = context.Model
            .FindEntityType(typeof(Question))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Quiz));
        var sessionQuestionSourceForeignKey = context.Model
            .FindEntityType(typeof(SessionQuestion))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Question));

        Assert.Equal(DeleteBehavior.Cascade, questionForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, sessionQuestionSourceForeignKey.DeleteBehavior);
    }

    [Fact]
    public void UserReferences_UseRestrictDeleteBehavior()
    {
        using var context = CreateContext();

        Type[] dependentTypes =
        [
            typeof(Quiz),
            typeof(QuizSession),
            typeof(Participant)
        ];

        Assert.All(
            dependentTypes,
            dependentType =>
            {
                var userForeignKey = context.Model
                    .FindEntityType(dependentType)!
                    .GetForeignKeys()
                    .Single(
                        foreignKey =>
                            foreignKey.PrincipalEntityType.ClrType ==
                            typeof(ApplicationUser));

                Assert.Equal(DeleteBehavior.Restrict, userForeignKey.DeleteBehavior);
            });
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=qubix_model_tests;" +
                "Username=qubix;Password=model-tests-only")
            .Options;

        return new AppDbContext(options);
    }
}
