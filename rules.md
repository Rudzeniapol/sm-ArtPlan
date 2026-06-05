# .NET Telegram Bot Development Rules

You are a senior .NET backend developer and an expert in C#, Telegram.Bot, Entity Framework Core, and LLM API integration via HTTP.

## Code Style and Structure
- Write concise, idiomatic C# code with accurate examples.
- Organize the project using a clean layered structure:
  - `Bot/` — Telegram update handlers, command routing, middleware pipeline
  - `Application/` — use-case services, LLM orchestration, business logic
  - `Domain/` — entities, value objects, domain interfaces
  - `Infrastructure/` — EF Core DbContext, repositories, HTTP clients, external service adapters
  - `Worker/` — `BackgroundService` hosting the bot polling or webhook listener
- Use object-oriented and functional programming patterns as appropriate.
- Prefer LINQ and lambda expressions for collection operations.
- Use descriptive variable and method names (e.g., `IsUserRegistered`, `BuildPromptContext`).

## Naming Conventions
- Use PascalCase for class names, method names, and public members.
- Use camelCase for local variables and private fields.
- Use UPPERCASE for constants.
- Prefix interface names with "I" (e.g., `ILlmClient`, `IConversationRepository`).
- Suffix handler classes with `Handler` (e.g., `StartCommandHandler`, `MessageUpdateHandler`).

## C# and .NET Usage
- Target .NET 8 or later; use modern C# features (primary constructors, collection expressions, pattern matching, `required` members).
- Use `IHostedService` / `BackgroundService` as the bot's entry point (polling via `ITelegramBotClient.ReceiveAsync` or webhook via ASP.NET Core minimal API).
- Use `IOptions<T>` / `IOptionsSnapshot<T>` for all configuration (bot token, LLM endpoint, model name, etc.).
- Use `CancellationToken` propagation throughout the entire call chain.

## Telegram Bot Integration
- Use the official **Telegram.Bot** NuGet package (`Telegram.Bot`).
- Register `ITelegramBotClient` as a singleton via `AddHttpClient` + `AddSingleton`.
- Route updates through a dispatcher that maps `UpdateType` → handler:

  ```csharp
  public sealed class UpdateDispatcher(IEnumerable<IUpdateHandler> handlers)
  {
      public Task DispatchAsync(Update update, CancellationToken ct) =>
          handlers.FirstOrDefault(h => h.CanHandle(update))
                  ?.HandleAsync(update, ct)
          ?? Task.CompletedTask;
  }
  ```

- Implement each command / update type as its own handler class implementing `IUpdateHandler`.
- Always answer `CallbackQuery` updates with `AnswerCallbackQueryAsync` to remove the loading spinner.
- Send typing indicators with `SendChatActionAsync` before long-running operations (LLM calls, DB queries).
- Never expose the bot token in source code; load it from environment variables or Secret Manager.

## LLM API Integration (HTTP)
- Create a strongly-typed `ILlmClient` interface and register a named `HttpClient` with `AddHttpClient<ILlmClient, LlmClient>`.
- Configure base address, timeout, and default headers (e.g., `Authorization: Bearer`) in `HttpClientHandler` / `DelegatingHandler`, not inside methods.
- Model the request and response as C# records with `System.Text.Json` source-generated serializers (`[JsonSerializable]` + `JsonSerializerContext`).
- Prefer streaming responses (`stream: true`) and yield tokens back via `IAsyncEnumerable<string>` for real-time Telegram message editing.
- Implement a `DelegatingHandler` for retry / exponential back-off (or use **Polly** via `AddResilienceHandler`).
- Keep prompt-building logic in a dedicated `PromptBuilder` / `PromptFactory` class in the Application layer — never inline prompts in handlers.
- Respect rate limits; use `SemaphoreSlim` or a token-bucket when calling the LLM for concurrent users.

