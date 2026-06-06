namespace GoalsBot.Bot;

// Centralized callback_data prefixes. Telegram caps callback_data at 64 bytes,
// so prefixes stay short. Date format is yyyy-MM-dd (10 chars).
public static class Cb
{
    // Menu
    public const string Menu       = "m";        // root menu
    public const string MenuAdd    = "m:a";      // open add date picker
    public const string MenuTasks  = "m:t";      // open tasks date picker
    public const string MenuSync   = "m:s";      // open sync date picker
    public const string MenuStats  = "m:x";      // open stats menu

    // Stats menu choices
    public const string StatsWeek  = "x:w";
    public const string StatsMonth = "x:m";

    // Date picker results: "p:{kind}:{yyyy-MM-dd}". Kind is one of a/t/s.
    public const string PickAddPrefix   = "p:a:";
    public const string PickTasksPrefix = "p:t:";
    public const string PickSyncPrefix  = "p:s:";
    public const string PickCancel      = "p:c";

    // Tasks view refresh / back-to:    "t:{yyyy-MM-dd}"
    public const string TasksPrefix = "t:";

    // Per-task action: action prefix + {taskId} + ":" + {yyyy-MM-dd}
    public const string CompletePrefix       = "c:";    // show confirm prompt
    public const string CompleteConfirmPrefix = "c+:";  // apply
    public const string CompleteCancelPrefix  = "c-:";  // cancel → back to tasks for date

    public const string DeletePrefix         = "d:";
    public const string DeleteConfirmPrefix  = "d+:";
    public const string DeleteCancelPrefix   = "d-:";

    public const string EditPrefix           = "e:";

    // Priority button during /edit wizard:  "pri:{Low|Medium|High|skip}"
    // The taskId + date live in conversation state.
    public const string PriorityPrefix = "pri:";
}
