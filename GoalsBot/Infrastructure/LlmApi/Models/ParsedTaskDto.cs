using System.Text.Json.Serialization;
using GoalsBot.Domain.Enums;

namespace GoalsBot.Infrastructure.LlmApi.Models;

public sealed record ParsedTaskDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("priority")] TaskPriority Priority,
    [property: JsonPropertyName("estimatedMinutes")] int? EstimatedMinutes
);
