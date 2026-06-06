namespace GoalsBot.Application.Calendar;

public sealed class CalendarNotConfiguredException()
    : InvalidOperationException("Google Calendar credentials are not configured.");
