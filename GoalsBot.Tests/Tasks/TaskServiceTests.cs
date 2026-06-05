using GoalsBot.Application.Tasks;
using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Enums;
using GoalsBot.Domain.Repositories;
using NSubstitute;
using Shouldly;

namespace GoalsBot.Tests.Tasks;

public class TaskServiceTests
{
    private static TaskItem MakeTask(Guid? id = null, bool completed = false) => new()
    {
        Id = id ?? Guid.NewGuid(),
        DailyGoalId = Guid.NewGuid(),
        UserId = 1,
        Date = new DateOnly(2026, 6, 6),
        Title = "Original title",
        Description = "Original description",
        Priority = TaskPriority.Medium,
        EstimatedMinutes = 30,
        IsCompleted = completed,
        CompletedAt = completed ? DateTimeOffset.UtcNow : null,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task MarkCompleteAsync_sets_completion_fields_and_saves()
    {
        var task = MakeTask();
        var repo = Substitute.For<ITaskRepository>();
        repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var sut = new TaskService(repo);

        await sut.MarkCompleteAsync(task.Id, CancellationToken.None);

        task.IsCompleted.ShouldBeTrue();
        task.CompletedAt.ShouldNotBeNull();
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkCompleteAsync_is_idempotent_when_already_complete()
    {
        var task = MakeTask(completed: true);
        var original = task.CompletedAt;
        var repo = Substitute.For<ITaskRepository>();
        repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var sut = new TaskService(repo);

        await sut.MarkCompleteAsync(task.Id, CancellationToken.None);

        task.CompletedAt.ShouldBe(original);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_only_overwrites_fields_supplied_in_dto()
    {
        var task = MakeTask();
        var repo = Substitute.For<ITaskRepository>();
        repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var sut = new TaskService(repo);

        var result = await sut.UpdateAsync(task.Id,
            new UpdateTaskDto(Title: "New title", Description: null, Priority: TaskPriority.High, EstimatedMinutes: null),
            CancellationToken.None);

        result.Title.ShouldBe("New title");
        result.Priority.ShouldBe(TaskPriority.High);
        result.Description.ShouldBe("Original description");
        result.EstimatedMinutes.ShouldBe(30);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_throws_TaskNotFound_when_repo_returns_null()
    {
        var repo = Substitute.For<ITaskRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var sut = new TaskService(repo);

        await Should.ThrowAsync<TaskNotFoundException>(() => sut.UpdateAsync(
            Guid.NewGuid(),
            new UpdateTaskDto("x", null, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves()
    {
        var task = MakeTask();
        var repo = Substitute.For<ITaskRepository>();
        repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var sut = new TaskService(repo);

        await sut.DeleteAsync(task.Id, CancellationToken.None);

        await repo.Received(1).RemoveAsync(task, Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_persists_new_task_and_returns_dto()
    {
        var repo = Substitute.For<ITaskRepository>();
        var sut = new TaskService(repo);

        var result = await sut.CreateAsync(
            userId: 7,
            new CreateTaskDto(
                DailyGoalId: Guid.NewGuid(),
                Date: new DateOnly(2026, 6, 6),
                Title: "Test",
                Description: null,
                Priority: TaskPriority.Low,
                EstimatedMinutes: 15),
            CancellationToken.None);

        result.Title.ShouldBe("Test");
        result.UserId.ShouldBe(7);
        result.IsCompleted.ShouldBeFalse();
        await repo.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
