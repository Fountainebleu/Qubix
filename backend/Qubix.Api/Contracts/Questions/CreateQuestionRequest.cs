using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Qubix.Core.Entities;
using Qubix.Core.Enums;

namespace Qubix.Api.Contracts.Questions;

public sealed class CreateQuestionRequest
{
    [MaxLength(Question.MaxTextLength)]
    public string? Text { get; init; }

    [MaxLength(Question.MaxImageUrlLength)]
    public string? ImageUrl { get; init; }

    [EnumDataType(typeof(QuestionType))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType Type { get; init; }

    [Range(0, int.MaxValue)]
    public int Position { get; init; }

    [Range(
        Question.MinimumTimeLimitSeconds,
        Question.MaximumTimeLimitSeconds)]
    public int TimeLimitSeconds { get; init; }

    [Range(Question.MinimumPoints, Question.MaximumPoints)]
    public int Points { get; init; }
}
