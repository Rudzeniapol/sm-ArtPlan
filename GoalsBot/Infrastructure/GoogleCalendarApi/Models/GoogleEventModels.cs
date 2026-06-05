using System.Text.Json.Serialization;

namespace GoalsBot.Infrastructure.GoogleCalendarApi.Models;

public sealed record GoogleEventDate(
    [property: JsonPropertyName("date")] string Date
);

public sealed record GoogleEventPayload(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("start")] GoogleEventDate Start,
    [property: JsonPropertyName("end")] GoogleEventDate End
);

public sealed record GoogleEventResponse(
    [property: JsonPropertyName("id")] string Id
);

public sealed record GoogleTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType
);

public sealed record GoogleServiceAccountKey(
    [property: JsonPropertyName("client_email")] string ClientEmail,
    [property: JsonPropertyName("private_key")] string PrivateKey,
    [property: JsonPropertyName("token_uri")] string TokenUri
);
