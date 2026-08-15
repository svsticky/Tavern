using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing role aliases within the system. The RoleAliasesController provides a set of endpoints for defining and managing alternative names for system roles, allowing for greater flexibility and user-friendly nomenclature across the application. This controller handles the full CRUD lifecycle for role aliases, ensuring that changes to role naming conventions are performed securely by authorized personnel. By interacting with the IRoleAliasService, the controller maintains a mapping between internal system roles and their public-facing aliases, facilitating a more intuitive experience for end-users while preserving the integrity of the underlying authorization logic.
/// </summary>
/// <param name="roleAliasService">The service responsible for role alias business logic and data persistence.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RoleAliasesController(IRoleAliasService roleAliasService) : ControllerBase
{

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: rolealiases
    /// <summary>
    /// Retrieves a list of all role aliases defined in the system. The GetRoleAliases endpoint allows clients to fetch the complete collection of defined aliases, which is useful for populating dropdowns or displaying role-based information with user-friendly labels. This endpoint provides a comprehensive view of how internal roles are presented within the user interface.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A collection of role alias objects.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<RoleAlias>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RoleAlias>>> GetRoleAliases(CancellationToken ct)
    {
        return Ok(await roleAliasService.GetRoleAliases(ct));
    }

    // GET: rolealiases/{id}
    /// <summary>
    /// Retrieves the details of a specific role alias by its unique identifier. The GetRoleAlias endpoint is designed to provide specific information about a single alias mapping, enabling clients to inspect individual role definitions and their associated system role identifiers. If the alias does not exist, the endpoint returns a 404 Not Found status to signal the absence of the resource.
    /// </summary>
    /// <param name="id">The unique identifier of the role alias to retrieve.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The requested role alias if found; otherwise, a 404 status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RoleAlias), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoleAlias>> GetRoleAlias(uint id, CancellationToken ct)
    {
        var result = await roleAliasService.GetRoleAlias(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    // POST: rolealiases
    /// <summary>
    /// Creates a new role alias within the system. The PostRoleAlias endpoint allows authorized administrators to define new custom names for existing system roles by providing a PostRoleAliasDTO. This process ensures that the provided alias is validated and correctly linked to a system role, while enforcing strict authorization to prevent unauthorized modifications to the system's naming configuration. Upon successful creation, the endpoint returns the details of the new alias along with its unique location.
    /// </summary>
    /// <param name="dto">The data transfer object containing the role alias definition.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The newly created role alias with a 201 Created status.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RoleAlias), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoleAlias>> PostRoleAlias(PostRoleAliasDTO dto, CancellationToken ct)
    {
        var result = await roleAliasService.CreateRoleAlias(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetRoleAlias), new { id = result.Id }, result);
    }

    // DELETE: rolealiases/{id}
    /// <summary>
    /// Permanently removes a role alias from the system. The DeleteRoleAlias endpoint ensures that a specific alias mapping is deleted, reverting the system to its default naming for the associated role. This action is restricted to authorized users and includes checks to ensure the resource exists before attempting deletion. Upon success, a 204 No Content status is returned to signify the resource's removal.
    /// </summary>
    /// <param name="id">The unique identifier of the role alias to be deleted.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful deletion.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRoleAlias(uint id, CancellationToken ct)
    {
        await roleAliasService.DeleteRoleAlias(id, GetUserId(), ct);
        return NoContent();
    }

    // PATCH: rolealiases/{id}
    /// <summary>
    /// Partially updates an existing role alias using a JSON Patch document. The PatchRoleAlias endpoint provides the flexibility to modify specific fields of an alias—such as the displayed name—without requiring the submission of the entire object. This is particularly useful for fine-tuning role nomenclature while ensuring that the updates are validated against system constraints and authorized by the proper administrative permissions.
    /// </summary>
    /// <param name="id">The unique identifier of the role alias to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the modifications.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful application of the patch.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, CancellationToken ct)
    {
        await roleAliasService.PatchRoleAlias(id, patchDoc, GetUserId(), ct);
        return NoContent();
    }

    // PUT: rolealiases/{id}
    /// <summary>
    /// Performs a full update of an existing role alias definition. The PutRoleAlias endpoint replaces the current state of a role alias with the data provided in the RoleAliasUpdateDTO. This is the standard method for comprehensive changes to an alias, ensuring that the updated record is fully validated and that the user has the necessary administrative rights to modify system role mappings.
    /// </summary>
    /// <param name="id">The unique identifier of the role alias to update.</param>
    /// <param name="dto">The data transfer object containing the updated alias information.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the update was successful.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutRoleAlias(uint id, RoleAliasUpdateDTO dto, CancellationToken ct)
    {
        await roleAliasService.UpdateRoleAlias(id, dto, GetUserId(), ct);
        return NoContent();
    }
}
