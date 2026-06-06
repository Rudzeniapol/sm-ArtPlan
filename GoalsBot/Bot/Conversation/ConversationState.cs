using GoalsBot.Domain.Enums;

namespace GoalsBot.Bot.Conversation;

public abstract record ConversationState;

public sealed record AwaitingGoalText(DateOnly Date) : ConversationState;

// Edit-wizard states carry the originating date so the "back to tasks" view
// after the wizard ends knows which day to render.
public sealed record AwaitingEditTitle(Guid TaskId, DateOnly Date, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditDescription(Guid TaskId, DateOnly Date, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditPriority(Guid TaskId, DateOnly Date, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditEstimate(Guid TaskId, DateOnly Date, EditDraft Draft) : ConversationState;

public sealed class EditDraft
{
    public string? Title { get; set; }
    public bool TitleSet { get; set; }
    public string? Description { get; set; }
    public bool DescriptionSet { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool EstimateSet { get; set; }
}
