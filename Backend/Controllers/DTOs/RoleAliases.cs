namespace Backend.Controllers.DTOs;

public class PostRoleAliasDTO
{
    /// <summary>
    /// The name of the role alias (e.g., "Schatbewaarder").
    /// </summary>
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
    public required string Name { get; set; }

    /// <summary>
    /// The updated parent role ID.
    /// </summary>
    public uint RoleId { get; set; }
}