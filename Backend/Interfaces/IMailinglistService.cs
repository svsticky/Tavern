using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Interface for managing mailing lists. The IMailinglistService defines the contract for operations related to mailing lists, including retrieving all mailing lists, fetching a specific mailing list by ID, creating a new mailing list, updating an existing mailing list, deleting a mailing list, and partially updating a mailing list using a JSON Patch document. This interface abstracts the underlying implementation of the mailing list management logic, allowing for separation of concerns and enabling different implementations to be used without affecting the consumers of the service. It also includes authorization checks to ensure that only authorized users can perform certain operations on the mailing lists.
/// </summary>
public interface IMailinglistService
{
    /// <summary>
    /// Retrieves a list of all mailing lists. This method returns an enumerable collection of Mailinglist entities, allowing consumers to access the complete set of mailing lists available in the system. The method accepts a CancellationToken to support cancellation of the operation if needed. This functionality is essential for displaying available mailing lists to users and enabling them to manage their email communication preferences effectively.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of mailing lists.</returns>
    Task<IEnumerable<Mailinglist>> GetMailinglists(CancellationToken ct);

    /// <summary>
    /// Retrieves a specific mailing list by its ID. This method allows consumers to fetch the details of a single mailing list identified by its unique ID. The method accepts the ID of the mailing list and a CancellationToken for operation cancellation. If the mailing list is found, it returns the corresponding Mailinglist entity; if not found, it returns null. This functionality is crucial for users to view and manage specific mailing lists within the application.
    /// </summary>
    /// <param name="id">The ID of the mailing list to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The mailing list if found, otherwise null.</returns>
    Task<Mailinglist?> GetMailinglist(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new mailing list. This method allows authorized users to create a new mailing list by providing the necessary details in the form of a PostMailinglistDTO. The method accepts the DTO containing the details for the new mailing list, the user ID of the creator for authorization purposes, and a CancellationToken for operation cancellation. If the mailing list is successfully created, it returns the newly created Mailinglist entity. This functionality is essential for users to expand their communication channels by adding new mailing lists as needed.
    /// </summary>
    /// <param name="dto">The DTO containing the details for the new mailing list.</param>
    /// <param name="userId">The ID of the user creating the mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created mailing list.</returns>
    Task<Mailinglist> CreateMailinglist(PostMailinglistDTO dto, Guid userId, CancellationToken ct);
    
    /// <summary>
    /// Updates an existing mailing list identified by its ID. This method allows authorized users to modify the details of an existing mailing list by providing the updated information in the form of a PostMailinglistDTO. The method accepts the ID of the mailing list to update, the DTO containing the updated details, the user ID for authorization purposes, and a CancellationToken for operation cancellation. If the update is successful, it returns void; if the mailing list is not found, it throws an exception. This functionality is crucial for users to maintain accurate and up-to-date mailing list information within the application.
    /// </summary>
    /// <param name="id">The ID of the mailing list to update.</param>
    /// <param name="dto">The DTO containing the updated details for the mailing list.</param>
    /// <param name="userId">The ID of the user updating the mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateMailinglist(int id, PostMailinglistDTO dto, Guid userId, CancellationToken ct);
    
    /// <summary>
    /// Deletes an existing mailing list identified by its ID. This method allows authorized users to remove a mailing list from the system. The method accepts the ID of the mailing list to delete, the user ID for authorization purposes, and a CancellationToken for operation cancellation. If the deletion is successful, it returns void; if the mailing list is not found, it throws an exception. This functionality is essential for users to manage their mailing lists effectively by removing those that are no longer needed.
    /// </summary>
    /// <param name="id">The ID of the mailing list to delete.</param>
    /// <param name="userId">The ID of the user deleting the mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteMailinglist(int id, Guid userId, CancellationToken ct);
    
    /// <summary>
    /// Partially updates an existing mailing list identified by its ID using a JSON Patch document. This method allows authorized users to modify specific fields of an existing mailing list without needing to provide the entire entity. The method accepts the ID of the mailing list to update, a JsonPatchDocument<Mailinglist> containing the update operations, the user ID for authorization purposes, and a CancellationToken for operation cancellation. If the patch is successful, it returns void; if the mailing list is not found, it throws an exception. This functionality provides flexibility for users to make partial updates to mailing lists efficiently.
    /// </summary>
    /// <param name="id">The ID of the mailing list to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the update operations.</param>
    /// <param name="userId">The ID of the user updating the mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PatchMailinglist(int id, JsonPatchDocument<Mailinglist> patchDoc, Guid userId, CancellationToken ct);
}