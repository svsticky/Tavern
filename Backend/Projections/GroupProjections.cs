using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The GroupProjections class provides a method to project a Group entity into a GroupResponseDTO. This projection is used to transform the data from the Group model into a format that is suitable for API responses, including relevant group information such as its name, type, active status, and picture path. The ToDto method centralizes the logic for mapping Group entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling group-related data transformations for API responses.
/// </summary>
public static class GroupProjections
{
    /// <summary>
    /// Projects a Group entity into a GroupResponseDTO, including relevant group information such as its name, type, active status, and picture path. This projection is used to transform the data from the Group model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <returns>An expression that projects a Group entity into a GroupResponseDTO.</returns>
    public static Expression<Func<Group, GroupResponseDTO>> ToDto()
    {
        return g => new GroupResponseDTO
        {
            Id = g.Id,
            Name = g.Name,
            Type = g.Type,
            Active = g.Active,
            GroupPicturePath = g.GroupPicturePath
        };
    }
}
