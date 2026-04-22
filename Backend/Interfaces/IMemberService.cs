using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IMemberService
    {
        Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken);
        Task<MemberResponseDTO?> GetMember(Guid userIdFromUserToGet, Guid userId, CancellationToken cancellationToken);

        Task<Member> CreateMember(PostMemberDTO dto, CancellationToken cancellationToken);

        Task DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken);

        Task PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken);

        Task UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken);
                 
        Task DeleteProfilePicture(Guid id, Guid userId, CancellationToken cancellationToken);
    }
}