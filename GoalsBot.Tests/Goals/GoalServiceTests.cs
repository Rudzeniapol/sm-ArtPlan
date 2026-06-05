using GoalsBot.Application.Goals;
using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Enums;
using GoalsBot.Domain.Repositories;
using GoalsBot.Infrastructure.LlmApi;
using GoalsBot.Infrastructure.LlmApi.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace GoalsBot.Tests.Goals;

public class GoalServiceTests
{
    [Fact]
    public async Task ParseAndSaveAsync_persists_goal_and_tasks_and_returns_dto()
    {
        var goals = Substitute.For<IGoalRepository>();
        var tasksRepo = Substitute.For<ITaskRepository>();
        var llm = Substitute.For<ILlmClient>();
        llm.ParseGoalsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ParsedTaskDto>
            {
                new("Buy milk", null, TaskPriority.Low, 5),
                new("Write report", "Quarterly summary", TaskPriority.High, 90)
            });

        var sut = new GoalService(goals, tasksRepo, llm, NullLogger<GoalService>.Instance);

        var result = await sut.ParseAndSaveAsync(
            userId: 42,
            date: new DateOnly(2026, 6, 10),
            rawInput: "I need to buy milk and write the quarterly report.",
            CancellationToken.None);

        result.Tasks.Count.ShouldBe(2);
        result.UserId.ShouldBe(42);
        result.Date.ShouldBe(new DateOnly(2026, 6, 10));
        result.Tasks[0].Title.ShouldBe("Buy milk");
        result.Tasks[1].Priority.ShouldBe(TaskPriority.High);

        await goals.Received(1).AddAsync(Arg.Any<DailyGoal>(), Arg.Any<CancellationToken>());
        await tasksRepo.Received(2).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await goals.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseAndSaveAsync_truncates_overlong_titles_and_descriptions()
    {
        var longTitle = new string('T', 250);
        var longDescription = new string('D', 1500);

        var goals = Substitute.For<IGoalRepository>();
        var tasksRepo = Substitute.For<ITaskRepository>();
        var llm = Substitute.For<ILlmClient>();
        llm.ParseGoalsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ParsedTaskDto>
            {
                new(longTitle, longDescription, TaskPriority.Medium, null)
            });

        var sut = new GoalService(goals, tasksRepo, llm, NullLogger<GoalService>.Instance);

        var result = await sut.ParseAndSaveAsync(1, new DateOnly(2026, 6, 6), "...", CancellationToken.None);

        result.Tasks[0].Title.Length.ShouldBe(200);
        result.Tasks[0].Description!.Length.ShouldBe(1000);
    }

    [Fact]
    public async Task ParseAndSaveAsync_throws_when_raw_input_is_blank()
    {
        var sut = new GoalService(
            Substitute.For<IGoalRepository>(),
            Substitute.For<ITaskRepository>(),
            Substitute.For<ILlmClient>(),
            NullLogger<GoalService>.Instance);

        await Should.ThrowAsync<ArgumentException>(() =>
            sut.ParseAndSaveAsync(1, new DateOnly(2026, 6, 6), "   ", CancellationToken.None));
    }
}
