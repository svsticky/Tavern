using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing system roles and their associated permissions within the application. The RolesController provides a robust set of endpoints for the creation, retrieval, modification, and deletion of roles, which serve as the foundation for the system's access control and organizational structure. By coordinating with the IRoleService, this controller ensures that role definitions are managed securely and consistently, allowing administrators to define the levels of authority and responsibilities assigned to different users. Proper authorization is enforced across all endpoints to maintain the integrity of the system's security model, preventing unauthorized modifications to the foundational role data.
/// </summary>
/// <param name="roleService">The service responsible for role-related business logic and data persistence.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: roles
    /// <summary>
    /// Retrieves a collection of all roles defined within the system. The GetRoles endpoint allows authorized clients to fetch the full registry of available roles, facilitating administrative oversight and providing the necessary data for role assignment interfaces. This endpoint is designed to return a comprehensive list of role entities, ensuring that clients have access to the current organizational structure and permission tiers established within the application.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A collection of role objects currently stored in the system.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Role>>> GetRoles(CancellationToken ct)
    {
        return Ok(await roleService.GetRoles(ct));
    }

    // GET: roles/{id}
    /// <summary>
    /// Retrieves the details of a specific role by its unique identifier. The GetRole endpoint provides a focused view of a single role entity, including its properties and associated metadata. This endpoint is useful for inspecting individual role configurations or verifying the details of a role before performing updates. If the requested role cannot be found, the endpoint provides appropriate feedback to ensure a secure and predictable API experience.
    /// </summary>
    /// <param name="id">The unique identifier of the role to retrieve.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The detailed role object if found; otherwise, a 404 Not Found status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Role>> GetRole(uint id, CancellationToken ct)
    {
        var role = await roleService.GetRole(id, ct);
        return role != null ? Ok(role) : NotFound();
    }

    // POST: roles
    /// <summary>
    /// Creates a new system role based on the provided configuration data. The PostRole endpoint allows authorized users to expand the system's organizational hierarchy by defining new roles through the PostRoleDTO. This process includes validating the role's properties, ensuring there are no naming conflicts, and associating the creation event with the requesting administrator. Upon successful creation, the endpoint returns the full details of the newly established role, including its system-generated identifier.
    /// </summary>
    /// <param name="dto">The data transfer object containing the initial role configuration.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The newly created role entity with a 201 Created status.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Role), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Role>> PostRole(PostRoleDTO dto, CancellationToken ct)
    {
        var result = await roleService.CreateRole(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetRole), new { id = result.Id }, result);
    }

    // DELETE: roles/{id}
    /// <summary>
    /// Permanently removes a specific role from the system by its unique identifier. The DeleteRole endpoint facilitates the decommissioning of roles that are no longer required, ensuring that the operation is performed only by users with the requisite administrative authority. This operation involves cleaning up the role record and ensuring that the system's security integrity is maintained. Upon successful deletion, a 204 No Content status is returned, confirming that the resource has been removed.
    /// </summary>
    /// <param name="id">The unique identifier of the role to be deleted.</param>
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
    public async Task<ActionResult> DeleteRole(uint id, CancellationToken ct)
    {
        await roleService.DeleteRole(id, GetUserId(), ct);
        return NoContent();
    }

    // PATCH: roles/{id}
    /// <summary>
    /// Partially updates the properties of an existing role using a JSON Patch document. The PatchRole endpoint provides a highly flexible mechanism for modifying specific attributes of a role—such as its name or description—without the need to transmit the entire role object. This endpoint validates the proposed changes against the role's domain model and ensures that the user is authorized to perform the requested modifications before persisting the changes to the system.
    /// </summary>
    /// <param name="id">The unique identifier of the role to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the set of intended modifications.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the patch was applied successfully.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchRole(uint id, JsonPatchDocument<Role> patchDoc, CancellationToken ct)
    {
        await roleService.PatchRole(id, patchDoc, GetUserId(), ct);
        return NoContent();
    }

    // PUT: roles/{id}
    /// <summary>
    /// Updates the complete representation of an existing role with the provided data. The PutRole endpoint is used for comprehensive updates where a role's configuration needs to be entirely refreshed. By providing a RoleUpdateDTO, clients can ensure the role's attributes are set to a specific state. This endpoint enforces strict authorization to ensure that only designated administrators can modify the foundational role definitions of the application.
    /// </summary>
    /// <param name="id">The unique identifier of the role to update.</param>
    /// <param name="dto">The data transfer object containing the full updated role information.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon a successful full update.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutRole(uint id, RoleUpdateDTO dto, CancellationToken ct)
    {
        await roleService.UpdateRole(id, dto, GetUserId(), ct);
        return NoContent();
    }

    // GET: roles/{id}/permissions
    /// <summary>
    /// Retrieves the permissions currently granted to a role.
    /// </summary>
    /// <param name="id">The unique identifier of the role.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The permissions granted to the role.</returns>
    [HttpGet("{id}/permissions")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetRolePermissions(uint id, CancellationToken ct)
    {
        return Ok(await roleService.GetRolePermissions(id, ct));
    }

    // PUT: roles/{id}/permissions
    /// <summary>
    /// Replaces the full set of permissions granted to a role. Each entry is either the string name
    /// of one of the 12 known permissions, or an arbitrary custom string for other applications
    /// sharing this Keycloak instance to interpret - Tavern's own backend only evaluates the known ones.
    /// </summary>
    /// <param name="id">The unique identifier of the role.</param>
    /// <param name="permissions">The full set of permission keys the role should have.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon a successful update.</returns>
    [HttpPut("{id}/permissions")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SetRolePermissions(uint id, [FromBody] List<string> permissions, CancellationToken ct)
    {
        await roleService.SetRolePermissions(id, permissions, GetUserId(), ct);
        return NoContent();
    }
}
