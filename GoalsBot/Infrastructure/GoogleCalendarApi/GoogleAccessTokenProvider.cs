using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoalsBot.Infrastructure.Configuration;
using GoalsBot.Infrastructure.GoogleCalendarApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoalsBot.Infrastructure.GoogleCalendarApi;

// Mints Google access tokens from a service-account key using the
// "JWT bearer" grant (RFC 7523). Tokens are cached until ~30 s before expiry.
public sealed class GoogleAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GoogleCalendarOptions> options,
    TimeProvider clock,
    ILogger<GoogleAccessTokenProvider> logger) : IGoogleAccessTokenProvider
{
    private const string Scope = "https://www.googleapis.com/auth/calendar.events";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (_cachedToken is not null && now < _cachedTokenExpiresAt - RefreshSkew)
            return _cachedToken;

        await _gate.WaitAsync(ct);
        try
        {
            now = clock.GetUtcNow();
            if (_cachedToken is not null && now < _cachedTokenExpiresAt - RefreshSkew)
                return _cachedToken;

            var key = ParseKey(options.Value.CredentialsJson);
            var assertion = BuildJwt(key, now);

            using var http = httpClientFactory.CreateClient("GoogleAuth");
            using var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", assertion)
            });

            using var response = await http.PostAsync(key.TokenUri, body, ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync(
                GoogleCalendarJsonContext.Default.GoogleTokenResponse, ct)
                ?? throw new InvalidOperationException("Google returned an empty token response.");

            _cachedToken = token.AccessToken;
            _cachedTokenExpiresAt = now.AddSeconds(token.ExpiresIn);
            logger.LogInformation("Refreshed Google access token; valid for {Seconds}s.", token.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static GoogleServiceAccountKey ParseKey(string credentialsJson)
    {
        if (string.IsNullOrWhiteSpace(credentialsJson))
            throw new InvalidOperationException("GoogleCalendar:CredentialsJson is empty.");

        return JsonSerializer.Deserialize(credentialsJson, GoogleCalendarJsonContext.Default.GoogleServiceAccountKey)
            ?? throw new InvalidOperationException("Failed to parse Google service-account credentials.");
    }

    private static string BuildJwt(GoogleServiceAccountKey key, DateTimeOffset now)
    {
        var iat = now.ToUnixTimeSeconds();
        var exp = iat + 3600;

        var header = """{"alg":"RS256","typ":"JWT"}""";
        var claims = $$"""
            {"iss":"{{key.ClientEmail}}","scope":"{{Scope}}","aud":"{{key.TokenUri}}","exp":{{exp}},"iat":{{iat}}}
            """;

        var encodedHeader = Base64Url(Encoding.UTF8.GetBytes(header));
        var encodedClaims = Base64Url(Encoding.UTF8.GetBytes(claims));
        var signingInput = $"{encodedHeader}.{encodedClaims}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PrivateKey);
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
