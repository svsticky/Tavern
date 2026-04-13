using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

[PrimaryKey(nameof(Name))]
public class Setting
{
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
}