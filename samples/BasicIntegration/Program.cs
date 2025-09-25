using Agent.DI;
using Agent.AI.Models;
using Agent.Security;
using Agent.Security.Authentication;
using Agent.Tools.Samples;

var builder = WebApplication.CreateBuilder(args);

// Add standard ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Add GenericAgents services - CORRECT API
builder.Services.AddAgentServices(builder.Configuration);

// Enable automatic tool discovery
builder.Services.AddAgentToolDiscovery();

// Add security services (optional for development)
builder.Services.AddEnvironmentSecretManagement();
builder.Services.AddLocalJwtAuthentication(
    builder.Configuration["JWT:SigningKey"] ?? "development-key-minimum-32-characters"
);

// Register sample tools explicitly (optional - tool discovery will find them)
builder.Services.AddSingleton<FileSystemTool>();
builder.Services.AddSingleton<HttpClientTool>();
builder.Services.AddSingleton<TextManipulationTool>();

// Register our custom services
builder.Services.AddSingleton<CustomerServiceAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Add JWT authentication
app.UseAuthorization();  // Add RBAC authorization
app.MapControllers();

// Add health check endpoints
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    GenericAgents = "Enabled"
}));

// Test endpoint to verify AI service
app.MapGet("/health/ai", async (Agent.AI.IAIService aiService) =>
{
    try
    {
        var isHealthy = await aiService.IsHealthyAsync();
        return isHealthy 
            ? Results.Ok(new { Status = "AI Service is healthy", Timestamp = DateTime.UtcNow })
            : Results.Problem("AI Service is not responding");
    }
    catch (Exception ex)
    {
        return Results.Problem($"AI Service error: {ex.Message}");
    }
});

Console.WriteLine("🚀 GenericAgents Basic Integration Sample");
Console.WriteLine("📊 Swagger UI: https://localhost:5001/swagger");
Console.WriteLine("🔍 Health Check: https://localhost:5001/health");
Console.WriteLine("🤖 AI Health Check: https://localhost:5001/health/ai");

app.Run();