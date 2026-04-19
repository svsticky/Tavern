using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostRoleAliasDTO
{
    /// <summary>
    /// The name of the role alias (e.g., "Schatbewaarder").
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// The ID of the parent role this alias belongs to.
    /// </summary>
    public uint RoleId { get; set; }
}

public class RoleAliasUpdateDTO
{
    /// <summary>
    /// The updated name of the role alias.
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// The updated parent role ID.
    /// </summary>
    public uint RoleId { get; set; }
}

public class GetRoleAliasDTO
{
    public uint? GroupId { get; set; }
}