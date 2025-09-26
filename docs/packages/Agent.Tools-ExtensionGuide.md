# 🛠️ Agent.Tools: Extending AI Agents with Real-World Capabilities

## What is Agent.Tools?

**Simple explanation:** Agent.Tools provides a framework for creating reusable tools that extend your AI agents with real-world capabilities like file operations, API calls, data processing, and more.

**When to use it:** Every AI agent that needs to perform actions beyond generating text - reading files, calling APIs, processing data, or interacting with external systems.

**Key concepts in plain English:**
- **Tools** are reusable components that perform specific actions (read files, call APIs, process data)
- **Tool Discovery** automatically finds and registers tools so agents can use them
- **Parameter Validation** ensures tools receive correct inputs and handle errors gracefully
- **Tool Results** provide structured output that agents and workflows can use

## From Chatbots to Action-Taking Agents

### The Evolution of AI Capabilities

```
📝 Traditional Chatbot (Text Only):
User: "What's the weather like?"
Bot: "I don't have access to current weather data"

🛠️ Tool-Enabled Agent (Takes Action):
User: "What's the weather like?"
Agent: [Uses weather-api tool] → "It's 72°F and sunny in your location"

User: "Save that to a file"
Agent: [Uses file-system tool] → "Weather data saved to weather-report.txt"
```

### Why Tools Transform AI Agents

```
🤖 Without Tools - Limited to Text:
┌─────────────────────────────────────┐
│  AI Agent                           │
│  ├─ Can understand language         │
│  ├─ Can generate responses          │
│  └─ Cannot take actions             │
└─────────────────────────────────────┘
Result: Smart conversations, no real impact

🔧 With Tools - Connected to Reality:
┌─────────────────────────────────────┐
│  AI Agent + Tools                   │
│  ├─ Understands language            │
│  ├─ Generates responses             │
│  ├─ Reads and writes files          │
│  ├─ Calls APIs                      │
│  ├─ Processes data                  │
│  ├─ Sends emails                    │
│  └─ Interacts with databases        │
└─────────────────────────────────────┘
Result: AI that gets things done in the real world
```

## Tool Development Framework

### Pattern 1: Simple Data Processing Tool
Perfect for text manipulation, calculations, and formatting:

