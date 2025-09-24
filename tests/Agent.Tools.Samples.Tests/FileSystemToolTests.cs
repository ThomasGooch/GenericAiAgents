using Agent.Tools;
using Agent.Tools.Models;
using Agent.Tools.Samples;

namespace Agent.Tools.Samples.Tests;

public class FileSystemToolTests
{
    private readonly string _testDirectory;

    public FileSystemToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"agent-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void FileSystemTool_ShouldHaveCorrectMetadata()
    {
        var tool = new FileSystemTool();
        
        Assert.Equal("file-system", tool.Name);
        Assert.Equal("Performs file system operations like read, write, list, and delete files and directories", tool.Description);
    }

    [Fact]
    public void GetParameterSchema_ShouldReturnExpectedSchema()
    {
        var tool = new FileSystemTool();
        
        var schema = tool.GetParameterSchema();
        
        Assert.Contains("operation", schema.Keys);
        Assert.Contains("path", schema.Keys);
        Assert.Contains("content", schema.Keys);
        
        Assert.Equal(typeof(string), schema["operation"]);
        Assert.Equal(typeof(string), schema["path"]);
        Assert.Equal(typeof(string), schema["content"]);
    }

    [Fact]
    public void ValidateParameters_WithValidParameters_ShouldReturnTrue()
    {
        var tool = new FileSystemTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "read" },
            { "path", "/test/path" },
            { "content", "" }
        };

        var result = tool.ValidateParameters(parameters);

        Assert.True(result);
    }

    [Fact]
    public void ValidateParameters_WithMissingOperation_ShouldReturnFalse()
    {
        var tool = new FileSystemTool();
        var parameters = new Dictionary<string, object>
        {
            { "path", "/test/path" },
            { "content", "" }
        };

        var result = tool.ValidateParameters(parameters);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithWriteOperation_ShouldCreateFile()
    {
        var tool = new FileSystemTool();
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var parameters = new Dictionary<string, object>
        {
            { "operation", "write" },
            { "path", filePath },
            { "content", "Hello, World!" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.True(File.Exists(filePath));
        Assert.Equal("Hello, World!", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task ExecuteAsync_WithReadOperation_ShouldReadFile()
    {
        var tool = new FileSystemTool();
        var filePath = Path.Combine(_testDirectory, "read-test.txt");
        var expectedContent = "Test content for reading";
        
        File.WriteAllText(filePath, expectedContent);

        var parameters = new Dictionary<string, object>
        {
            { "operation", "read" },
            { "path", filePath },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains(expectedContent, result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithListOperation_ShouldListDirectory()
    {
        var tool = new FileSystemTool();
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.txt"), "content2");

        var parameters = new Dictionary<string, object>
        {
            { "operation", "list" },
            { "path", _testDirectory },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("file1.txt", result.Output);
        Assert.Contains("file2.txt", result.Output);
        Assert.Contains("subdir", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeleteFileOperation_ShouldDeleteFile()
    {
        var tool = new FileSystemTool();
        var filePath = Path.Combine(_testDirectory, "delete-test.txt");
        File.WriteAllText(filePath, "content to delete");

        var parameters = new Dictionary<string, object>
        {
            { "operation", "delete" },
            { "path", filePath },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task ExecuteAsync_WithCreateDirOperation_ShouldCreateDirectory()
    {
        var tool = new FileSystemTool();
        var dirPath = Path.Combine(_testDirectory, "new-directory");

        var parameters = new Dictionary<string, object>
        {
            { "operation", "mkdir" },
            { "path", dirPath },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.True(Directory.Exists(dirPath));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidOperation_ShouldReturnError()
    {
        var tool = new FileSystemTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "invalid" },
            { "path", "/test/path" },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.Contains("Unsupported operation", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithReadNonExistentFile_ShouldReturnError()
    {
        var tool = new FileSystemTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "read" },
            { "path", "/non-existent/file.txt" },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistsOperation_ShouldReturnExistence()
    {
        var tool = new FileSystemTool();
        var existingFile = Path.Combine(_testDirectory, "exists-test.txt");
        File.WriteAllText(existingFile, "test");

        // Test existing file
        var parameters = new Dictionary<string, object>
        {
            { "operation", "exists" },
            { "path", existingFile },
            { "content", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("\"Exists\": true", result.Output);

        // Test non-existing file
        parameters["path"] = Path.Combine(_testDirectory, "non-existent.txt");
        result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("\"Exists\": false", result.Output);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}