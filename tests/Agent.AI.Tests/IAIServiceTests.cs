using Agent.AI;
using Agent.AI.Models;
using NSubstitute;

namespace Agent.AI.Tests;

public class IAIServiceTests
{
    [Fact]
    public void IAIService_ShouldHaveRequiredMembers()
    {
        // This test verifies the interface contract exists
        var aiService = Substitute.For<IAIService>();

        Assert.NotNull(aiService);
    }

    [Fact]
    public async Task GetCompletionAsync_ShouldAcceptPromptAndReturnResponse()
    {
        var aiService = Substitute.For<IAIService>();
        var response = AIResponse.CreateSuccess("Test response", 50, "gpt-4", TimeSpan.FromSeconds(1));

        aiService.GetCompletionAsync("Test prompt").Returns(response);

        var result = await aiService.GetCompletionAsync("Test prompt");

        Assert.NotNull(result);
        Assert.Equal("Test response", result.Content);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task IsConfiguredAsync_ShouldReturnConfigurationStatus()
    {
        var aiService = Substitute.For<IAIService>();

        aiService.IsConfiguredAsync().Returns(true);

        var result = await aiService.IsConfiguredAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task InitializeAsync_ShouldAcceptConfiguration()
    {
        var aiService = Substitute.For<IAIService>();
        var config = new AIConfiguration { ModelId = "gpt-4" };

        await aiService.InitializeAsync(config);

        await aiService.Received(1).InitializeAsync(config);
    }
}