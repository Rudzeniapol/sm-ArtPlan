using GoalsBot.Domain.Enums;

namespace GoalsBot.Bot.Conversation;

public abstract record ConversationState;

public sealed record AwaitingGoalText(DateOnly Date) : ConversationState;

public sealed record AwaitingEditTitle(Guid TaskId, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditDescription(Guid TaskId, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditPriority(Guid TaskId, EditDraft Draft) : ConversationState;
public sealed record AwaitingEditEstimate(Guid TaskId, EditDraft Draft) : ConversationState;

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
