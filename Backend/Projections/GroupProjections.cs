using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class GroupProjections
{
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
