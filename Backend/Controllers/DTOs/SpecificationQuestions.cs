using System.Linq.Expressions;
using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for specification questions, containing all necessary information for creating, updating, and retrieving specification question data. The SpecificationQuestionDTO serves as a base class for various
/// </summary>
public class SpecificationQuestionDTO
{    
    /// <inheritdoc cref="SpecificationQuestion.QuestionDutch"/>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionDutch { get; set; }
    
    /// <inheritdoc cref="SpecificationQuestion.QuestionEnglish"/>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionEnglish { get; set; }
    
    /// <inheritdoc cref="SpecificationQuestion.Type"/>
    public required QuestionType Type { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.IsMandatory"/>
    public required bool IsMandatory { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.Options"/>
    public required bool IsPublic { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.Options"/>
    public List<string>? Options { get; set; }
}

/// <summary>
/// Defines the DTO for posting a specification question, containing the necessary information for creating a new specification question, including the activity ID and all relevant properties of the specification question. The PostSpecificationQuestionDTO is used to transfer data from the client to the server when creating a new specification question, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostSpecificationQuestionDTO : SpecificationQuestionDTO
{
    /// <inheritdoc cref="SpecificationQuestion.ActivityId"/>
    public required uint ActivityId { get; set; }
}

/// <summary>
/// Represents the response DTO for a specification question, containing all relevant information about the specification question, including its properties and any associated data. The GetSpecificationQuestionResponseDTO is used to transfer comprehensive specification question data from the server to the client when retrieving specification question information, allowing for a complete representation of the specification question's details in the response payload. The GetSpecificationQuestionResponseDTO can include properties such as the question ID, activity ID, question text in both Dutch and English, question type, mandatory status, public visibility, and any associated options, providing a comprehensive view of the specification question data for the client application.
/// </summary>
public class GetSpecificationQuestionResponseDTO : SpecificationQuestionDTO
{
    /// <inheritdoc cref="SpecificationQuestion.Id"/>
    public required uint Id { get; set; }

    /// <summary>
        /// Projects a SpecificationQuestion entity into a GetSpecificationQuestionResponseDTO, including the question ID, Dutch and English versions of the question, type, mandatory and public flags, and options if available. This projection is used to transform the data from the SpecificationQuestion model into a format that is suitable for API responses, ensuring that the relevant information about the specification question is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping SpecificationQuestion entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling specification question-related data transformations for API responses.
        /// </summary>
        /// <returns>An expression that projects a SpecificationQuestion entity into a GetSpecificationQuestionResponseDTO.</returns>
        public static Expression<Func<SpecificationQuestion, GetSpecificationQuestionResponseDTO>> ToDto()
        {
            return sq => new GetSpecificationQuestionResponseDTO
            {
                Id = sq.Id,
                QuestionDutch = sq.QuestionDutch,
                QuestionEnglish = sq.QuestionEnglish,
                Type = sq.Type,
                IsMandatory = sq.IsMandatory,
                IsPublic = sq.IsPublic,
                Options = sq.Options != null
                    ? sq.Options.Split(new[] { ';' }, StringSplitOptions.None).ToList()
                    : null
            };
        }
}

/// <summary>
/// Defines the DTO for updating an existing specification question, containing all necessary information for modifying a specification question's properties. The UpdateSpecificationQuestionDTO is used to transfer data from the client to the server when updating an existing specification question, allowing for changes to be made to the specification question's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class UpdateSpecificationQuestionDTO : SpecificationQuestionDTO
{
    /// <inheritdoc cref="SpecificationQuestion.Id"/>
    public uint? Id { get; set; } 
}