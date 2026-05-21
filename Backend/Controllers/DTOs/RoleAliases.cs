using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a role alias, containing the necessary information for creating a new role alias, including its name and the associated parent role ID. The PostRoleAliasDTO is used to transfer data from the client to the server when creating a new role alias, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostRoleAliasDTO
{
    /// <inheritdoc cref="Models.Domain.RoleAlias.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Domain.RoleAlias.RoleId"/>
    public uint RoleId { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing role alias, containing all necessary information for modifying a role alias's properties. The RoleAliasUpdateDTO is used to transfer data from the client to the server when updating an existing role alias, allowing for changes to be made to the role alias's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class RoleAliasUpdateDTO
{
    /// <inheritdoc cref="Models.Domain.RoleAlias.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Domain.RoleAlias.RoleId"/>
    public uint RoleId { get; set; }
}