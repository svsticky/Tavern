using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing specification answers within the system. The SpecificationAnswers controller provides endpoints for handling user-submitted responses to specific requirements or questions defined within the system's specification models. This controller is primarily focused on enabling flexible updates to existing answers, ensuring that data integrity and business rules are maintained during the modification process. By leveraging the ISpecificationAnswerService, the controller facilitates a secure way for users to provide or adjust information while enforcing strict authorization policies to ensure that only the rightful owners or authorized personnel can modify sensitive specification data.
/// </summary>
/// <param name="service">The specification answer service responsible for processing answer logic and persistence.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class SpecificationAnswers(ISpecificationAnswerService service) : ControllerBase
{
    // PATCH: specificationanswers/5
    /// <summary>
    /// Partially updates a specific specification answer by its unique identifier using a JSON Patch document. The PatchSpecificationAnswer endpoint allows clients to modify individual properties of an existing answer—such as its value or status—without having to submit the entire answer entity. This approach is highly efficient for targeted updates and ensures that only the intended fields are altered. The endpoint validates the patch operations against the specification's requirements and ensures that the requesting user has the necessary permissions to perform the update. Upon a successful operation, it returns a 204 No Content status, indicating that the changes have been applied successfully.
    /// </summary>
    /// <param name="answerId">The unique identifier of the specification answer to be patched.</param>
    /// <param name="patchDoc">The JSON Patch document containing the set of modifications to apply.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful update; otherwise, appropriate error feedback.</returns>
    [HttpPatch("{answerId}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PatchSpecificationAnswer(uint answerId, [FromBody] JsonPatchDocument<SpecificationAnswer> patchDoc, CancellationToken ct)
    {
        try
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await service.PatchSpecificationAnswersAsync(userId, answerId, patchDoc, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }
}