namespace GoalsBot.Application.Goals;

public sealed class PromptBuilder
{
    private const string SystemPrompt = """
        You are a productivity assistant. The user will describe their goals for the day in free form. Extract every distinct actionable task from their message and return ONLY a valid JSON array — no markdown, no extra text. Each element must match this schema exactly:

        [
          {
            "title": "string (max 200 chars, imperative verb phrase)",
            "description": "string or null (elaboration, max 1000 chars)",
            "priority": "Low" | "Medium" | "High",
            "estimatedMinutes": integer or null
          }
        ]

        Rules:
        - One object per distinct task; split compound tasks.
        - Infer priority from urgency/importance language; default to Medium.
        - estimatedMinutes: estimate realistically from context; null if unclear.
        - Never add tasks not implied by the user's message.
        - Return an empty array [] if no tasks can be extracted.
        """;

    public string BuildSystemPrompt() => SystemPrompt;

    public string SanitizeUserInput(string raw)
    {
        // Trim and cap absurdly long messages so we don't ship multi-MB prompts.
        var trimmed = raw.Trim();
        return trimmed.Length > 4000 ? trimmed[..4000] : trimmed;
    }
}
