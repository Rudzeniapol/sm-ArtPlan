namespace GoalsBot.Infrastructure.GoogleCalendarApi;

public interface IGoogleAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct);
}
