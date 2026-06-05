using System.Net.Http.Headers;
using GoalsBot.Application.Calendar;
using GoalsBot.Application.Goals;
using GoalsBot.Application.Stats;
using GoalsBot.Application.Tasks;
using GoalsBot.Bot;
using GoalsBot.Bot.Conversation;
using GoalsBot.Bot.Handlers;
using GoalsBot.Bot.Middleware;
using GoalsBot.Domain.Repositories;
using GoalsBot.Infrastructure.Configuration;
using GoalsBot.Infrastructure.GoogleCalendarApi;
using GoalsBot.Infrastructure.LlmApi;
using GoalsBot.Infrastructure.Persistence;
using GoalsBot.Infrastructure.Persistence.Repositories;
using GoalsBot.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

// --- Options (validated at startup) --------------------------------------
builder.Services.AddOptions<BotOptions>()
    .Bind(builder.Configuration.GetSection(BotOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<LlmOptions>()
    .Bind(builder.Configuration.GetSection(LlmOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<GoogleCalendarOptions>()
    .Bind(builder.Configuration.GetSection(GoogleCalendarOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// --- Persistence ---------------------------------------------------------
builder.Services.AddDbContextPool<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGoalRepository, GoalRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICalendarSyncRepository, CalendarSyncRepository>();

// --- Cross-cutting -------------------------------------------------------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConversationStateStore, MemoryConversationStateStore>();
builder.Services.AddSingleton<PromptBuilder>();

// --- Telegram bot client (singleton) -------------------------------------
builder.Services.AddHttpClient("Telegram.Bot");
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Telegram.Bot");
    return new TelegramBotClient(opts.Token, httpClient);
});

// --- LLM client with resilience -----------------------------------------
builder.Services.AddHttpClient<ILlmClient, LlmClient>((sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    http.BaseAddress = new Uri(opts.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
})
.AddStandardResilienceHandler(o =>
{
    o.Retry.MaxRetryAttempts = 3;
    o.Retry.UseJitter = true;
    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
});

// --- Google Calendar HTTP clients ---------------------------------------
builder.Services.AddTransient<TokenRefreshHandler>();
builder.Services.AddHttpClient("GoogleAuth");
builder.Services.AddSingleton<IGoogleAccessTokenProvider, GoogleAccessTokenProvider>();
builder.Services.AddHttpClient<IGoogleCalendarClient, GoogleCalendarClient>(http =>
{
    http.BaseAddress = new Uri("https://www.googleapis.com/calendar/v3/");
}).AddHttpMessageHandler<TokenRefreshHandler>();

// --- Application services -----------------------------------------------
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();

// --- Bot pipeline -------------------------------------------------------
builder.Services.AddScoped<UserRegistrationMiddleware>();
builder.Services.AddScoped<UpdateDispatcher>();

// Order matters: state-aware handlers are tried first so a pending flow takes precedence.
builder.Services.AddScoped<EditTaskHandler>();
builder.Services.AddScoped<IUpdateHandler>(sp => sp.GetRequiredService<EditTaskHandler>());
builder.Services.AddScoped<IUpdateHandler, AddGoalHandler>();
builder.Services.AddScoped<IUpdateHandler, CallbackQueryHandler>();
builder.Services.AddScoped<IUpdateHandler, StartCommandHandler>();
builder.Services.AddScoped<IUpdateHandler, TasksHandler>();
builder.Services.AddScoped<IUpdateHandler, DeleteTaskHandler>();
builder.Services.AddScoped<IUpdateHandler, StatsHandler>();
builder.Services.AddScoped<IUpdateHandler, SyncHandler>();

builder.Services.AddHostedService<BotPollingWorker>();

var host = builder.Build();
await host.RunAsync();
