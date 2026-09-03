using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// The outcome of a <see cref="IMemberService.SendActivationEmail"/> call.
    /// </summary>
    public enum ActivationEmailStatus
    {
        /// <summary>
        /// The activation email was queued to be sent.
        /// </summary>
        Sent,

        /// <summary>
        /// An activation email was already sent for this member previously; nothing was queued again.
        /// </summary>
        AlreadySent,

        /// <summary>
        /// The member isn't linked to the authentication system yet. The caller should retry shortly.
        /// </summary>
        Pending,

        /// <summary>
        /// The member has one or more membership payment attempts on record and none of them succeeded
        /// (e.g. cancelled or expired at the payment provider). Mollie redirects the browser back to the
        /// app regardless of the payment's actual outcome, so reaching this endpoint is not proof of
        /// payment - the caller should not treat this as a working account and should not retry.
        /// </summary>
        PaymentRequired
    }

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

        /// <summary>
        /// Retrieves the curated mailing lists together with the given member's current subscription status for each, fetched live from the mail subscription provider.
        /// </summary>
        /// <param name="id">The user ID of the member whose mailing lists are retrieved.</param>
        /// <param name="includeYearlyRenewal">Whether to also include YearlyRenewalOnly lists, not just General ones.</param>
        /// <param name="userId">The ID of the user performing the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<MemberMailinglistDto>> GetMemberMailinglists(Guid id, bool includeYearlyRenewal, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces a member's mailing list subscriptions within the given context with the given set of list IDs. Subscriptions to lists outside the given context (e.g. YearlyRenewalOnly lists, when updating from the General context) are left untouched.
        /// </summary>
        /// <param name="id">The user ID of the member whose subscriptions are updated.</param>
        /// <param name="subscribedListIds">The IDs of the mailing lists, within the given context, the member should be subscribed to.</param>
        /// <param name="includeYearlyRenewal">Whether the given IDs are being edited within the General+YearlyRenewalOnly context rather than just General.</param>
        /// <param name="userId">The ID of the user performing the update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateMemberMailinglists(Guid id, List<string> subscribedListIds, bool includeYearlyRenewal, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the one-time account-activation email (verify email + set password) for a member, if it hasn't
        /// been sent before and the member is linked to the authentication system yet. Safe to call repeatedly.
        /// </summary>
        /// <param name="id">The ID of the member to send the activation email for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<ActivationEmailStatus> SendActivationEmail(Guid id, CancellationToken cancellationToken);
    }
}