```csharp
using Agent.Tools;
using Agent.Tools.Models;
using System.Text.Json;

[Tool("json-processor")]
[Description("Processes JSON data with validation, formatting, and extraction capabilities")]
public class JsonProcessorTool : BaseTool
{
    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "operation", typeof(string) }, // validate, format, extract, merge
            { "json", typeof(string) },      // JSON data to process
            { "path", typeof(string) },      // JSON path for extraction (optional)
            { "value", typeof(string) }      // Value for merge operations (optional)
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        var operation = parameters["operation"].ToString()!.ToLowerInvariant();
        var jsonText = parameters["json"].ToString()!;
        var path = parameters.GetValueOrDefault("path")?.ToString() ?? "";
        var value = parameters.GetValueOrDefault("value")?.ToString() ?? "";

        try
        {
            return operation switch
            {
                "validate" => await ValidateJsonAsync(jsonText),
                "format" => await FormatJsonAsync(jsonText),
                "extract" => await ExtractFromJsonAsync(jsonText, path),
                "merge" => await MergeJsonAsync(jsonText, path, value),
                _ => ToolResult.CreateError($"Unsupported operation: {operation}. Supported: validate, format, extract, merge")
            };
        }
        catch (JsonException ex)
        {
            return ToolResult.CreateError($"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"JSON processing failed: {ex.Message}");
        }
    }

    private async Task<ToolResult> ValidateJsonAsync(string jsonText)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonText);
            var stats = AnalyzeJsonStructure(document.RootElement);

            var result = new
            {
                Operation = "validate",
                IsValid = true,
                Statistics = stats,
                Message = "JSON is valid and well-formed"
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (JsonException ex)
        {
            var result = new
            {
                Operation = "validate",
                IsValid = false,
                Error = ex.Message,
                Position = ex.BytePositionInLine
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private async Task<ToolResult> FormatJsonAsync(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);
        var formatted = JsonSerializer.Serialize(document, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var result = new
        {
            Operation = "format",
            OriginalSize = jsonText.Length,
            FormattedSize = formatted.Length,
            FormattedJson = formatted
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<ToolResult> ExtractFromJsonAsync(string jsonText, string path)
    {
        using var document = JsonDocument.Parse(jsonText);
        var element = NavigateJsonPath(document.RootElement, path);

        if (!element.HasValue)
        {
            return ToolResult.CreateError($"Path '{path}' not found in JSON");
        }

        var result = new
        {
            Operation = "extract",
            Path = path,
            ValueType = element.Value.ValueKind.ToString(),
            ExtractedValue = element.Value.GetRawText()
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<ToolResult> MergeJsonAsync(string jsonText, string path, string value)
    {
        // Simplified merge implementation
        using var document = JsonDocument.Parse(jsonText);
        var merged = MergeJsonValue(document.RootElement, path, value);

        var result = new
        {
            Operation = "merge",
            Path = path,
            NewValue = value,
            MergedJson = merged
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private JsonElement? NavigateJsonPath(JsonElement element, string path)
    {
        if (string.IsNullOrEmpty(path)) return element;

        var parts = path.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
            {
                current = property;
            }
            else if (current.ValueKind == JsonValueKind.Array && int.TryParse(part, out var index))
            {
                if (index >= 0 && index < current.GetArrayLength())
                {
                    current = current[index];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private object AnalyzeJsonStructure(JsonElement element)
    {
        return new
        {
            Type = element.ValueKind.ToString(),
            Properties = element.ValueKind == JsonValueKind.Object ? element.EnumerateObject().Count() : 0,
            ArrayLength = element.ValueKind == JsonValueKind.Array ? element.GetArrayLength() : 0,
            Depth = CalculateDepth(element, 0)
        };
    }

    private int CalculateDepth(JsonElement element, int currentDepth)
    {
        var maxDepth = currentDepth;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var childDepth = CalculateDepth(property.Value, currentDepth + 1);
                maxDepth = Math.Max(maxDepth, childDepth);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var childDepth = CalculateDepth(item, currentDepth + 1);
                maxDepth = Math.Max(maxDepth, childDepth);
            }
        }

        return maxDepth;
    }

    private string MergeJsonValue(JsonElement element, string path, string value)
    {
        // Simplified implementation - in production, use a proper JSON merge library
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
        // Add merge logic here...
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

### Pattern 2: External API Integration Tool
For connecting agents to web services and APIs:

```csharp
[Tool("weather-api")]
[Description("Gets current weather information for locations using OpenWeatherMap API")]
public class WeatherApiTool : BaseTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherApiTool> _logger;
    private readonly string _apiKey;

    public WeatherApiTool(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherApiTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? throw new ArgumentException("OpenWeatherMap API key not configured");
    }

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "location", typeof(string) },     // City name or coordinates
            { "units", typeof(string) },        // metric, imperial, or kelvin
            { "language", typeof(string) }      // Language for descriptions
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            var location = parameters["location"].ToString()!;
            var units = parameters.GetValueOrDefault("units")?.ToString() ?? "metric";
            var language = parameters.GetValueOrDefault("language")?.ToString() ?? "en";

            _logger.LogInformation("Getting weather for location: {Location}", location);

            // Build API request URL
            var baseUrl = "https://api.openweathermap.org/data/2.5/weather";
            var queryParams = new Dictionary<string, string>
            {
                ["q"] = location,
                ["appid"] = _apiKey,
                ["units"] = units,
                ["lang"] = language
            };

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var requestUrl = $"{baseUrl}?{queryString}";

            // Make API request
            using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = ParseErrorResponse(content);
                return ToolResult.CreateError($"Weather API error: {errorResponse}");
            }

            // Parse and format response
            var weatherData = JsonSerializer.Deserialize<WeatherApiResponse>(content);
            var formattedResult = FormatWeatherResult(weatherData, units);

            _logger.LogInformation("Weather data retrieved successfully for {Location}", location);

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(formattedResult, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["apiResponseTime"] = response.Headers.Date?.ToString() ?? DateTime.UtcNow.ToString(),
                    ["dataSource"] = "OpenWeatherMap",
                    ["requestUrl"] = requestUrl.Replace(_apiKey, "***") // Hide API key in logs
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for weather API");
            return ToolResult.CreateError($"Weather API request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse weather API response");
            return ToolResult.CreateError($"Invalid weather API response: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in weather API tool");
            return ToolResult.CreateError($"Weather lookup failed: {ex.Message}");
        }
    }

    private object FormatWeatherResult(WeatherApiResponse weatherData, string units)
    {
        var temperatureUnit = units switch
        {
            "metric" => "°C",
            "imperial" => "°F",
            _ => "K"
        };

        return new
        {
            Location = new
            {
                Name = weatherData.Name,
                Country = weatherData.Sys.Country,
                Coordinates = new
                {
                    Latitude = weatherData.Coord.Lat,
                    Longitude = weatherData.Coord.Lon
                }
            },
            Weather = new
            {
                Condition = weatherData.Weather.FirstOrDefault()?.Main ?? "Unknown",
                Description = weatherData.Weather.FirstOrDefault()?.Description ?? "No description",
                Temperature = new
                {
                    Current = $"{weatherData.Main.Temp:F1}{temperatureUnit}",
                    FeelsLike = $"{weatherData.Main.FeelsLike:F1}{temperatureUnit}",
                    Min = $"{weatherData.Main.TempMin:F1}{temperatureUnit}",
                    Max = $"{weatherData.Main.TempMax:F1}{temperatureUnit}"
                },
                Humidity = $"{weatherData.Main.Humidity}%",
                Pressure = $"{weatherData.Main.Pressure} hPa",
                WindSpeed = units == "metric" ? $"{weatherData.Wind.Speed} m/s" : $"{weatherData.Wind.Speed} mph",
                Visibility = $"{weatherData.Visibility / 1000.0:F1} km"
            },
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(weatherData.Dt).ToString(),
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunrise).ToString("HH:mm"),
            Sunset = DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunset).ToString("HH:mm")
        };
    }

    private string ParseErrorResponse(string content)
    {
        try
        {
            var errorData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            return errorData.GetValueOrDefault("message")?.ToString() ?? "Unknown API error";
        }
        catch
        {
            return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
        }
    }
}

