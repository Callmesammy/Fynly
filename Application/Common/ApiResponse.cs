namespace AiCFO.Application.Common;

/// <summary>
/// Standard API response envelope for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public ApiMeta Meta { get; set; } = new();
}

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Dictionary<string, string[]>? Details { get; set; }
}

public class ApiMeta
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Non-generic API response (for delete operations, etc.)
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public ApiError? Error { get; set; }
    public ApiMeta Meta { get; set; } = new();

    public static ApiResponse Ok() => new() { Success = true };
    public static ApiResponse Fail(string message, string? code = null, Dictionary<string, string[]>? errors = null)
        => new()
        {
            Success = false,
            Error = new ApiError { Message = message, Code = code, Details = errors }
        };
}
