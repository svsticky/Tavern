using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostRoleDTO
{
    /// <inheritdoc cref="Models.Role.Name"/>
    [StringLength(100)]
    public required string Name { get; set; }
}

public class RoleUpdateDTO
{
    /// <inheritdoc cref="Models.Role.Name"/>
    [StringLength(100)]
    public required string Name { get; set; }
}