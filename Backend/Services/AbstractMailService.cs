using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Backend.Interfaces;

public abstract class AbstractMailService
{
    public abstract Task SendEmailAsync(PostMailDTO dto, Guid UserId, CancellationToken ct);
    private readonly Dictionary<uint, string> _roleMailMap;

    protected readonly PostgresDbContext _db;

    public AbstractMailService(PostgresDbContext db)
    {
        _db = db;
        _roleMailMap = new Dictionary<uint, string>();
        
        var settings = _db.Settings.ToList();

        foreach (Setting setting in settings)
        {
            if (setting.Name.StartsWith("ROLEMAILMAP_"))
            {
                string roleId = setting.Name.Replace("ROLEMAILMAP_", "");
                string email = setting.Value;
                
                _roleMailMap.Add(uint.Parse(roleId), email);
            }
        }
    }

    protected async Task<MailRecipient> GetSenderInfo(Guid userId, CancellationToken ct = default)
    {
        int boardGroupId = _db.Settings.Where(s => s.Name == "BoardGroupId").Select(s => int.Parse(s.Value)).FirstOrDefault();

        Role? role = await _db.GroupMemberships
            .Where(gm => gm.MemberId == userId && gm.GroupId == boardGroupId)
            .Select(gm => gm.RoleAlias != null ? gm.RoleAlias.Role : null)
            .FirstOrDefaultAsync(ct);

        if(role == null)
        {
            // to do: throw exception that user does not have a role in the board group and therefore cannot send emails
            return null!;
        }

        Member? sender = await _db.Members.FindAsync(userId, ct);
        if(sender == null)        
        {
            // to do: throw exception that sender member record was not found
            return null!;
        }

        return new MailRecipient { Mail = _roleMailMap[role.Id], Name = $"{sender.FirstName} {sender.LastName}" };
    }

    protected async Task<MailRecipient[]> ExtractRecipients(MailRecipient[]? recipients = null, uint? activityId = null, CancellationToken ct = default)
    {
        MailRecipient[] resultRecipients = recipients ?? Array.Empty<MailRecipient>();

        if(recipients == null || recipients.Length == 0)
        {
            if(activityId == null)
            {
                // to do: throw exception that either recipient mails or activity ID must be provided
                return null!;
            }

            Activity? activity = await _db.Activities.Include(a => a.Enrollments).ThenInclude(e => e.Member).FirstOrDefaultAsync(a => a.Id == activityId, ct);
            if(activity == null)
            {
                // to do: throw activity not found exception
                return null!;
            }

            foreach(var enrollment in activity.Enrollments)
            {
                resultRecipients = resultRecipients.Append(new MailRecipient { Mail = enrollment.Member.Email, Name = $"{enrollment.Member.FirstName} {enrollment.Member.LastName}" }).ToArray();
            }
        }
        return resultRecipients;
    }

    protected string StripHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        text = Regex.Replace(text, @"<(?:br\/?|\/p)>", "\r\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]*>", string.Empty);
        text = Regex.Replace(text, @" +", " ");
        return Regex.Replace(text, @"[\r\n]{2,}", "\r\n").Trim();
    }
}