// Response models for OpenWeatherMap API
public class WeatherApiResponse
{
    public Coord Coord { get; set; } = new();
    public Weather[] Weather { get; set; } = Array.Empty<Weather>();
    public Main Main { get; set; } = new();
    public Wind Wind { get; set; } = new();
    public Sys Sys { get; set; } = new();
    public int Visibility { get; set; }
    public long Dt { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Coord
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class Weather
{
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Main
{
    public double Temp { get; set; }
    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }
    [JsonPropertyName("temp_min")]
    public double TempMin { get; set; }
    [JsonPropertyName("temp_max")]
    public double TempMax { get; set; }
    public int Pressure { get; set; }
    public int Humidity { get; set; }
}

public class Wind
{
    public double Speed { get; set; }
    public int Deg { get; set; }
}

public class Sys
{
    public string Country { get; set; } = string.Empty;
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
}
```

### Pattern 3: Database Integration Tool
For agents that need to query and update databases:

```csharp
[Tool("sql-database")]
[Description("Executes SQL queries against configured databases with safety validation")]
public class SqlDatabaseTool : BaseTool
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SqlDatabaseTool> _logger;
    private readonly SqlSafetyValidator _safetyValidator;

    public SqlDatabaseTool(
        IDbConnectionFactory connectionFactory,
        ILogger<SqlDatabaseTool> logger,
        SqlSafetyValidator safetyValidator)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _safetyValidator = safetyValidator;
    }

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "query", typeof(string) },        // SQL query to execute
            { "database", typeof(string) },     // Database connection name
            { "parameters", typeof(string) },   // JSON string of parameters
            { "maxRows", typeof(string) }       // Maximum rows to return
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            var query = parameters["query"].ToString()!;
            var database = parameters.GetValueOrDefault("database")?.ToString() ?? "default";
            var parametersJson = parameters.GetValueOrDefault("parameters")?.ToString() ?? "{}";
            var maxRowsStr = parameters.GetValueOrDefault("maxRows")?.ToString() ?? "100";

            if (!int.TryParse(maxRowsStr, out var maxRows))
                maxRows = 100;

            // Safety validation
            var safetyResult = await _safetyValidator.ValidateQueryAsync(query);
            if (!safetyResult.IsSafe)
            {
                return ToolResult.CreateError($"SQL query failed safety validation: {string.Join(", ", safetyResult.Issues)}");
            }

            // Parse parameters
            var sqlParameters = ParseSqlParameters(parametersJson);

            _logger.LogInformation("Executing SQL query on database: {Database}", database);
            _logger.LogDebug("SQL Query: {Query}", query);

            using var connection = await _connectionFactory.CreateConnectionAsync(database);
            await connection.OpenAsync(cancellationToken);

            // Determine query type
            var queryType = DetermineQueryType(query);
            var result = queryType switch
            {
                SqlQueryType.Select => await ExecuteSelectQueryAsync(connection, query, sqlParameters, maxRows, cancellationToken),
                SqlQueryType.Insert or SqlQueryType.Update or SqlQueryType.Delete => await ExecuteNonQueryAsync(connection, query, sqlParameters, cancellationToken),
                _ => ToolResult.CreateError("Unsupported SQL query type")
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database query execution failed");
            return ToolResult.CreateError($"Database operation failed: {ex.Message}");
        }
    }

    private async Task<ToolResult> ExecuteSelectQueryAsync(
        IDbConnection connection, 
        string query, 
        Dictionary<string, object> parameters, 
        int maxRows,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = 30; // 30 second timeout

        // Add parameters
        foreach (var param in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = param.Key;
            parameter.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<Dictionary<string, object>>();
        var columns = new List<string>();

        // Get column names
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        // Read data
        int rowCount = 0;
        while (await reader.ReadAsync(cancellationToken) && rowCount < maxRows)
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value is DBNull ? null : value;
            }
            results.Add(row);
            rowCount++;
        }

        var response = new
        {
            QueryType = "SELECT",
            ColumnCount = columns.Count,
            RowCount = results.Count,
            MaxRowsReached = rowCount >= maxRows,
            Columns = columns,
            Data = results,
            ExecutedAt = DateTime.UtcNow
        };

        return ToolResult.CreateSuccess(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }),
            new Dictionary<string, object>
            {
                ["rowsReturned"] = results.Count,
                ["columnsReturned"] = columns.Count,
                ["truncated"] = rowCount >= maxRows
            });
    }

    private async Task<ToolResult> ExecuteNonQueryAsync(
        IDbConnection connection, 
        string query, 
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.Transaction = transaction;
            command.CommandTimeout = 30;

            // Add parameters
            foreach (var param in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = param.Key;
                parameter.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = new
            {
                QueryType = DetermineQueryType(query).ToString(),
                RowsAffected = rowsAffected,
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };

            return ToolResult.CreateSuccess(
                JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["rowsAffected"] = rowsAffected
                });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private Dictionary<string, object> ParseSqlParameters(string parametersJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private SqlQueryType DetermineQueryType(string query)
    {
        var trimmed = query.TrimStart().ToUpperInvariant();
        return trimmed switch
        {
            var q when q.StartsWith("SELECT") => SqlQueryType.Select,
            var q when q.StartsWith("INSERT") => SqlQueryType.Insert,
            var q when q.StartsWith("UPDATE") => SqlQueryType.Update,
            var q when q.StartsWith("DELETE") => SqlQueryType.Delete,
            _ => SqlQueryType.Unknown
        };
    }
}

public enum SqlQueryType
{
    Unknown,
    Select,
    Insert,
    Update,
    Delete
}

public class SqlSafetyValidator
{
    private readonly string[] _dangerousKeywords = 
    {
        "DROP", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE", "sp_",
        "xp_", "SHUTDOWN", "GRANT", "REVOKE", "BACKUP", "RESTORE"
    };

    public async Task<SqlSafetyResult> ValidateQueryAsync(string query)
    {
        var issues = new List<string>();
        var upperQuery = query.ToUpperInvariant();

        // Check for dangerous keywords
        foreach (var keyword in _dangerousKeywords)
        {
            if (upperQuery.Contains(keyword))
            {
                issues.Add($"Contains potentially dangerous keyword: {keyword}");
            }
        }

        // Check for multiple statements (basic check)
        if (query.Count(c => c == ';') > 1)
        {
            issues.Add("Multiple SQL statements detected - only single statements are allowed");
        }

        return new SqlSafetyResult
        {
            IsSafe = !issues.Any(),
            Issues = issues
        };
    }
}

public class SqlSafetyResult
{
    public bool IsSafe { get; set; }
    public List<string> Issues { get; set; } = new();
}
```

## Advanced Tool Patterns

### Pattern 4: Streaming Data Tool
For real-time data processing and large datasets:

```csharp
[Tool("data-stream-processor")]
[Description("Processes streaming data with real-time filtering, transformation, and aggregation")]
public class DataStreamProcessorTool : BaseTool
{
    private readonly ILogger<DataStreamProcessorTool> _logger;
    private readonly ConcurrentDictionary<string, StreamProcessor> _activeStreams = new();

    public DataStreamProcessorTool(ILogger<DataStreamProcessorTool> logger)
    {
        _logger = logger;
    }

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "operation", typeof(string) },     // start, stop, status, process
            { "streamId", typeof(string) },      // Unique stream identifier
            { "source", typeof(string) },        // Data source configuration
            { "filters", typeof(string) },       // JSON filters configuration
            { "transformations", typeof(string) }, // JSON transformations
            { "data", typeof(string) }           // Data to process (for process operation)
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            var operation = parameters["operation"].ToString()!.ToLowerInvariant();
            var streamId = parameters["streamId"].ToString()!;

            return operation switch
            {
                "start" => await StartStreamAsync(streamId, parameters, cancellationToken),
                "stop" => await StopStreamAsync(streamId, cancellationToken),
                "status" => await GetStreamStatusAsync(streamId, cancellationToken),
                "process" => await ProcessDataAsync(streamId, parameters, cancellationToken),
                _ => ToolResult.CreateError($"Unsupported operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream processing operation failed");
            return ToolResult.CreateError($"Stream processing failed: {ex.Message}");
        }
    }

    private async Task<ToolResult> StartStreamAsync(
        string streamId, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (_activeStreams.ContainsKey(streamId))
        {
            return ToolResult.CreateError($"Stream {streamId} is already active");
        }

        var source = parameters.GetValueOrDefault("source")?.ToString() ?? "";
        var filtersJson = parameters.GetValueOrDefault("filters")?.ToString() ?? "{}";
        var transformationsJson = parameters.GetValueOrDefault("transformations")?.ToString() ?? "{}";

        // Parse configuration
        var filters = ParseFilters(filtersJson);
        var transformations = ParseTransformations(transformationsJson);

        // Create stream processor
        var processor = new StreamProcessor(streamId, source, filters, transformations, _logger);
        _activeStreams[streamId] = processor;

        await processor.StartAsync(cancellationToken);

        var result = new
        {
            Operation = "start",
            StreamId = streamId,
            Source = source,
            Status = "active",
            FiltersCount = filters.Count,
            TransformationsCount = transformations.Count,
            StartedAt = DateTime.UtcNow
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<ToolResult> ProcessDataAsync(
        string streamId, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!_activeStreams.TryGetValue(streamId, out var processor))
        {
            return ToolResult.CreateError($"Stream {streamId} is not active");
        }

        var data = parameters.GetValueOrDefault("data")?.ToString() ?? "";
        if (string.IsNullOrEmpty(data))
        {
            return ToolResult.CreateError("No data provided for processing");
        }

        var processedData = await processor.ProcessAsync(data, cancellationToken);

        var result = new
        {
            Operation = "process",
            StreamId = streamId,
            InputSize = data.Length,
            OutputSize = processedData?.Length ?? 0,
            ProcessedAt = DateTime.UtcNow,
            ProcessedData = processedData
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<ToolResult> GetStreamStatusAsync(string streamId, CancellationToken cancellationToken)
    {
        if (!_activeStreams.TryGetValue(streamId, out var processor))
        {
            return ToolResult.CreateError($"Stream {streamId} not found");
        }

        var status = processor.GetStatus();
        return ToolResult.CreateSuccess(JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<ToolResult> StopStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        if (!_activeStreams.TryRemove(streamId, out var processor))
        {
            return ToolResult.CreateError($"Stream {streamId} not found");
        }

        await processor.StopAsync(cancellationToken);

        var result = new
        {
            Operation = "stop",
            StreamId = streamId,
            Status = "stopped",
            StoppedAt = DateTime.UtcNow
        };

        return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private List<DataFilter> ParseFilters(string filtersJson)
    {
        try
        {
            var filtersData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(filtersJson) ?? new();
            return filtersData.Select(f => new DataFilter
            {
                Field = f.GetValueOrDefault("field")?.ToString() ?? "",
                Operator = f.GetValueOrDefault("operator")?.ToString() ?? "equals",
                Value = f.GetValueOrDefault("value")
            }).ToList();
        }
        catch
        {
            return new List<DataFilter>();
        }
    }

    private List<DataTransformation> ParseTransformations(string transformationsJson)
    {
        try
        {
            var transformData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(transformationsJson) ?? new();
            return transformData.Select(t => new DataTransformation
            {
                Type = t.GetValueOrDefault("type")?.ToString() ?? "map",
                Source = t.GetValueOrDefault("source")?.ToString() ?? "",
                Target = t.GetValueOrDefault("target")?.ToString() ?? "",
                Expression = t.GetValueOrDefault("expression")?.ToString() ?? ""
            }).ToList();
        }
        catch
        {
            return new List<DataTransformation>();
        }
    }
}

public class StreamProcessor
{
    private readonly string _streamId;
    private readonly string _source;
    private readonly List<DataFilter> _filters;
    private readonly List<DataTransformation> _transformations;
    private readonly ILogger _logger;
    private readonly StreamStatistics _statistics;
    private bool _isActive;

    public StreamProcessor(
        string streamId, 
        string source, 
        List<DataFilter> filters, 
        List<DataTransformation> transformations, 
        ILogger logger)
    {
        _streamId = streamId;
        _source = source;
        _filters = filters;
        _transformations = transformations;
        _logger = logger;
        _statistics = new StreamStatistics { StartedAt = DateTime.UtcNow };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _isActive = true;
        _statistics.StartedAt = DateTime.UtcNow;
        _logger.LogInformation("Stream processor {StreamId} started", _streamId);
    }

    public async Task<string?> ProcessAsync(string data, CancellationToken cancellationToken)
    {
        if (!_isActive) return null;

        try
        {
            _statistics.TotalProcessed++;
            _statistics.LastProcessedAt = DateTime.UtcNow;

            // Parse input data
            var parsedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            if (parsedData == null) return null;

            // Apply filters
            if (!ApplyFilters(parsedData))
            {
                _statistics.Filtered++;
                return null;
            }

            // Apply transformations
            var transformedData = ApplyTransformations(parsedData);
            
            _statistics.Transformed++;
            return JsonSerializer.Serialize(transformedData);
        }
        catch (Exception ex)
        {
            _statistics.Errors++;
            _logger.LogError(ex, "Error processing data in stream {StreamId}", _streamId);
            return null;
        }
    }

    private bool ApplyFilters(Dictionary<string, object> data)
    {
        return _filters.All(filter => filter.Evaluate(data));
    }

    private Dictionary<string, object> ApplyTransformations(Dictionary<string, object> data)
    {
        var result = new Dictionary<string, object>(data);

        foreach (var transformation in _transformations)
        {
            transformation.Apply(result);
        }

        return result;
    }

    public object GetStatus()
    {
        return new
        {
            StreamId = _streamId,
            IsActive = _isActive,
            Source = _source,
            FiltersCount = _filters.Count,
            TransformationsCount = _transformations.Count,
            Statistics = _statistics
        };
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isActive = false;
        _logger.LogInformation("Stream processor {StreamId} stopped", _streamId);
    }
}

public class DataFilter
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object? Value { get; set; }

    public bool Evaluate(Dictionary<string, object> data)
    {
        if (!data.TryGetValue(Field, out var fieldValue))
            return false;

        return Operator.ToLowerInvariant() switch
        {
            "equals" => fieldValue?.Equals(Value) == true,
            "contains" => fieldValue?.ToString()?.Contains(Value?.ToString() ?? "") == true,
            "greater" => CompareNumbers(fieldValue, Value) > 0,
            "less" => CompareNumbers(fieldValue, Value) < 0,
            _ => false
        };
    }

    private int CompareNumbers(object? left, object? right)
    {
        if (left == null || right == null) return 0;
        if (double.TryParse(left.ToString(), out var leftNum) && 
            double.TryParse(right.ToString(), out var rightNum))
        {
            return leftNum.CompareTo(rightNum);
        }
        return 0;
    }
}

public class DataTransformation
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;

    public void Apply(Dictionary<string, object> data)
    {
        if (Type.ToLowerInvariant() == "map" && data.TryGetValue(Source, out var value))
        {
            data[Target] = TransformValue(value);
        }
    }

    private object TransformValue(object value)
    {
        // Simplified transformation - in production, use a proper expression engine
        return Expression.ToLowerInvariant() switch
        {
            "uppercase" => value?.ToString()?.ToUpperInvariant() ?? "",
            "lowercase" => value?.ToString()?.ToLowerInvariant() ?? "",
            "trim" => value?.ToString()?.Trim() ?? "",
            _ => value
        };
    }
}

public class StreamStatistics
{
    public DateTime StartedAt { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public long TotalProcessed { get; set; }
    public long Transformed { get; set; }
    public long Filtered { get; set; }
    public long Errors { get; set; }
}
```

## Tool Discovery Deep Dive

### Automatic Tool Registration
Understanding how tools are automatically discovered and registered:

```csharp
public class AdvancedToolDiscoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<AdvancedToolDiscoveryService> _logger;
    private readonly ToolDiscoveryOptions _options;

    public AdvancedToolDiscoveryService(
        IServiceProvider serviceProvider,
        IToolRegistry toolRegistry,
        IOptions<ToolDiscoveryOptions> options,
        ILogger<AdvancedToolDiscoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _toolRegistry = toolRegistry;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting advanced tool discovery...");

            // Discover tools from multiple sources
            var discoveredTools = new List<ToolInfo>();

            // 1. Assembly scanning
            if (_options.EnableAssemblyScanning)
            {
                var assemblyTools = await DiscoverFromAssembliesAsync(cancellationToken);
                discoveredTools.AddRange(assemblyTools);
                _logger.LogInformation("Discovered {Count} tools from assemblies", assemblyTools.Count);
            }

            // 2. Plugin directory scanning
            if (_options.EnablePluginScanning && !string.IsNullOrEmpty(_options.PluginDirectory))
            {
                var pluginTools = await DiscoverFromPluginsAsync(_options.PluginDirectory, cancellationToken);
                discoveredTools.AddRange(pluginTools);
                _logger.LogInformation("Discovered {Count} tools from plugins", pluginTools.Count);
            }

            // 3. Configuration-based tools
            if (_options.EnableConfigurationTools)
            {
                var configTools = await DiscoverFromConfigurationAsync(cancellationToken);
                discoveredTools.AddRange(configTools);
                _logger.LogInformation("Discovered {Count} tools from configuration", configTools.Count);
            }

            // Register all discovered tools
            foreach (var toolInfo in discoveredTools)
            {
                await RegisterToolAsync(toolInfo, cancellationToken);
            }

            // Validate tool dependencies
            await ValidateToolDependenciesAsync(cancellationToken);

            _logger.LogInformation("Tool discovery completed. Total tools registered: {TotalCount}", discoveredTools.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool discovery failed");
            throw;
        }
    }

    private async Task<List<ToolInfo>> DiscoverFromAssembliesAsync(CancellationToken cancellationToken)
    {
        var tools = new List<ToolInfo>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var toolTypes = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract)
                    .Where(type => typeof(ITool).IsAssignableFrom(type))
                    .Where(type => type.GetCustomAttribute<ToolAttribute>() != null);

                foreach (var toolType in toolTypes)
                {
                    var toolAttribute = toolType.GetCustomAttribute<ToolAttribute>()!;
                    var descriptionAttribute = toolType.GetCustomAttribute<DescriptionAttribute>();

                    tools.Add(new ToolInfo
                    {
                        Name = toolAttribute.Name,
                        Description = descriptionAttribute?.Description ?? "No description available",
                        Type = toolType,
                        Source = ToolSource.Assembly,
                        AssemblyName = assembly.FullName ?? "Unknown",
                        Capabilities = AnalyzeToolCapabilities(toolType),
                        Dependencies = DiscoverToolDependencies(toolType)
                    });
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning("Could not load types from assembly {AssemblyName}: {Error}", 
                    assembly.FullName, ex.Message);
            }
        }

        return tools;
    }

    private async Task<List<ToolInfo>> DiscoverFromPluginsAsync(string pluginDirectory, CancellationToken cancellationToken)
    {
        var tools = new List<ToolInfo>();

        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {Directory}", pluginDirectory);
            return tools;
        }

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll");

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                var pluginTools = await DiscoverFromAssemblyAsync(assembly, ToolSource.Plugin, cancellationToken);
                tools.AddRange(pluginTools);

                _logger.LogDebug("Loaded {Count} tools from plugin: {Plugin}", pluginTools.Count, Path.GetFileName(pluginFile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {Plugin}", pluginFile);
            }
        }

        return tools;
    }

    private async Task<List<ToolInfo>> DiscoverFromConfigurationAsync(CancellationToken cancellationToken)
    {
        var tools = new List<ToolInfo>();
        
        // Load tools from configuration
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        var toolConfigs = configuration.GetSection("Tools:Custom").Get<List<CustomToolConfiguration>>() ?? new();

        foreach (var toolConfig in toolConfigs)
        {
            try
            {
                var toolInfo = await CreateConfigurationBasedToolAsync(toolConfig, cancellationToken);
                if (toolInfo != null)
                {
                    tools.Add(toolInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create configuration-based tool: {ToolName}", toolConfig.Name);
            }
        }

        return tools;
    }

    private async Task RegisterToolAsync(ToolInfo toolInfo, CancellationToken cancellationToken)
    {
        try
        {
            // Create tool instance
            ITool toolInstance;

            if (toolInfo.Type != null)
            {
                // Create from type
                toolInstance = (ITool)ActivatorUtilities.CreateInstance(_serviceProvider, toolInfo.Type);
            }
            else if (toolInfo.Factory != null)
            {
                // Create from factory
                toolInstance = await toolInfo.Factory(cancellationToken);
            }
            else
            {
                _logger.LogError("Cannot create tool {ToolName}: No type or factory specified", toolInfo.Name);
                return;
            }

            // Register with tool registry
            await _toolRegistry.RegisterToolAsync(toolInstance, cancellationToken);

            _logger.LogDebug("Successfully registered tool: {ToolName} from {Source}", 
                toolInfo.Name, toolInfo.Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register tool: {ToolName}", toolInfo.Name);
        }
    }

    private ToolCapabilities AnalyzeToolCapabilities(Type toolType)
    {
        var capabilities = new ToolCapabilities();

        // Check for async operations
        var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        capabilities.SupportsAsync = methods.Any(m => m.ReturnType == typeof(Task) || 
                                                    m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

        // Check for streaming support
        capabilities.SupportsStreaming = methods.Any(m => 
            m.ReturnType.IsGenericType && 
            (m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) ||
             m.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

        // Check for cancellation support
        capabilities.SupportsCancellation = methods.Any(m => 
            m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)));

        // Check for disposable resources
        capabilities.RequiresDisposal = typeof(IDisposable).IsAssignableFrom(toolType) ||
                                      typeof(IAsyncDisposable).IsAssignableFrom(toolType);

        return capabilities;
    }

    private List<ToolDependency> DiscoverToolDependencies(Type toolType)
    {
        var dependencies = new List<ToolDependency>();

        // Analyze constructor parameters
        var constructors = toolType.GetConstructors();
        foreach (var constructor in constructors)
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (parameter.ParameterType != typeof(ILogger) && 
                    parameter.ParameterType != typeof(IServiceProvider))
                {
                    dependencies.Add(new ToolDependency
                    {
                        Type = parameter.ParameterType,
                        Name = parameter.Name ?? "unknown",
                        IsRequired = !parameter.HasDefaultValue
                    });
                }
            }
        }

        return dependencies;
    }

    private async Task ValidateToolDependenciesAsync(CancellationToken cancellationToken)
    {
        var allTools = await _toolRegistry.GetAllToolsAsync(cancellationToken);
        var validationResults = new List<ToolValidationResult>();

        foreach (var tool in allTools)
        {
            var result = await ValidateToolAsync(tool, cancellationToken);
            validationResults.Add(result);

            if (!result.IsValid)
            {
                _logger.LogWarning("Tool validation failed: {ToolName} - {Issues}", 
                    tool.Name, string.Join(", ", result.Issues));
            }
        }

        var failedTools = validationResults.Where(r => !r.IsValid).ToList();
        if (failedTools.Any())
        {
            _logger.LogWarning("Tool validation completed with {FailedCount} validation failures", failedTools.Count);
        }
        else
        {
            _logger.LogInformation("All tools passed validation");
        }
    }

    private async Task<ToolValidationResult> ValidateToolAsync(ITool tool, CancellationToken cancellationToken)
    {
        var result = new ToolValidationResult { ToolName = tool.Name, IsValid = true };

        try
        {
            // Test parameter schema
            var schema = tool.GetParameterSchema();
            if (!schema.Any())
            {
                result.Issues.Add("No parameter schema defined");
            }

            // Test parameter validation
            var testParams = new Dictionary<string, object>();
            if (!tool.ValidateParameters(testParams) && schema.Any())
            {
                // This is expected if required parameters exist
            }

            // Test with empty parameters (should handle gracefully)
            var validationResult = tool.ValidateParameters(new Dictionary<string, object>());
            // Just ensure it doesn't throw an exception

            _logger.LogDebug("Tool validation passed for: {ToolName}", tool.Name);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tool discovery service stopped");
        return Task.CompletedTask;
    }
}

// Supporting classes
public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type? Type { get; set; }
    public Func<CancellationToken, Task<ITool>>? Factory { get; set; }
    public ToolSource Source { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public ToolCapabilities Capabilities { get; set; } = new();
    public List<ToolDependency> Dependencies { get; set; } = new();
}

public class ToolCapabilities
{
    public bool SupportsAsync { get; set; }
    public bool SupportsStreaming { get; set; }
    public bool SupportsCancellation { get; set; }
    public bool RequiresDisposal { get; set; }
}

public class ToolDependency
{
    public Type Type { get; set; } = typeof(object);
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public class ToolValidationResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
}

public enum ToolSource
{
    Assembly,
    Plugin,
    Configuration,
    Runtime
}

public class ToolDiscoveryOptions
{
    public bool EnableAssemblyScanning { get; set; } = true;
    public bool EnablePluginScanning { get; set; } = false;
    public bool EnableConfigurationTools { get; set; } = false;
    public string? PluginDirectory { get; set; }
    public List<string> ExcludedAssemblies { get; set; } = new();
}

public class CustomToolConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
}
```

## Tool Testing Strategies

### Unit Testing Tools
Comprehensive testing approaches for your custom tools:

```csharp
public class ToolTestingFramework
{
    public static async Task<ToolTestResult> TestToolAsync<T>(
        T tool, 
        Dictionary<string, object> parameters,
        ToolTestOptions? options = null) 
        where T : ITool
    {
        options ??= new ToolTestOptions();
        var result = new ToolTestResult { ToolName = tool.Name };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test 1: Parameter schema validation
            result.SchemaValidation = TestParameterSchema(tool);

            // Test 2: Parameter validation
            result.ParameterValidation = TestParameterValidation(tool, parameters);

            // Test 3: Execution test
            if (result.ParameterValidation.Passed)
            {
                result.ExecutionTest = await TestExecutionAsync(tool, parameters, options);
            }

            // Test 4: Error handling
            result.ErrorHandling = await TestErrorHandlingAsync(tool, options);

            // Test 5: Performance test
            if (options.IncludePerformanceTests)
            {
                result.PerformanceTest = await TestPerformanceAsync(tool, parameters, options);
            }

            stopwatch.Stop();
            result.TotalTestTime = stopwatch.Elapsed;
            result.OverallSuccess = result.SchemaValidation.Passed &&
                                  result.ParameterValidation.Passed &&
                                  result.ExecutionTest.Passed &&
                                  result.ErrorHandling.Passed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.TotalTestTime = stopwatch.Elapsed;
            result.OverallSuccess = false;
            result.UnexpectedError = ex.Message;
            return result;
        }
    }

    private static TestResult TestParameterSchema(ITool tool)
    {
        try
        {
            var schema = tool.GetParameterSchema();
            var issues = new List<string>();

            if (!schema.Any())
            {
                issues.Add("No parameter schema defined - tool may not require parameters");
            }

            // Check for common parameter types
            foreach (var param in schema)
            {
                if (param.Value == null)
                {
                    issues.Add($"Parameter '{param.Key}' has null type");
                }
                else if (!IsValidParameterType(param.Value))
                {
                    issues.Add($"Parameter '{param.Key}' has unsupported type: {param.Value.Name}");
                }
            }

            return new TestResult
            {
                Passed = !issues.Any(i => i.Contains("null") || i.Contains("unsupported")),
                Issues = issues,
                Details = $"Schema contains {schema.Count} parameters"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Passed = false,
                Issues = new List<string> { $"Schema test failed: {ex.Message}" }
            };
        }
    }

    private static TestResult TestParameterValidation(ITool tool, Dictionary<string, object> parameters)
    {
        try
        {
            var schema = tool.GetParameterSchema();
            var issues = new List<string>();

            // Test with provided parameters
            var validationResult = tool.ValidateParameters(parameters);
            
            // Test with empty parameters
            var emptyValidation = tool.ValidateParameters(new Dictionary<string, object>());

            // Test with invalid parameters
            var invalidParams = new Dictionary<string, object> { ["invalid_param"] = "test" };
            var invalidValidation = tool.ValidateParameters(invalidParams);

            if (schema.Any() && emptyValidation)
            {
                issues.Add("Tool accepts empty parameters but has required schema");
            }

            return new TestResult
            {
                Passed = validationResult,
                Issues = issues,
                Details = $"Parameter validation: provided={validationResult}, empty={emptyValidation}, invalid={invalidValidation}"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Passed = false,
                Issues = new List<string> { $"Parameter validation test failed: {ex.Message}" }
            };
        }
    }

    private static async Task<TestResult> TestExecutionAsync(
        ITool tool, 
        Dictionary<string, object> parameters, 
        ToolTestOptions options)
    {
        try
        {
            using var cts = new CancellationTokenSource(options.ExecutionTimeout);
            var result = await tool.ExecuteAsync(parameters, cts.Token);

            var issues = new List<string>();

            if (result == null)
            {
                issues.Add("ExecuteAsync returned null");
            }
            else
            {
                if (!result.Success && string.IsNullOrEmpty(result.Error))
                {
                    issues.Add("Tool failed but provided no error message");
                }

                if (result.Success && result.Data == null && result.Output == null)
                {
                    issues.Add("Tool succeeded but provided no output data");
                }
            }

            return new TestResult
            {
                Passed = result?.Success == true,
                Issues = issues,
                Details = $"Success: {result?.Success}, HasOutput: {!string.IsNullOrEmpty(result?.Output)}"
            };
        }
        catch (OperationCanceledException)
        {
            return new TestResult
            {
                Passed = false,
                Issues = new List<string> { $"Tool execution exceeded timeout of {options.ExecutionTimeout}" }
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Passed = false,
                Issues = new List<string> { $"Tool execution threw exception: {ex.Message}" }
            };
        }
    }

    private static async Task<TestResult> TestErrorHandlingAsync(ITool tool, ToolTestOptions options)
    {
        var issues = new List<string>();
        var testsPassed = 0;
        var totalTests = 0;

        // Test 1: Invalid parameters
        totalTests++;
        try
        {
            var invalidParams = new Dictionary<string, object> { [""] = null };
            var result = await tool.ExecuteAsync(invalidParams);
            
            if (result != null && !result.Success)
            {
                testsPassed++;
            }
            else
            {
                issues.Add("Tool did not handle invalid parameters properly");
            }
        }
        catch (Exception)
        {
            // Exception is acceptable for invalid parameters
            testsPassed++;
        }

        // Test 2: Cancellation
        totalTests++;
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately
            
            var validParams = tool.GetParameterSchema()
                .ToDictionary(p => p.Key, p => GetDefaultValue(p.Value));
            
            var result = await tool.ExecuteAsync(validParams, cts.Token);
            
            if (result != null && !result.Success && result.Error?.Contains("cancel") == true)
            {
                testsPassed++;
            }
            else
            {
                issues.Add("Tool did not handle cancellation properly");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected for cancellation
            testsPassed++;
        }
        catch (Exception ex)
        {
            issues.Add($"Unexpected exception during cancellation test: {ex.Message}");
        }

        return new TestResult
        {
            Passed = testsPassed == totalTests,
            Issues = issues,
            Details = $"Error handling tests passed: {testsPassed}/{totalTests}"
        };
    }

    private static async Task<TestResult> TestPerformanceAsync(
        ITool tool, 
        Dictionary<string, object> parameters, 
        ToolTestOptions options)
    {
        var issues = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var iterations = options.PerformanceIterations;

        try
        {
            var executionTimes = new List<TimeSpan>();

            for (int i = 0; i < iterations; i++)
            {
                var iterationStopwatch = Stopwatch.StartNew();
                var result = await tool.ExecuteAsync(parameters);
                iterationStopwatch.Stop();
                
                executionTimes.Add(iterationStopwatch.Elapsed);

                if (!result.Success)
                {
                    issues.Add($"Iteration {i + 1} failed: {result.Error}");
                }
            }

            stopwatch.Stop();

            var averageTime = TimeSpan.FromTicks((long)executionTimes.Average(t => t.Ticks));
            var maxTime = executionTimes.Max();
            var minTime = executionTimes.Min();

            if (averageTime > options.MaxAcceptableExecutionTime)
            {
                issues.Add($"Average execution time ({averageTime.TotalMilliseconds:F2}ms) exceeds threshold ({options.MaxAcceptableExecutionTime.TotalMilliseconds:F2}ms)");
            }

            return new TestResult
            {
                Passed = !issues.Any(),
                Issues = issues,
                Details = $"Avg: {averageTime.TotalMilliseconds:F2}ms, Min: {minTime.TotalMilliseconds:F2}ms, Max: {maxTime.TotalMilliseconds:F2}ms"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Passed = false,
                Issues = new List<string> { $"Performance test failed: {ex.Message}" }
            };
        }
    }

    private static bool IsValidParameterType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(int) ||
               type == typeof(double) ||
               type == typeof(bool) ||
               type == typeof(DateTime) ||
               type == typeof(object);
    }

    private static object GetDefaultValue(Type type)
    {
        return type switch
        {
            var t when t == typeof(string) => "test",
            var t when t == typeof(int) => 0,
            var t when t == typeof(double) => 0.0,
            var t when t == typeof(bool) => false,
            var t when t == typeof(DateTime) => DateTime.UtcNow,
            _ => "test"
        };
    }
}

