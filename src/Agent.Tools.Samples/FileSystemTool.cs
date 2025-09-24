using Agent.Tools;
using Agent.Tools.Models;
using System.Text.Json;

namespace Agent.Tools.Samples;

/// <summary>
/// Tool for file system operations like read, write, list, and delete
/// </summary>
[Tool("file-system")]
[Description("Performs file system operations like read, write, list, and delete files and directories")]
public class FileSystemTool : BaseTool
{
    private static readonly string[] _allowedOperations = { "read", "write", "list", "delete", "mkdir", "exists", "info" };

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "operation", typeof(string) },
            { "path", typeof(string) },
            { "content", typeof(string) }
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        try
        {
            var operation = parameters["operation"].ToString()!.ToLowerInvariant();
            var path = parameters["path"].ToString()!;
            var content = parameters["content"].ToString() ?? "";

            // Validate operation
            if (!_allowedOperations.Contains(operation))
            {
                return ToolResult.CreateError($"Unsupported operation: {operation}. Supported operations: {string.Join(", ", _allowedOperations)}");
            }

            // Validate path
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResult.CreateError("Path cannot be empty");
            }

            return operation switch
            {
                "read" => await ReadFileAsync(path, cancellationToken),
                "write" => await WriteFileAsync(path, content, cancellationToken),
                "list" => await ListDirectoryAsync(path, cancellationToken),
                "delete" => await DeleteAsync(path, cancellationToken),
                "mkdir" => await CreateDirectoryAsync(path, cancellationToken),
                "exists" => await CheckExistsAsync(path, cancellationToken),
                "info" => await GetFileInfoAsync(path, cancellationToken),
                _ => ToolResult.CreateError($"Operation {operation} not implemented")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"File system operation failed: {ex.Message}");
        }
    }

    private Task<ToolResult> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
            {
                return ToolResult.CreateError($"File does not exist: {path}");
            }

            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var result = new
            {
                Operation = "read",
                Path = path,
                Content = content,
                Size = new FileInfo(path).Length,
                LastModified = File.GetLastWriteTime(path)
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to read file: {ex.Message}");
        }
    }

    private Task<ToolResult> WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content, cancellationToken);

            var result = new
            {
                Operation = "write",
                Path = path,
                BytesWritten = System.Text.Encoding.UTF8.GetByteCount(content),
                LastModified = File.GetLastWriteTime(path)
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to write file: {ex.Message}");
        }
    }

    private Task<ToolResult> ListDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return Task.FromResult(ToolResult.CreateError($"Directory does not exist: {path}"));
            }

            var entries = new List<object>();

            // Get directories
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirInfo = new DirectoryInfo(dir);
                entries.Add(new
                {
                    Name = dirInfo.Name,
                    FullPath = dirInfo.FullName,
                    Type = "directory",
                    LastModified = dirInfo.LastWriteTime,
                    Created = dirInfo.CreationTime
                });
            }

            // Get files
            foreach (var file in Directory.GetFiles(path))
            {
                var fileInfo = new FileInfo(file);
                entries.Add(new
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    Type = "file",
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Created = fileInfo.CreationTime
                });
            }

            var result = new
            {
                Operation = "list",
                Path = path,
                TotalItems = entries.Count,
                Items = entries
            };

            return Task.FromResult(ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.CreateError($"Failed to list directory: {ex.Message}"));
        }
    }

    private Task<ToolResult> DeleteAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            bool wasFile = File.Exists(path);
            bool wasDirectory = Directory.Exists(path);

            if (!wasFile && !wasDirectory)
            {
                return ToolResult.CreateError($"Path does not exist: {path}");
            }

            if (wasFile)
            {
                File.Delete(path);
            }
            else if (wasDirectory)
            {
                Directory.Delete(path, true); // Recursive delete
            }

            var result = new
            {
                Operation = "delete",
                Path = path,
                Type = wasFile ? "file" : "directory",
                Deleted = true
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to delete: {ex.Message}");
        }
    }

    private Task<ToolResult> CreateDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (Directory.Exists(path))
            {
                return ToolResult.CreateError($"Directory already exists: {path}");
            }

            var dirInfo = Directory.CreateDirectory(path);

            var result = new
            {
                Operation = "mkdir",
                Path = path,
                Created = true,
                FullPath = dirInfo.FullName,
                CreatedTime = dirInfo.CreationTime
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to create directory: {ex.Message}");
        }
    }

    private Task<ToolResult> CheckExistsAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            bool fileExists = File.Exists(path);
            bool directoryExists = Directory.Exists(path);
            bool exists = fileExists || directoryExists;

            var result = new
            {
                Operation = "exists",
                Path = path,
                Exists = exists,
                Type = fileExists ? "file" : directoryExists ? "directory" : "none"
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to check existence: {ex.Message}");
        }
    }

    private Task<ToolResult> GetFileInfoAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var result = new
                {
                    Operation = "info",
                    Path = path,
                    Type = "file",
                    Size = fileInfo.Length,
                    Created = fileInfo.CreationTime,
                    LastModified = fileInfo.LastWriteTime,
                    LastAccessed = fileInfo.LastAccessTime,
                    Extension = fileInfo.Extension,
                    Directory = fileInfo.DirectoryName,
                    IsReadOnly = fileInfo.IsReadOnly
                };

                return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                var result = new
                {
                    Operation = "info",
                    Path = path,
                    Type = "directory",
                    Created = dirInfo.CreationTime,
                    LastModified = dirInfo.LastWriteTime,
                    LastAccessed = dirInfo.LastAccessTime,
                    Parent = dirInfo.Parent?.FullName,
                    FileCount = Directory.GetFiles(path).Length,
                    DirectoryCount = Directory.GetDirectories(path).Length
                };

                return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                return ToolResult.CreateError($"Path does not exist: {path}");
            }
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Failed to get file info: {ex.Message}");
        }
    }
}