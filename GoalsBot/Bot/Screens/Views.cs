using System.Globalization;
using System.Text;
using GoalsBot.Application.Stats;
using GoalsBot.Application.Tasks;
using GoalsBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot.Screens;

public static class Views
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---- Main menu ----------------------------------------------------------
    public static (string Text, InlineKeyboardMarkup Markup) MainMenu() => (
        "🏠 Main menu\nPick what you want to do:",
        new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📝 Add goals",   Cb.MenuAdd)   },
            new[] { InlineKeyboardButton.WithCallbackData("📋 Tasks",       Cb.MenuTasks) },
            new[] { InlineKeyboardButton.WithCallbackData("📊 Stats",       Cb.MenuStats) },
            new[] { InlineKeyboardButton.WithCallbackData("📅 Sync calendar", Cb.MenuSync) }
        }));

    // ---- Date picker --------------------------------------------------------
    // 7 buttons starting tomorrow, two per row, plus a Back button.
    public static (string Text, InlineKeyboardMarkup Markup) DatePicker(string kind, DateOnly today)
    {
        var prefix = kind switch
        {
            "a" => Cb.PickAddPrefix,
            "t" => Cb.PickTasksPrefix,
            "s" => Cb.PickSyncPrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var heading = kind switch
        {
            "a" => "📝 Add goals for…",
            "t" => "📋 Show tasks for…",
            "s" => "📅 Sync calendar for…",
            _ => "Pick a date:"
        };

        var dates = Enumerable.Range(1, 7).Select(today.AddDays).ToArray();
        var buttons = dates
            .Select(d => InlineKeyboardButton.WithCallbackData(
                FormatDayLabel(d),
                prefix + d.ToString("yyyy-MM-dd", Inv)))
            .ToArray();

        // 2-wide rows; last row may have a single button if count is odd.
        var rows = new List<InlineKeyboardButton[]>();
        for (var i = 0; i < buttons.Length; i += 2)
        {
            rows.Add(i + 1 < buttons.Length
                ? [buttons[i], buttons[i + 1]]
                : [buttons[i]]);
        }
        rows.Add([InlineKeyboardButton.WithCallbackData("← Back", Cb.PickCancel)]);

        return (heading, new InlineKeyboardMarkup(rows));
    }

    // ---- Goal-text prompt (for /add after date picked) ----------------------
    public static (string Text, InlineKeyboardMarkup Markup) GoalPrompt(DateOnly date) => (
        $"📝 Tell me your goals for {date.ToString("yyyy-MM-dd", Inv)}. Write freely — I'll structure them into tasks.",
        new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("← Back", Cb.Menu) }
        }));

    // ---- "Analyzing…" ephemeral message --------------------------------------
    public const string AnalyzingText = "🔄 Analyzing your goals…";

    // ---- Tasks list ---------------------------------------------------------
    public static (string Text, InlineKeyboardMarkup Markup) TasksList(DateOnly date, IReadOnlyList<TaskDto> tasks)
    {
        var iso = date.ToString("yyyy-MM-dd", Inv);
        var sb = new StringBuilder();

        if (tasks.Count == 0)
        {
            sb.Append("📋 No tasks for ").Append(iso).Append(" yet.");
        }
        else
        {
            sb.Append("📋 Tasks for ").Append(iso).Append('\n');
            for (var i = 0; i < tasks.Count; i++)
                sb.Append('\n').Append(FormatTaskLine(i + 1, tasks[i]));
        }

        var rows = new List<InlineKeyboardButton[]>();

        // Only incomplete tasks get action buttons. This keeps the keyboard
        // visually clean once an item is done and removes the inconsistency
        // where the title-toggle disappeared but edit/delete remained.
        foreach (var t in tasks.Where(t => !t.IsCompleted))
        {
            var label = $"◻️ {TruncateForButton(t.Title, 40)}";
            // Row 1: full-width title-toggle (single button → spans the whole keyboard).
            rows.Add([InlineKeyboardButton.WithCallbackData(label, Cb.CompletePrefix + t.Id + ":" + iso)]);
            // Row 2: edit + delete share that row 50/50.
            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData("✏️ Edit",   Cb.EditPrefix   + t.Id + ":" + iso),
                InlineKeyboardButton.WithCallbackData("🗑 Delete", Cb.DeletePrefix + t.Id + ":" + iso)
            ]);
        }

        rows.Add([InlineKeyboardButton.WithCallbackData("📅 Sync to Calendar", Cb.PickSyncPrefix + iso)]);
        rows.Add([InlineKeyboardButton.WithCallbackData("← Back to menu", Cb.Menu)]);

        return (sb.ToString(), new InlineKeyboardMarkup(rows));
    }

    // ---- Stats menu --------------------------------------------------------
    public static (string Text, InlineKeyboardMarkup Markup) StatsMenu() => (
        "📊 Stats\nPick a period:",
        new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Week", Cb.StatsWeek),
                InlineKeyboardButton.WithCallbackData("Month", Cb.StatsMonth)
            },
            new[] { InlineKeyboardButton.WithCallbackData("← Back", Cb.Menu) }
        }));

    public static (string Text, InlineKeyboardMarkup Markup) Stats(StatsDto s)
    {
        var sb = new StringBuilder();
        sb.Append("📊 Stats (").Append(s.Period).Append(", ")
          .Append(s.FromInclusive.ToString("yyyy-MM-dd", Inv)).Append(" → ")
          .Append(s.ToInclusive.ToString("yyyy-MM-dd", Inv)).Append(")\n\n");
        sb.Append("📝 Total: ").Append(s.TotalTasks).Append('\n');
        sb.Append("✅ Completed: ").Append(s.CompletedTasks)
          .Append(" (").Append(s.CompletionRate.ToString("0.0", Inv)).Append("%)\n");
        sb.Append("⏱ Estimated: ").Append(s.TotalEstimatedMinutes).Append(" min")
          .Append("  |  Completed: ").Append(s.TotalCompletedMinutes).Append(" min\n");
        sb.Append("⚖️ By priority:\n");
        foreach (var kv in s.TasksByPriority.OrderByDescending(kv => kv.Key))
            sb.Append("  • ").Append(kv.Key).Append(": ").Append(kv.Value).Append('\n');
        sb.Append("🔥 Streak: ").Append(s.StreakDays).Append(" day(s)");

        var markup = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Week", Cb.StatsWeek),
                InlineKeyboardButton.WithCallbackData("Month", Cb.StatsMonth)
            },
            new[] { InlineKeyboardButton.WithCallbackData("← Back", Cb.Menu) }
        });
        return (sb.ToString(), markup);
    }

    // ---- Confirmation prompts -----------------------------------------------
    public static (string Text, InlineKeyboardMarkup Markup) Confirm(
        string question, string confirmLabel, string confirmCallback, string cancelCallback) =>
    (
        question,
        new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(confirmLabel, confirmCallback),
                InlineKeyboardButton.WithCallbackData("Cancel", cancelCallback)
            }
        })
    );

    public static (string Text, InlineKeyboardMarkup Markup) PriorityPicker() => (
        "Pick a priority:",
        new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Low",    Cb.PriorityPrefix + "Low"),
                InlineKeyboardButton.WithCallbackData("Medium", Cb.PriorityPrefix + "Medium"),
                InlineKeyboardButton.WithCallbackData("High",   Cb.PriorityPrefix + "High")
            },
            new[] { InlineKeyboardButton.WithCallbackData("Skip", Cb.PriorityPrefix + "skip") }
        }));

    // ---- Helpers ------------------------------------------------------------
    private static string FormatTaskLine(int index, TaskDto t)
    {
        var box = t.IsCompleted ? "✅" : "◻️";
        var line = $"{index}. {box} {t.Title} ({t.Priority})";
        if (t.EstimatedMinutes is { } m) line += $" — {m} min";
        return line;
    }

    private static string FormatDayLabel(DateOnly d) =>
        d.ToString("ddd MMM dd", Inv);

    private static string TruncateForButton(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
