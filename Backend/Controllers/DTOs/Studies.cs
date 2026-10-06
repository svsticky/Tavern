using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a study, containing the necessary information for creating a new study, including its title, nominal duration in years, and type. The PostStudyDTO is used to transfer data from the client to the server when creating a new study, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostStudyDTO
{
    /// <inheritdoc cref="Study.Title"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Study.NominalDurationYears"/>
    public required uint NominalDurationYears { get; set; }

    /// <inheritdoc cref="Study.Type"/>
    public required StudyType Type { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing study, containing all necessary information for modifying a study's properties. The StudyUpdateDTO is used to transfer data from the client to the server when updating an existing study, allowing for changes to be made to the study's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class StudyUpdateDTO
{
    /// <inheritdoc cref="Study.Title"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Study.NominalDurationYears"/>
    public required uint NominalDurationYears { get; set; }

    /// <inheritdoc cref="Study.Type"/>
    public required StudyType Type { get; set; }

    /// <inheritdoc cref="Study.Active"/>
    public required bool Active { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving studies, allowing filtering based on active status. The GetStudyDTO is used to transfer filter criteria from the client to the server when retrieving study data, ensuring that public consumers of the study catalog (e.g. the registration form) only see currently active studies, while administrative views can opt in to see inactive ones as well.
/// </summary>
public class GetStudyDTO
{
    /// <summary>
    /// Indicates whether to include inactive studies in the retrieved study data. If set to true, both active and inactive studies will be included in the response; if set to false, only active studies will be included.
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
