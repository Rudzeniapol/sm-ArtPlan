using GoalsBot.Infrastructure.LlmApi.Models;

namespace GoalsBot.Infrastructure.LlmApi;

public interface ILlmClient
{
    Task<List<ParsedTaskDto>> ParseGoalsAsync(string userMessage, CancellationToken ct);
}
