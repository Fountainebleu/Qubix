using System.ComponentModel.DataAnnotations;
using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Quizzes;

public sealed class CreateQuizRequest
{
    [Required]
    [MaxLength(Quiz.MaxTitleLength)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(Quiz.MaxDescriptionLength)]
    public string? Description { get; init; }

    [MaxLength(Quiz.MaxCategoryLength)]
    public string? Category { get; init; }

    [MaxLength(Quiz.MaxRulesLength)]
    public string? Rules { get; init; }

    [Range(
        Quiz.MinimumQuestionTimeSeconds,
        Quiz.MaximumQuestionTimeSeconds)]
    public int DefaultQuestionTimeSeconds { get; init; } = 30;
}
