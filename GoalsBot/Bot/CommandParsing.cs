using System.Globalization;

namespace GoalsBot.Bot;

public static class CommandParsing
{
    public static bool TryParseCommand(string? text, string command, out string remainder)
    {
        remainder = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var trimmed = text.Trim();
        if (!trimmed.StartsWith('/')) return false;

        var space = trimmed.IndexOf(' ');
        var head = space < 0 ? trimmed : trimmed[..space];

        // Strip bot mentions like /add@SmartPlanBot
        var at = head.IndexOf('@');
        if (at >= 0) head = head[..at];

        if (!string.Equals(head, command, StringComparison.OrdinalIgnoreCase)) return false;

        remainder = space < 0 ? string.Empty : trimmed[(space + 1)..].Trim();
        return true;
    }

    public static DateOnly ParseDateOrToday(string remainder, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(remainder)) return today;
        return DateOnly.TryParseExact(
            remainder,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed) ? parsed : today;
    }
}
