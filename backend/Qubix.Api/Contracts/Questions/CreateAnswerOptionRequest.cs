using System.ComponentModel.DataAnnotations;
using Qubix.Core.Entities;

namespace Qubix.Api.Contracts.Questions;

public sealed class CreateAnswerOptionRequest
{
    [Required]
    [MaxLength(AnswerOption.MaxTextLength)]
    public string Text { get; init; } = string.Empty;

    public bool IsCorrect { get; init; }

    [Range(0, int.MaxValue)]
    public int Position { get; init; }
}