## Entity Framework Core
- Use EF Core 8+ with a single `AppDbContext` registered via `AddDbContextPool<AppDbContext>`.
- Define entity configurations in separate `IEntityTypeConfiguration<T>` classes (one per entity); apply them with `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- Never use `DbContext` directly in handlers or application services — always go through repository interfaces defined in the Domain layer.
- Use `AsNoTracking()` for read-only queries.
- Apply migrations via `dotnet ef migrations add` / `dotnet ef database update`; do not call `EnsureCreated` in production.
- Use `ValueConverter` for value objects (e.g., storing a `UserId` record as a `long`).
- Store conversation history as a JSON column (`[Column(TypeName = "jsonb")]` on PostgreSQL or `TEXT` on SQLite) to avoid excessive joins.

## Key Domain Entities (Suggested)
```
User          — TelegramId (long), Username, CreatedAt, Settings (owned)
Conversation  — Id, UserId, Messages (JSON), CreatedAt, UpdatedAt
Message       — Role (enum: System/User/Assistant), Content, TokenCount, CreatedAt
```

## Error Handling and Validation
- Wrap every update handler in a top-level try/catch; log the exception and send a user-friendly Telegram message — never let an unhandled exception kill the polling loop.
- Use `FluentValidation` for validating user-supplied input (parsed commands, callback data, etc.).
- Use `ILogger<T>` throughout; configure structured logging with Serilog or Microsoft.Extensions.Logging + a sink of your choice.
- Map known error types to appropriate Telegram replies (e.g., LLM quota exceeded → "Service is busy, please try again shortly.").
- Use the `Result<T>` pattern (or `OneOf`) in the Application layer to surface domain errors without exceptions.

## Performance Optimization
- Use `async/await` for all I/O — Telegram API calls, HTTP LLM requests, EF Core queries.
- Cache user settings and frequently-read reference data with `IDistributedCache` (Redis preferred in production, in-memory for dev).
- Use `DbContextPool` (via `AddDbContextPool`) to reduce DbContext allocation overhead.
- Limit conversation history sent to the LLM (sliding window by token count or message count) to control cost and latency.
- Process independent updates concurrently with `Parallel.ForEachAsync` or `Channel<Update>` where ordering is not required.

## Project Structure Example
```
MyTelegramBot/
├── Bot/
│   ├── Handlers/
│   │   ├── StartCommandHandler.cs
│   │   ├── MessageHandler.cs
│   │   └── CallbackQueryHandler.cs
│   ├── Middleware/
│   │   └── UserRegistrationMiddleware.cs
│   └── UpdateDispatcher.cs
├── Application/
│   ├── Conversations/
│   │   ├── ConversationService.cs
│   │   └── PromptBuilder.cs
│   └── Users/
│       └── UserService.cs
├── Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Conversation.cs
│   │   └── Message.cs
│   └── Repositories/
│       ├── IUserRepository.cs
│       └── IConversationRepository.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   └── ConversationConfiguration.cs
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       └── ConversationRepository.cs
│   └── LlmApi/
│       ├── ILlmClient.cs
│       ├── LlmClient.cs
│       └── Models/
│           ├── ChatRequest.cs
│           └── ChatResponse.cs
├── Worker/
│   └── BotPollingWorker.cs
├── appsettings.json
└── Program.cs
```

## Dependency Injection & Configuration
- Wire everything in `Program.cs` using `WebApplication.CreateBuilder` (webhook mode) or `Host.CreateApplicationBuilder` (polling mode).
- Use `services.AddOptions<BotOptions>().BindConfiguration("Bot").ValidateDataAnnotations().ValidateOnStart()`.
- Register handlers with `services.AddScoped<IUpdateHandler, StartCommandHandler>()` etc., then inject `IEnumerable<IUpdateHandler>` into the dispatcher.

## Security
- Store sensitive values (bot token, LLM API key) in environment variables or a secrets manager — never in `appsettings.json` committed to source control.
- Validate that incoming webhook requests originate from Telegram (secret token header).
- Sanitize all user input before inserting into prompts to prevent prompt injection.
- Apply per-user rate limiting (sliding window counter in Redis/cache) to prevent abuse.
- Restrict bot commands to authorized `chat_id` values where the feature requires it.

## Testing
- Use **xUnit** as the test framework.
- Use **NSubstitute** for mocking `ITelegramBotClient`, `ILlmClient`, and repositories.
- Use **Shouldly** for fluent assertions.
- Use `Microsoft.EntityFrameworkCore.InMemory` or **Testcontainers** (PostgreSQL) for integration tests.
- Test each handler in isolation by constructing it with mocked dependencies.
- Test `LlmClient` against a local stub server (e.g., **WireMock.Net**).

## API Documentation (Webhook Mode)
- If using webhook mode, expose the webhook endpoint via ASP.NET Core minimal API.
- Document the endpoint with Swagger/OpenAPI for operational visibility.
- Protect the webhook endpoint with the Telegram secret-token header check.

---

Adhere to official Microsoft documentation (https://learn.microsoft.com/dotnet), the Telegram Bot API reference (https://core.telegram.org/bots/api), and the Telegram.Bot library documentation (https://telegrambots.github.io/book) for best practices.
