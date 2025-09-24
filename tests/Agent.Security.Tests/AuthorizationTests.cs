using Agent.Security.Authorization;
using Agent.Security.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Agent.Security.Tests;

public class AuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add authorization services with Agent policies
        services.AddAgentAuthorization();

        _serviceProvider = services.BuildServiceProvider();
        _authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();
    }

    [Fact]
    public async Task AdminUser_ShouldHaveAdminOnlyAccess()
    {
        // Arrange
        var user = CreateUser("admin-user", AgentRoles.Admin);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.AdminOnly);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RegularUser_ShouldNotHaveAdminOnlyAccess()
    {
        // Arrange
        var user = CreateUser("regular-user", AgentRoles.User);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.AdminOnly);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticatedUser_ShouldHaveAuthenticatedUserAccess()
    {
        // Arrange
        var user = CreateUser("any-user", AgentRoles.User);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.AuthenticatedUser);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldNotHaveAuthenticatedUserAccess()
    {
        // Arrange
        var user = new ClaimsPrincipal(); // Empty user (not authenticated)

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.AuthenticatedUser);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ServiceAccount_ShouldHaveServiceAccountAccess()
    {
        // Arrange
        var user = CreateUser("service-user", AgentRoles.Service);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.ServiceAccount);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminUser_ShouldHaveAdminOrServiceAccess()
    {
        // Arrange
        var adminUser = CreateUser("admin-user", AgentRoles.Admin);

        // Act
        var result = await _authorizationService.AuthorizeAsync(adminUser, AuthorizationPolicies.AdminOrService);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ServiceUser_ShouldHaveAdminOrServiceAccess()
    {
        // Arrange
        var serviceUser = CreateUser("service-user", AgentRoles.Service);

        // Act
        var result = await _authorizationService.AuthorizeAsync(serviceUser, AuthorizationPolicies.AdminOrService);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RegularUser_ShouldNotHaveAdminOrServiceAccess()
    {
        // Arrange
        var user = CreateUser("regular-user", AgentRoles.User);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.AdminOrService);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminUser_ShouldHaveWorkflowManagerAccess()
    {
        // Arrange
        var adminUser = CreateUser("admin-user", AgentRoles.Admin);

        // Act
        var result = await _authorizationService.AuthorizeAsync(adminUser, AuthorizationPolicies.WorkflowManager);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UserWithWorkflowPermission_ShouldHaveWorkflowManagerAccess()
    {
        // Arrange
        var user = CreateUserWithPermission("workflow-user", AgentRoles.User, "workflow:manage");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.WorkflowManager);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UserWithoutWorkflowPermission_ShouldNotHaveWorkflowManagerAccess()
    {
        // Arrange
        var user = CreateUser("regular-user", AgentRoles.User);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.WorkflowManager);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AnyAuthenticatedUser_ShouldHaveMetricsViewerAccess()
    {
        // Arrange
        var user = CreateUser("any-user", AgentRoles.User);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.MetricsViewer);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminWithConfigPermission_ShouldHaveSystemConfiguratorAccess()
    {
        // Arrange
        var user = CreateUserWithPermission("config-admin", AgentRoles.Admin, "system:configure");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.SystemConfigurator);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminWithoutConfigPermission_ShouldNotHaveSystemConfiguratorAccess()
    {
        // Arrange
        var user = CreateUser("regular-admin", AgentRoles.Admin);

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, AuthorizationPolicies.SystemConfigurator);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void AgentRoles_ShouldContainExpectedRoles()
    {
        // Assert
        Assert.Contains(AgentRoles.Admin, AgentRoles.AllRoles);
        Assert.Contains(AgentRoles.User, AgentRoles.AllRoles);
        Assert.Contains(AgentRoles.Service, AgentRoles.AllRoles);
        Assert.Equal(3, AgentRoles.AllRoles.Length);
    }

    [Fact]
    public void AuthorizationPolicies_ShouldHaveExpectedPolicyNames()
    {
        // Assert - just verify constants exist
        Assert.NotNull(AuthorizationPolicies.AdminOnly);
        Assert.NotNull(AuthorizationPolicies.AuthenticatedUser);
        Assert.NotNull(AuthorizationPolicies.ServiceAccount);
        Assert.NotNull(AuthorizationPolicies.AdminOrService);
        Assert.NotNull(AuthorizationPolicies.WorkflowManager);
        Assert.NotNull(AuthorizationPolicies.MetricsViewer);
        Assert.NotNull(AuthorizationPolicies.SystemConfigurator);
    }

    private ClaimsPrincipal CreateUser(string userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"User {userId}"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private ClaimsPrincipal CreateUserWithPermission(string userId, string role, string permission)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"User {userId}"),
            new Claim(ClaimTypes.Role, role),
            new Claim("permission", permission)
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

public class AuthorizationHandlerTests
{
    [Fact]
    public async Task PermissionAuthorizationHandler_AdminUser_ShouldSucceed()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var handler = new PermissionAuthorizationHandler(logger);
        var requirement = new PermissionAuthorizationRequirement("test:permission");
        var user = CreateUser("admin", AgentRoles.Admin);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_UserWithPermission_ShouldSucceed()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var handler = new PermissionAuthorizationHandler(logger);
        var requirement = new PermissionAuthorizationRequirement("test:permission");
        var user = CreateUserWithPermission("user", AgentRoles.User, "test:permission");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_UserWithoutPermission_ShouldFail()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
        var handler = new PermissionAuthorizationHandler(logger);
        var requirement = new PermissionAuthorizationRequirement("test:permission");
        var user = CreateUser("user", AgentRoles.User);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ResourceAuthorizationHandler_ResourceOwner_ShouldSucceed()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ResourceAuthorizationHandler>>();
        var handler = new ResourceAuthorizationHandler(logger);
        var requirement = new ResourceAuthorizationRequirement("read");
        var user = CreateUser("user123", AgentRoles.User);
        var resource = new TestResource("res1", "user123", "workflow", false);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ResourceAuthorizationHandler_NonOwner_ShouldFail()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ResourceAuthorizationHandler>>();
        var handler = new ResourceAuthorizationHandler(logger);
        var requirement = new ResourceAuthorizationRequirement("read");
        var user = CreateUser("user123", AgentRoles.User);
        var resource = new TestResource("res1", "otheruser", "workflow", false);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    private ClaimsPrincipal CreateUser(string userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"User {userId}"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private ClaimsPrincipal CreateUserWithPermission(string userId, string role, string permission)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"User {userId}"),
            new Claim(ClaimTypes.Role, role),
            new Claim("permission", permission)
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private class TestResource : IResource
    {
        public string Id { get; }
        public string? OwnerId { get; }
        public string ResourceType { get; }
        public bool AllowServiceAccess { get; }

        public TestResource(string id, string? ownerId, string resourceType, bool allowServiceAccess)
        {
            Id = id;
            OwnerId = ownerId;
            ResourceType = resourceType;
            AllowServiceAccess = allowServiceAccess;
        }
    }
}