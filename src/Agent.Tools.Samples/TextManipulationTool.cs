using Agent.Tools;
using Agent.Tools.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Agent.Tools.Samples;

/// <summary>
/// Tool for text manipulation operations like transform, search, replace, and format
/// </summary>
[Tool("text-manipulation")]
[Description("Performs text manipulation operations like transform, search, replace, and format text")]
public class TextManipulationTool : BaseTool
{
    private static readonly string[] _allowedOperations = 
    { 
        "uppercase", "lowercase", "replace", "trim", "reverse", "count", "words", "lines", 
        "contains", "startswith", "endswith", "substring", "capitalize", "titlecase"
    };

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "operation", typeof(string) },
            { "text", typeof(string) },
            { "search", typeof(string) },
            { "replace", typeof(string) }
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        try
        {
            var operation = parameters["operation"].ToString()!.ToLowerInvariant();
            var text = parameters["text"].ToString() ?? "";
            var search = parameters["search"].ToString() ?? "";
            var replace = parameters["replace"].ToString() ?? "";

            // Validate operation
            if (!_allowedOperations.Contains(operation))
            {
                return ToolResult.CreateError($"Unsupported operation: {operation}. Supported operations: {string.Join(", ", _allowedOperations)}");
            }

            var result = operation switch
            {
                "uppercase" => ProcessUppercase(text),
                "lowercase" => ProcessLowercase(text),
                "replace" => ProcessReplace(text, search, replace),
                "trim" => ProcessTrim(text),
                "reverse" => ProcessReverse(text),
                "count" => ProcessCount(text),
                "words" => ProcessWords(text),
                "lines" => ProcessLines(text),
                "contains" => ProcessContains(text, search),
                "startswith" => ProcessStartsWith(text, search),
                "endswith" => ProcessEndsWith(text, search),
                "substring" => ProcessSubstring(text, search, replace),
                "capitalize" => ProcessCapitalize(text),
                "titlecase" => ProcessTitleCase(text),
                _ => throw new InvalidOperationException($"Operation {operation} not implemented")
            };

            return ToolResult.CreateSuccess(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Text manipulation failed: {ex.Message}");
        }
    }

    private object ProcessUppercase(string text)
    {
        return new
        {
            Operation = "uppercase",
            OriginalText = text,
            Result = text.ToUpperInvariant(),
            Length = text.Length
        };
    }

    private object ProcessLowercase(string text)
    {
        return new
        {
            Operation = "lowercase",
            OriginalText = text,
            Result = text.ToLowerInvariant(),
            Length = text.Length
        };
    }

    private object ProcessReplace(string text, string search, string replace)
    {
        if (string.IsNullOrEmpty(search))
        {
            return new
            {
                Operation = "replace",
                OriginalText = text,
                Result = text,
                ReplacementsMade = 0,
                Message = "Search string is empty, no replacements made"
            };
        }

        var result = text.Replace(search, replace);
        var originalCount = Regex.Matches(text, Regex.Escape(search)).Count;

        return new
        {
            Operation = "replace",
            OriginalText = text,
            SearchString = search,
            ReplaceString = replace,
            Result = result,
            ReplacementsMade = originalCount
        };
    }

    private object ProcessTrim(string text)
    {
        var trimmed = text.Trim();
        return new
        {
            Operation = "trim",
            OriginalText = text,
            Result = trimmed,
            OriginalLength = text.Length,
            TrimmedLength = trimmed.Length,
            CharactersRemoved = text.Length - trimmed.Length
        };
    }

    private object ProcessReverse(string text)
    {
        var reversed = new string(text.Reverse().ToArray());
        return new
        {
            Operation = "reverse",
            OriginalText = text,
            Result = reversed,
            Length = text.Length
        };
    }

    private object ProcessCount(string text)
    {
        var lines = text.Split('\n').Length;
        var words = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var sentences = string.IsNullOrWhiteSpace(text) ? 0 : Regex.Matches(text, @"[.!?]+").Count;

        return new
        {
            Operation = "count",
            Text = text,
            Characters = text.Length,
            CharactersNoSpaces = text.Count(c => !char.IsWhiteSpace(c)),
            Words = words,
            Lines = lines,
            Sentences = sentences
        };
    }

    private object ProcessWords(string text)
    {
        var words = string.IsNullOrWhiteSpace(text) 
            ? Array.Empty<string>() 
            : text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        return new
        {
            Operation = "words",
            OriginalText = text,
            Words = words,
            WordCount = words.Length
        };
    }

    private object ProcessLines(string text)
    {
        var lines = text.Split('\n');
        return new
        {
            Operation = "lines",
            OriginalText = text,
            Lines = lines,
            LineCount = lines.Length
        };
    }

    private object ProcessContains(string text, string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return new
            {
                Operation = "contains",
                Text = text,
                SearchString = search,
                Result = false,
                Message = "Search string is empty"
            };
        }

        var contains = text.Contains(search, StringComparison.OrdinalIgnoreCase);
        return new
        {
            Operation = "contains",
            Text = text,
            SearchString = search,
            Result = contains
        };
    }

    private object ProcessStartsWith(string text, string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return new
            {
                Operation = "startswith",
                Text = text,
                SearchString = search,
                Result = false,
                Message = "Search string is empty"
            };
        }

        var startsWith = text.StartsWith(search, StringComparison.OrdinalIgnoreCase);
        return new
        {
            Operation = "startswith",
            Text = text,
            SearchString = search,
            Result = startsWith
        };
    }

    private object ProcessEndsWith(string text, string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return new
            {
                Operation = "endswith",
                Text = text,
                SearchString = search,
                Result = false,
                Message = "Search string is empty"
            };
        }

        var endsWith = text.EndsWith(search, StringComparison.OrdinalIgnoreCase);
        return new
        {
            Operation = "endswith",
            Text = text,
            SearchString = search,
            Result = endsWith
        };
    }

    private object ProcessSubstring(string text, string startIndexStr, string lengthStr)
    {
        if (!int.TryParse(startIndexStr, out var startIndex) || startIndex < 0 || startIndex >= text.Length)
        {
            return new
            {
                Operation = "substring",
                Text = text,
                Error = "Invalid start index",
                StartIndex = startIndexStr
            };
        }

        if (string.IsNullOrEmpty(lengthStr))
        {
            // If no length specified, take from start index to end
            var result = text.Substring(startIndex);
            return new
            {
                Operation = "substring",
                OriginalText = text,
                StartIndex = startIndex,
                Result = result,
                ResultLength = result.Length
            };
        }

        if (!int.TryParse(lengthStr, out var length) || length < 0)
        {
            return new
            {
                Operation = "substring",
                Text = text,
                Error = "Invalid length",
                Length = lengthStr
            };
        }

        if (startIndex + length > text.Length)
        {
            length = text.Length - startIndex;
        }

        var substring = text.Substring(startIndex, length);
        return new
        {
            Operation = "substring",
            OriginalText = text,
            StartIndex = startIndex,
            Length = length,
            Result = substring
        };
    }

    private object ProcessCapitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new
            {
                Operation = "capitalize",
                OriginalText = text,
                Result = text
            };
        }

        var result = char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
        return new
        {
            Operation = "capitalize",
            OriginalText = text,
            Result = result
        };
    }

    private object ProcessTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new
            {
                Operation = "titlecase",
                OriginalText = text,
                Result = text
            };
        }

        var words = text.Split(' ');
        var titleCased = words.Select(word => 
            string.IsNullOrEmpty(word) 
                ? word 
                : char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant());

        var result = string.Join(" ", titleCased);
        return new
        {
            Operation = "titlecase",
            OriginalText = text,
            Result = result,
            WordsProcessed = words.Length
        };
    }
}