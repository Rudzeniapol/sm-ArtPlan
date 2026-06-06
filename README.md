# smArtPlan

A Telegram bot that turns free-form daily-goal descriptions into structured tasks
via an LLM, lets you complete/edit/delete them, summarizes your progress, and
optionally syncs each day to a Google Calendar all-day event.

Stack: .NET 9 worker, EF Core + PostgreSQL, Telegram.Bot, any OpenAI-compatible
chat-completions endpoint, Google Calendar (optional).

---

## Quick start (Docker, recommended)

You need Docker + Docker Compose, and one Telegram bot token + one LLM API key.

```bash
git clone <this repo> && cd smArtPlan
cp .env.example .env
# Edit .env and fill in BOT_TOKEN and LLM_API_KEY (at minimum).
docker compose up --build
```

First boot: the bot container applies EF Core migrations against Postgres,
then starts long-polling Telegram. Send `/start` to your bot in Telegram.

Stop with `Ctrl+C`. Data persists in the `pgdata` Docker volume — `docker compose
down -v` if you want to wipe it.

---

## Where do my keys go?

Every secret lives in a single `.env` file at the repo root (gitignored).
`docker compose` reads it automatically and forwards values to the bot
as environment variables.

| Variable | Required | What it is |
| --- | --- | --- |
| `BOT_TOKEN` | yes | Talk to [@BotFather](https://t.me/BotFather), `/newbot`, copy the token |
| `LLM_API_KEY` | yes | API key from your LLM provider (see below) |
| `LLM_BASE_URL` | yes | OpenAI-compatible base URL, **must end with `/`** |
| `LLM_MODEL` | yes | Model id at that provider, e.g. `gpt-4o-mini` |
| `POSTGRES_PASSWORD` | recommended | Change from `changeme` |
| `BOT_DEFAULT_TIMEZONE` | optional | IANA tz id for new users, e.g. `Europe/Kyiv` |
| `GOOGLE_CALENDAR_ID` | optional | Defaults to `primary` |

LLM providers known to work because they're OpenAI-compatible:

| Provider | `LLM_BASE_URL` | Get a key at |
| --- | --- | --- |
| OpenAI | `https://api.openai.com/v1/` | platform.openai.com |
| DeepSeek | `https://api.deepseek.com/v1/` | platform.deepseek.com |
| Groq | `https://api.groq.com/openai/v1/` | console.groq.com |
| Anything else OpenAI-compatible | — | — |

### Optional: Google Calendar (`/sync`)

1. Create a Google Cloud project, enable the **Google Calendar API**.
2. Create a **service account**, give it a JSON key, download the key file.
3. Save the file as `./secrets/google-credentials.json`.
4. In Google Calendar, share the target calendar with the service-account
   email (`...@<project>.iam.gserviceaccount.com`) with edit access.
   For your **primary** calendar, you'll need to create a dedicated calendar
   first or use a non-primary calendar — service accounts cannot impersonate
   you on your primary calendar without domain-wide delegation.
5. If you used a non-primary calendar, set `GOOGLE_CALENDAR_ID=<calendar id>`
   in `.env`.
6. Restart: `docker compose up -d --build`.

If you skip this section, `/sync` simply replies "Google Calendar isn't
configured" and the rest of the bot keeps working.

---

## Bot commands

| Command | What it does |
| --- | --- |
| `/start` | Register you and show the command list |
| `/add` or `/add YYYY-MM-DD` | Prompt for goals → LLM parses → save tasks |
| `/tasks` or `/tasks YYYY-MM-DD` | Show tasks with action buttons |
| `/edit {taskId}` | Step through title → description → priority → estimate |
| `/delete {taskId}` | Confirm and delete a task |
| `/stats week` or `/stats month` | Completion rate, totals by priority, streak |
| `/sync` or `/sync YYYY-MM-DD` | Push the day's tasks to Google Calendar |

You can also tap inline buttons on the `/tasks` list to complete/edit/delete
individual tasks or sync the whole day.

---

## Running locally without Docker

You need .NET 9 SDK and a running Postgres.

```bash
# 1. Start Postgres however you like (e.g. docker compose up -d postgres).
# 2. Set environment variables, or put them in dotnet user-secrets:
cd GoalsBot
dotnet user-secrets set "Bot:Token"        "<your bot token>"
dotnet user-secrets set "Llm:ApiKey"       "<your llm api key>"
dotnet user-secrets set "Llm:BaseUrl"      "https://api.openai.com/v1/"
dotnet user-secrets set "Llm:Model"        "gpt-4o-mini"
dotnet user-secrets set "ConnectionStrings:Default" \
    "Host=localhost;Port=5432;Database=goalsbot;Username=goalsbot;Password=changeme"

# 3. Run — migrations apply automatically on startup.
dotnet run
```

To regenerate migrations:

```bash
dotnet tool install -g dotnet-ef    # one-time
dotnet ef migrations add <Name> --project GoalsBot --output-dir Infrastructure/Persistence/Migrations
```

---

## Tests

```bash
dotnet test
```

19 unit tests cover `GoalService`, `TaskService`, and `LlmClient`
(HTTP mocked via a stub `HttpMessageHandler`).

---

## Architecture

```
GoalsBot/
├── Bot/                      # Update routing, command handlers, conversation state
├── Application/              # Use-case services + DTOs
├── Domain/                   # Entities, enums, repository interfaces
├── Infrastructure/
│   ├── Persistence/          # AppDbContext, IEntityTypeConfiguration<T>, repos, migrations
│   ├── LlmApi/               # ILlmClient + LlmClient + source-gen JSON
│   ├── GoogleCalendarApi/    # Service-account JWT, TokenRefreshHandler, calendar client
│   └── Configuration/        # Strongly-typed Options classes
└── Worker/BotPollingWorker.cs
```

See `rules.md` for the full coding rules.

---

## Troubleshooting

- **`Conflict: bot is already polling`** — only one bot instance can long-poll
  at a time. Stop any other process / `docker compose down` first.
- **`Bot:Token` validation failure on startup** — `.env` isn't being read.
  Make sure you're running `docker compose` from the repo root (where `.env`
  lives), not from a subdirectory.
- **`relation "users" does not exist`** — migrations didn't apply. Check
  the bot logs; Postgres has to be reachable from the bot container before
  startup (the `depends_on: service_healthy` should handle this).
- **Calendar sync fails with 403** — the service-account email isn't shared
  on the target calendar yet (or you tried to use `primary` without DWD).
