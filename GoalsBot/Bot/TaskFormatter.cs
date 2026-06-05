using System.Globalization;
using System.Text;
using GoalsBot.Application.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot;

public static class TaskFormatter
{
    public static string FormatList(IReadOnlyList<TaskDto> tasks, DateOnly date)
    {
        if (tasks.Count == 0)
            return $"No tasks for {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.";

        var sb = new StringBuilder();
        sb.AppendLine($"📋 Tasks for {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:");
        for (var i = 0; i < tasks.Count; i++)
            sb.AppendLine(FormatLine(i + 1, tasks[i]));
        return sb.ToString().TrimEnd();
    }

    public static string FormatLine(int index, TaskDto t)
    {
        var box = t.IsCompleted ? "✅" : "◻️";
        var line = $"{index}. {box} {t.Title} ({t.Priority})";
        if (t.EstimatedMinutes is { } m) line += $" — {m} min";
        return line;
    }

    public static InlineKeyboardMarkup BuildTaskKeyboard(IReadOnlyList<TaskDto> tasks, DateOnly date)
    {
        var rows = new List<InlineKeyboardButton[]>();
        foreach (var t in tasks)
        {
            var titleSnippet = Truncate(t.Title, 16);
            var row = new List<InlineKeyboardButton>();
            if (!t.IsCompleted)
                row.Add(InlineKeyboardButton.WithCallbackData($"◻️ {titleSnippet}", $"complete:{t.Id}"));
            row.Add(InlineKeyboardButton.WithCallbackData("✏️", $"edit:{t.Id}"));
            row.Add(InlineKeyboardButton.WithCallbackData("🗑", $"delete:{t.Id}"));
            rows.Add(row.ToArray());
        }
        var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        rows.Add([InlineKeyboardButton.WithCallbackData("📅 Sync to Calendar", $"sync:{iso}")]);
        return new InlineKeyboardMarkup(rows);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
