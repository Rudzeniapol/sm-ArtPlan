using System.Text.Json.Serialization;
using GoalsBot.Infrastructure.LlmApi.Models;

namespace GoalsBot.Infrastructure.LlmApi;

// Source-generated serializer for the LLM HTTP payloads.
// `UseStringEnumConverter` makes `TaskPriority` round-trip as "Low"/"Medium"/"High".
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(List<ParsedTaskDto>))]
public sealed partial class LlmJsonContext : JsonSerializerContext;
