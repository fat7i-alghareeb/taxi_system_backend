namespace Taxi.Contracts.Common;

/// <summary>
/// A machine-readable format for specifying errors in HTTP API responses
/// based on https://tools.ietf.org/html/rfc7807.
/// </summary>
public class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = [];
}

