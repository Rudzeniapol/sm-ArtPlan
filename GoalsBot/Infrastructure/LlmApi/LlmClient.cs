using System.Net.Http.Json;
using System.Text.Json;
using GoalsBot.Application.Goals;
using GoalsBot.Infrastructure.Configuration;
using GoalsBot.Infrastructure.LlmApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoalsBot.Infrastructure.LlmApi;

public sealed class LlmClient(
    HttpClient http,
    IOptions<LlmOptions> options,
    PromptBuilder promptBuilder,
    ILogger<LlmClient> logger) : ILlmClient
{
    private readonly LlmOptions _options = options.Value;

    public async Task<List<ParsedTaskDto>> ParseGoalsAsync(string userMessage, CancellationToken ct)
    {
        var systemPrompt = promptBuilder.BuildSystemPrompt();
        var sanitized = promptBuilder.SanitizeUserInput(userMessage);

        var request = new ChatRequest(
            Model: _options.Model,
            MaxTokens: 1000,
            Messages: new[]
            {
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", sanitized)
            });

        using var content = JsonContent.Create(request, LlmJsonContext.Default.ChatRequest);
        using var response = await http.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var chat = await response.Content.ReadFromJsonAsync(LlmJsonContext.Default.ChatResponse, ct)
            ?? throw new InvalidOperationException("LLM returned an empty body.");

        if (chat.Choices.Count == 0)
        {
            logger.LogWarning("LLM returned a response with no choices.");
            return [];
        }

        var raw = chat.Choices[0].Message.Content;
        var cleaned = StripCodeFences(raw);

        try
        {
            return JsonSerializer.Deserialize(cleaned, LlmJsonContext.Default.ListParsedTaskDto) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize LLM output as ParsedTaskDto[]. Raw: {Raw}", raw);
            throw new InvalidOperationException("LLM returned malformed JSON.", ex);
        }
    }

    internal static string StripCodeFences(string raw)
    {
        var s = raw.Trim();
        if (!s.StartsWith("```")) return s;

        // Drop the opening fence line (may carry a language tag like ```json).
        var firstNewline = s.IndexOf('\n');
        s = firstNewline >= 0 ? s[(firstNewline + 1)..] : s[3..];

        if (s.EndsWith("```"))
            s = s[..^3];

        return s.Trim();
    }
}
