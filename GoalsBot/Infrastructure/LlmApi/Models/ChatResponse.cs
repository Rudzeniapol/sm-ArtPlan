using System.Text.Json.Serialization;

namespace GoalsBot.Infrastructure.LlmApi.Models;

public sealed record ChatResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices
);

public sealed record ChatChoice(
    [property: JsonPropertyName("message")] ChatMessage Message
);
