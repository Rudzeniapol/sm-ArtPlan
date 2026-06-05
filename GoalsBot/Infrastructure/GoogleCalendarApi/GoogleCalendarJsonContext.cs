using System.Text.Json.Serialization;
using GoalsBot.Infrastructure.GoogleCalendarApi.Models;

namespace GoalsBot.Infrastructure.GoogleCalendarApi;

[JsonSerializable(typeof(GoogleEventPayload))]
[JsonSerializable(typeof(GoogleEventResponse))]
[JsonSerializable(typeof(GoogleTokenResponse))]
[JsonSerializable(typeof(GoogleServiceAccountKey))]
public sealed partial class GoogleCalendarJsonContext : JsonSerializerContext;
