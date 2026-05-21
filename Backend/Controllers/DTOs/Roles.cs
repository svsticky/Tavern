using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a role, containing the necessary information for creating a new role, including its name. The PostRoleDTO is used to transfer data from the client to the server when creating a new role, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostRoleDTO
{
    /// <inheritdoc cref="Models.Domain.Role.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing role, containing all necessary information for modifying a role's properties. The RoleUpdateDTO is used to transfer data from the client to the server when updating an existing role, allowing for changes to be made to the role's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class RoleUpdateDTO
{
    /// <inheritdoc cref="Models.Domain.Role.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }
}