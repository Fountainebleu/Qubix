using Microsoft.EntityFrameworkCore;
using Qubix.Core.Entities;

namespace Qubix.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();

    public DbSet<SessionQuestion> SessionQuestions => Set<SessionQuestion>();

    public DbSet<SessionAnswerOption> SessionAnswerOptions => Set<SessionAnswerOption>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<AnswerSubmission> AnswerSubmissions => Set<AnswerSubmission>();

    public DbSet<AnswerSubmissionOption> AnswerSubmissionOptions =>
        Set<AnswerSubmissionOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
