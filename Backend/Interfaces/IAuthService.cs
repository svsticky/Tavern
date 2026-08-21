using Backend.Models.Domain;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the auth-provider operations used to keep local members in sync.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Synchronizes local member data to an existing auth user.
        /// </summary>
        /// <param name="authSystemId">The auth system user ID.</param>
        Task SyncMember(Guid authSystemId);

        /// <summary>
        /// Creates an auth user for a local member.
        /// </summary>
        /// <param name="member">The member to provision.</param>
        /// <returns>The created auth user ID when successful.</returns>
        Task<Guid?> CreateUser(Member member);

        /// <summary>
        /// Deletes an auth user.
        /// </summary>
        /// <param name="authSystemId">The auth system user ID.</param>
        Task DeleteUser(Guid authSystemId);

        /// <summary>
        /// Gets the email of a user in the auth system.
        /// </summary> 
        /// <param name="authSystemId">The auth system user ID.</param>
        /// <returns>The email address.</returns>
        Task<string> GetEmail(Guid authSystemId);

        /// <summary>
        /// Refreshes the local member email from the auth system.
        /// </summary>
        /// <param name="authSystemId">The auth system user ID.</param>
        Task RefreshEmail(Guid authSystemId);

        /// <summary>
        /// Sends a combined email-verification and set-password action email to a user.
        /// </summary>
        /// <param name="authSystemId">The auth system user ID.</param>
        Task SendActivationEmail(Guid authSystemId);
    }
}
