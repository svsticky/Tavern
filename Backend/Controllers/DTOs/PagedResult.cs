namespace Backend.Controllers.DTOs;

/// <summary>Generic paginated result wrapper.</summary>
/// <typeparam name="T">The item type.</typeparam>
public class PagedResultDTO<T>
{
    /// <summary>The items for the current page.</summary>
    public required IEnumerable<T> Items { get; set; }
    /// <summary>Total number of items across all pages.</summary>
    public required int TotalCount { get; set; }
    /// <summary>Current 1-based page number.</summary>
    public required int Page { get; set; }
    /// <summary>Number of items per page.</summary>
    public required int PageSize { get; set; }
}
