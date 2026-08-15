using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for managing member profiles and related member data.
    /// </summary>
    public interface IMemberService
    {
        /// <summary>
        /// Retrieves members visible to the requesting user.
        /// </summary>
        /// <param name="dto">The member query filters.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The members matching the supplied filters.</returns>
        Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a single member by user ID.
        /// </summary>
        /// <param name="userIdFromUserToGet">The user ID of the member to retrieve.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The member when found; otherwise <c>null</c>.</returns>
        Task<MemberResponseDTO?> GetMember(Guid userIdFromUserToGet, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new member.
        /// </summary>
        /// <param name="dto">The member payload.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created member entity.</returns>
        Task<Member> CreateMember(PostMemberDTO dto, Guid? userId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a member by user ID.
        /// </summary>
        /// <param name="id">The user ID of the member to delete.</param>
        /// <param name="userId">The ID of the user performing the delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Applies a JSON Patch document to a member.
        /// </summary>
        /// <param name="id">The user ID of the member to patch.</param>
        /// <param name="patchDoc">The patch document to apply.</param>
        /// <param name="userId">The ID of the user performing the update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces a member with the provided values.
        /// </summary>
        /// <param name="id">The user ID of the member to update.</param>
        /// <param name="dto">The replacement member payload.</param>
        /// <param name="userId">The ID of the user performing the update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the profile picture for a member.
        /// </summary>
        /// <param name="id">The user ID of the member whose picture is deleted.</param>
        /// <param name="userId">The ID of the user performing the delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteProfilePicture(Guid id, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes the email of a member.
        /// </summary>
        /// <param name="id">The user ID of the member whose email is refreshed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RefreshEmail(Guid id, CancellationToken cancellationToken);
    }
}
