using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IMemberService
    {
        Task<List<MemberResponseDTO>> GetMembers(Guid userId, CancellationToken cancellationToken);
        Task<MemberResponseDTO?> GetMember(Guid id, Guid userId, bool isBoard, CancellationToken cancellationToken);

        Task<Member> CreateMember(PostMemberDTO dto, CancellationToken cancellationToken);

        Task<bool> DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken);

        Task<bool> PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken);

        Task<bool> UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken);
        
        Task<Member?> GetMemberEntity(Guid id);
        
        Task<FileResultDto?> GetProfilePictureFile(string path);
 
        Task<bool> DeleteProfilePicture(Guid id);
        
        bool IsBoard(Guid userId);
    }
}