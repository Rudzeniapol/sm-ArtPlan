using System.Net;
using GoalsBot.Application.Goals;
using GoalsBot.Domain.Enums;
using GoalsBot.Infrastructure.Configuration;
using GoalsBot.Infrastructure.LlmApi;
using GoalsBot.Infrastructure.LlmApi.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace GoalsBot.Tests.Llm;

public class LlmClientTests
{
    private static LlmClient BuildClient(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/v1/") },
            Options.Create(new LlmOptions { BaseUrl = "https://api.example.com/v1/", ApiKey = "k", Model = "test-model" }),
            new PromptBuilder(),
            NullLogger<LlmClient>.Instance);

    private static string BuildChatResponseJson(string content)
    {
        // The model's reply lives in choices[0].message.content as a JSON-encoded string.
        var escapedContent = System.Text.Json.JsonSerializer.Serialize(content);
        return "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":" + escapedContent + "}}]}";
    }

    [Fact]
    public async Task ParseGoalsAsync_deserializes_tasks_when_response_is_plain_json_array()
    {
        var inner = """[{"title":"Buy milk","description":null,"priority":"Low","estimatedMinutes":5}]""";
        var handler = StubHttpMessageHandler.RespondingWithContent(BuildChatResponseJson(inner));
        var sut = BuildClient(handler);

        var result = await sut.ParseGoalsAsync("buy milk", CancellationToken.None);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Buy milk");
        result[0].Priority.ShouldBe(TaskPriority.Low);
        result[0].EstimatedMinutes.ShouldBe(5);
    }

    [Fact]
    public async Task ParseGoalsAsync_strips_markdown_fences_before_parsing()
    {
        var inner = """
            ```json
            [{"title":"Run","description":null,"priority":"High","estimatedMinutes":30}]
            ```
            """;
        var handler = StubHttpMessageHandler.RespondingWithContent(BuildChatResponseJson(inner));
        var sut = BuildClient(handler);

        var result = await sut.ParseGoalsAsync("go for a run", CancellationToken.None);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Run");
        result[0].Priority.ShouldBe(TaskPriority.High);
    }

    [Fact]
    public async Task ParseGoalsAsync_returns_empty_list_when_choices_array_is_empty()
    {
        var responseJson = """{"choices":[]}""";
        var handler = StubHttpMessageHandler.RespondingWithContent(responseJson);
        var sut = BuildClient(handler);

        var result = await sut.ParseGoalsAsync("anything", CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ParseGoalsAsync_throws_when_inner_json_is_malformed()
    {
        var inner = "not valid json";
        var handler = StubHttpMessageHandler.RespondingWithContent(BuildChatResponseJson(inner));
        var sut = BuildClient(handler);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.ParseGoalsAsync("anything", CancellationToken.None));
    }

    [Fact]
    public async Task ParseGoalsAsync_sends_request_with_model_max_tokens_and_two_messages()
    {
        var inner = "[]";
        var handler = StubHttpMessageHandler.RespondingWithContent(BuildChatResponseJson(inner));
        var sut = BuildClient(handler);

        await sut.ParseGoalsAsync("hello", CancellationToken.None);

        var sent = handler.Requests.ShouldHaveSingleItem();
        sent.Method.ShouldBe(HttpMethod.Post);
        sent.RequestUri!.AbsoluteUri.ShouldEndWith("/v1/chat/completions");

        var capturedBody = handler.CapturedBodies.ShouldHaveSingleItem();
        var body = System.Text.Json.JsonSerializer.Deserialize(capturedBody, LlmJsonContext.Default.ChatRequest);
        body.ShouldNotBeNull();
        body!.Model.ShouldBe("test-model");
        body.MaxTokens.ShouldBe(1000);
        body.Messages.Count.ShouldBe(2);
        body.Messages[0].Role.ShouldBe("system");
        body.Messages[1].Role.ShouldBe("user");
        body.Messages[1].Content.ShouldBe("hello");
    }

    [Fact]
    public async Task ParseGoalsAsync_throws_on_5xx_response()
    {
        var handler = StubHttpMessageHandler.RespondingWithContent("oops", HttpStatusCode.InternalServerError);
        var sut = BuildClient(handler);

        await Should.ThrowAsync<HttpRequestException>(() =>
            sut.ParseGoalsAsync("hi", CancellationToken.None));
    }

    [Theory]
    [InlineData("```\n[]\n```", "[]")]
    [InlineData("```json\n[\"a\"]\n```", "[\"a\"]")]
    [InlineData("[]", "[]")]
    [InlineData("  \n[]\n  ", "[]")]
    public void StripCodeFences_handles_common_shapes(string input, string expected)
    {
        LlmClient.StripCodeFences(input).ShouldBe(expected);
    }
}
