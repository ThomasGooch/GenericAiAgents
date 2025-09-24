using Agent.AI;
using Agent.Core;
using Agent.DI;
using Agent.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Agent.DI.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAgentCore_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();

        services.AddAgentCore();

        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.IsType<ToolRegistry>(serviceProvider.GetService<IToolRegistry>());
    }

    [Fact]
    public void AddAgentAI_ShouldRegisterAIServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAgentAI();

        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IAIService>());
        Assert.IsType<SemanticKernelAIService>(serviceProvider.GetService<IAIService>());
    }

    [Fact]
    public void AddAgentToolRegistry_ShouldRegisterAssingleton()
    {
        var services = new ServiceCollection();

        services.AddAgentToolRegistry();

        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IToolRegistry));
        
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        Assert.Equal(typeof(ToolRegistry), serviceDescriptor.ImplementationType);
    }

    [Fact]
    public void AddAgentServices_ShouldRegisterAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAgentServices();

        var serviceProvider = services.BuildServiceProvider();
        
        // Verify all services are registered
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.NotNull(serviceProvider.GetService<IAIService>());
    }

    [Fact]
    public void AddAgentServices_WithConfiguration_ShouldRegisterServicesWithConfig()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI",
                ["AI:ModelId"] = "gpt-4",
                ["AI:ApiKey"] = "test-key"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddAgentServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.NotNull(serviceProvider.GetService<IAIService>());
    }

    [Fact]
    public void AddAgentServices_ShouldAllowMultipleRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAgentServices();
        services.AddAgentServices(); // Should not throw

        var serviceProvider = services.BuildServiceProvider();
        
        // Should still work correctly
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.NotNull(serviceProvider.GetService<IAIService>());
    }

    [Fact]
    public void ServiceRegistration_ShouldSupportOptionsPattern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "AzureOpenAI",
                ["AI:Endpoint"] = "https://test.openai.azure.com",
                ["AI:ModelId"] = "gpt-4",
                ["AI:ApiKey"] = "test-key",
                ["AI:MaxTokens"] = "1000"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddAgentServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        
        // Verify services can be resolved
        Assert.NotNull(serviceProvider.GetService<IAIService>());
    }

    [Fact]
    public void ServiceLifetimes_ShouldBeConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAgentServices();

        // Check service lifetimes
        var toolRegistryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IToolRegistry));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));

        Assert.Equal(ServiceLifetime.Singleton, toolRegistryDescriptor?.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, aiServiceDescriptor?.Lifetime);
    }

    [Fact]
    public void ServiceRegistration_ShouldWorkWithExistingServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add some existing services
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddAgentServices();

        var serviceProvider = services.BuildServiceProvider();
        
        // Should resolve both existing and new services
        Assert.NotNull(serviceProvider.GetService<IConfiguration>());
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.NotNull(serviceProvider.GetService<IAIService>());
    }
}