using Agent.Configuration;
using Agent.Configuration.Models;

var validator = new ConfigurationValidator();

var config = new AgentSystemConfiguration
{
    AgentSystem = new AgentSystemSettings
    {
        Name = "", // Required field missing
        Version = "", // Required field missing
        Environment = null! // Required field missing
    }
};

var result = await validator.ValidateAsync(config);

Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine("Errors:");
foreach (var error in result.Errors)
{
    Console.WriteLine($"  - {error}");
}

Console.WriteLine("Warnings:");
foreach (var warning in result.Warnings)
{
    Console.WriteLine($"  - {warning}");
}