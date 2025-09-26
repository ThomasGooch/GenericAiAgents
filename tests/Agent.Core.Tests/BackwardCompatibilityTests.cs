using Agent.Core.Models;
using Xunit;

namespace Agent.Core.Tests;

/// <summary>
/// Tests to validate backward compatibility of obsolete properties.
/// These tests should generate CS0618 warnings but pass successfully.
/// </summary>
public class BackwardCompatibilityTests
{
    [Fact]
    public void AgentRequest_ObsoleteProperties_ShouldWorkCorrectly()
    {
        // Arrange
        var request = new AgentRequest
        {
            Payload = "test-payload"
        };
        
        // Act & Assert - These should generate CS0618 warnings but work
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(request.RequestId, request.Id);
        Assert.Equal(request.Payload, request.Input);
        // CancellationToken property should be accessible (value type)
        var token = request.CancellationToken;
        Assert.True(true); // Just verify the obsolete property is accessible
#pragma warning restore CS0618 // Type or member is obsolete
    }
    
    [Fact]
    public void AgentResult_ObsoleteProperties_ShouldWorkCorrectly()
    {
        // Arrange
        var successResult = AgentResult.CreateSuccess("test-data");
        var errorResult = AgentResult.CreateError("test-error");
        
        // Act & Assert - These should generate CS0618 warnings but work
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(successResult.IsSuccess, successResult.Success);
        Assert.Equal(successResult.Data, successResult.Output);
        Assert.Equal(successResult.ErrorMessage, successResult.Error);
        
        Assert.Equal(errorResult.IsSuccess, errorResult.Success);
        Assert.Equal(errorResult.Data, errorResult.Output);
        Assert.Equal(errorResult.ErrorMessage, errorResult.Error);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}