public class ToolTestResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool OverallSuccess { get; set; }
    public TimeSpan TotalTestTime { get; set; }
    public string? UnexpectedError { get; set; }
    
    public TestResult SchemaValidation { get; set; } = new();
    public TestResult ParameterValidation { get; set; } = new();
    public TestResult ExecutionTest { get; set; } = new();
    public TestResult ErrorHandling { get; set; } = new();
    public TestResult PerformanceTest { get; set; } = new();
}

public class TestResult
{
    public bool Passed { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Details { get; set; } = string.Empty;
}

public class ToolTestOptions
{
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IncludePerformanceTests { get; set; } = false;
    public int PerformanceIterations { get; set; } = 5;
    public TimeSpan MaxAcceptableExecutionTime { get; set; } = TimeSpan.FromSeconds(5);
}

// Usage example
[Test]
public async Task TestFileSystemTool()
{
    var tool = new FileSystemTool();
    var parameters = new Dictionary<string, object>
    {
        ["operation"] = "read",
        ["path"] = "test.txt",
        ["content"] = ""
    };

    var testResult = await ToolTestingFramework.TestToolAsync(tool, parameters, new ToolTestOptions
    {
        IncludePerformanceTests = true,
        PerformanceIterations = 10,
        MaxAcceptableExecutionTime = TimeSpan.FromSeconds(1)
    });

    Assert.IsTrue(testResult.OverallSuccess, 
        $"Tool test failed: {string.Join(", ", testResult.SchemaValidation.Issues)}");
}
```

## Next Steps

### Integration with Other Packages
- **Agent.Orchestration** - Use tools within complex workflows and agent coordination
- **Agent.Security** - Secure tool execution with proper authorization and input validation
- **Agent.Observability** - Monitor tool usage, performance, and failure patterns

### Advanced Tool Development
- **Custom Tool Categories** - Specialized tools for specific domains (finance, healthcare, logistics)
- **Tool Composition** - Combine multiple tools into higher-level capabilities
- **Tool Versioning** - Manage tool evolution and backward compatibility
- **Tool Marketplace** - Share and discover community-developed tools

### Production Considerations
- **Tool Security** - Input sanitization, output validation, resource limits
- **Tool Performance** - Caching, connection pooling, batch operations
- **Tool Reliability** - Error recovery, fallback mechanisms, health monitoring
- **Tool Governance** - Usage policies, access controls, audit trails

---

**🎯 You now have the complete framework for extending AI agents with powerful, real-world capabilities!**

Agent.Tools transforms your AI agents from text-only chatbots into action-taking systems that can interact with files, APIs, databases, and any external system you need. The comprehensive tool development framework, testing strategies, and discovery mechanisms ensure your tools are reliable, maintainable, and production-ready.