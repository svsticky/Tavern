using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

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

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<PostStudyEnrollmentDTO>? StudyEnrollments { get; set; }
}

public class MemberResponseDTO
{
    /// <inheritdoc cref="Models.Member.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public uint StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    public required string FirstName { get; set; };

    /// <inheritdoc cref="Models.Member.LastName"/>
    public required string LastName { get; set; };

    /// <inheritdoc cref="Models.Member.Email"/>
    public required string Email { get; set; };

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    public required string PhoneNumber { get; set; };

    /// <inheritdoc cref="Models.Member.Address"/>
    public required string Address { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Models.Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Models.Member.RegisteredOn"/>
    public DateTimeOffset RegisteredOn { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public Language PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<StudyEnrollmentResponseDTO> StudyEnrollments { get; set; } = new();

    /// <summary>
    /// Groups where the member is a part of.
    /// </summary>
    public List<GroupMembershipResponseDTO> GroupMemberships { get; set; } = new();
}

public class MemberUpdateDTO
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

    /// <inheritdoc cref="Models.Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <inheritdoc cref="Models.Member.Gratie"/>
    public bool Gratie { get; set; }

    /// <inheritdoc cref="Models.Member.LidVanVerdienste"/>
    public bool LidVanVerdienste { get; set; }

    /// <inheritdoc cref="Models.Member.EreLid"/>
    public bool EreLid { get; set; }

    /// <inheritdoc cref="Models.Member.Begunstiger"/>
    public bool Begunstiger { get; set; }

    /// <inheritdoc cref="Models.Member.Suspended"/>
    public bool Suspended { get; set; }
}