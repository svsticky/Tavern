namespace Backend.Controllers.DTOs;

/// <summary>
/// Represents an error response.
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// The error message describing the issue that occurred.
    /// </summary>
    public required string Message { get; set; } = null!;
}