using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostMemberDTO
{
    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public required uint StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    [StringLength(60)]
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Models.Member.LastName"/>
    [StringLength(60)]
    public required string LastName { get; set; }

    /// <inheritdoc cref="Models.Member.Email"/>
    [StringLength(100)]
    public required string Email { get; set; }

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    [StringLength(20)]
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.Address"/>
    [StringLength(200)]
    public required string Address { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public required DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }
}
