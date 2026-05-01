using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Backend.Validators;
using Backend.Utils.DateTime;
using Microsoft.Extensions.Logging;

namespace Backend.Interfaces;

/// <summary>
/// Provides shared mail-sending workflow and recipient resolution logic.
/// </summary>
public abstract class AbstractMailService
{
    /// <summary>
    /// Sends a composed email through the underlying provider implementation.
    /// </summary>
    /// <param name="from">The sender information.</param>
    /// <param name="to">The recipients.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlContent">The HTML email body.</param>
    /// <param name="ct">The cancellation token.</param>
    protected abstract Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct);
    
    private readonly Dictionary<uint, string> _roleMailMap;

    protected readonly PostgresDbContext _db;

    protected readonly IPaymentValidationService _paymentValidationService;

    protected readonly IPermissionService _permissionService;
    protected readonly ILogger<AbstractMailService> _logger;

    public AbstractMailService(
        PostgresDbContext db,
        IPaymentValidationService paymentValidationService,
        IPermissionService permissionService,
        ILogger<AbstractMailService> logger)
    {
        _db = db;
        _paymentValidationService = paymentValidationService;
        _permissionService = permissionService;
        _logger = logger;
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

    /// <summary>
    /// Sends an email to the provided recipients on behalf of an authorized user.
    /// </summary>
    /// <param name="dto">The outgoing email payload.</param>
    /// <param name="UserId">The requesting user ID.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task SendEmailAsync(PostMailDTO dto, Guid UserId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(UserId);
        _logger.LogInformation("Preparing mail send by user {UserId} to {RecipientCount} recipients.", UserId, dto.Recipients.Length);

        if(dto.Recipients.Length == 0)
        {
            _logger.LogInformation("Skipping mail send by user {UserId} because recipient list is empty.", UserId);
            return;
        }

        MailRecipient? from = await GetSenderInfo(UserId, ct);

        if(from == null)
        {
            throw new InvalidOperationException("Sender information could not be retrieved");
        }

        await SendEmailCoreAsync(from, dto.Recipients, dto.Subject, dto.HtmlContent, ct);
        _logger.LogInformation("Completed mail send by user {UserId} to {RecipientCount} recipients.", UserId, dto.Recipients.Length);
    }

    /// <summary>
    /// Sends an email to activity participants based on activity-mail options.
    /// </summary>
    /// <param name="dto">The activity email payload.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task SendEmailAsync(PostActivityMailDTO dto, Guid userId, CancellationToken ct)
    {
        MailRecipient[] recipients = await GetRecipientsFromActivity(dto.ActivityId, dto.IncludeWaitingList, ct);

        await SendEmailAsync(new PostMailDTO
        {
            Recipients = recipients,
            Subject = dto.Subject,
            HtmlContent = dto.HtmlContent
        }, userId, ct);
    }

    /// <summary>
    /// Builds outstanding-payment email content for members with unpaid enrollments.
    /// </summary>
    public void SendOutstandingPaymentMails()
    {
        var unpaidEnrollmentBalances = _paymentValidationService.GetAllUnpaidEnrollments();

        if (unpaidEnrollmentBalances.Count() == 0) return;

        var memberEnrollmentBalances = unpaidEnrollmentBalances
            .GroupBy(ueb => ueb.Enrollment.Member)
            .ToDictionary(g => g.Key, g => g.ToArray());

        foreach (var kvp in memberEnrollmentBalances)
        {
            Member member = kvp.Key;
            EnrollmentBalance[] balances = kvp.Value;
            Language language = member.PreferredLanguage;

            string subject = language switch
            {
                Language.NL => "Openstaande betalingen voor activiteiten",
                Language.EN => "Outstanding payments for activities",
                _ => throw new InvalidOperationException("Unsupported language")
            };

            string htmlContent = language switch
            {
                Language.NL => $"Beste {member.FirstName},<br><br>Je hebt nog openstaande betalingen voor de volgende activiteiten:<br><ul>{string.Join("", balances.Select(b => $"<li>{b.Enrollment.Activity.Name}: €{b.Balance}</li>"))}</ul><br>Gelieve deze zo snel mogelijk te voldoen op https://koala.svsticky.nl.<br><br>Met vriendelijke groet,<br>Het bestuur",
                Language.EN => $"Dear {member.FirstName},<br><br>You have outstanding payments for the following activities:<br><ul>{string.Join("", balances.Select(b => $"<li>{b.Enrollment.Activity.Name}: €{b.Balance}</li>"))}</ul><br>Please settle these as soon as possible at https://koala.svsticky.nl.<br><br>Best regards,<br>The board",
                _ => throw new InvalidOperationException("Unsupported language")
            };
        }
    }

    protected async Task<MailRecipient?> GetSenderInfo(Guid userId, CancellationToken ct = default)
    {
        int boardGroupId = _db.Settings.Where(s => s.Name == "BoardGroupId").Select(s => int.Parse(s.Value)).FirstOrDefault();
        int candidateBoardGroupId = _db.Settings.Where(s => s.Name == "CandidateBoardGroupId").Select(s => int.Parse(s.Value)).FirstOrDefault();

        Role? role = await _db.GroupMemberships
            .Where(gm => gm.MemberId == userId && (gm.GroupId == boardGroupId || gm.GroupId == candidateBoardGroupId) && gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear())
            .Select(gm => gm.RoleAlias != null ? gm.RoleAlias.Role : null)
            .FirstOrDefaultAsync(ct);

        if(role == null)
        {
            throw new UnauthorizedAccessException("User does not have permission to send mails");
        }

        Member? sender = await _db.Members.FindAsync(userId, ct);
        if(sender == null)
        {
            return null;
        }

        return new MailRecipient { Mail = _roleMailMap[role.Id], Name = $"{sender.FirstName} {sender.LastName}" };
    }

    protected async Task<MailRecipient[]> GetRecipientsFromActivity(uint activityId, bool includeWaitingList, CancellationToken ct)
    {
        MailRecipient[] resultRecipients = Array.Empty<MailRecipient>();

        Activity? activity = await _db.Activities.Where(a => a.Id == activityId).Include(a => a.Enrollments.Where(e => includeWaitingList || !e.IsOnWaitingList)).ThenInclude(e => e.Member).FirstOrDefaultAsync(ct);
        if(activity == null)
        {
            throw new InvalidOperationException("Activity not found");
        }

        foreach(var enrollment in activity.Enrollments)
        {
            resultRecipients = resultRecipients.Append(new MailRecipient { Mail = enrollment.Member.Email, Name = $"{enrollment.Member.FirstName} {enrollment.Member.LastName}" }).ToArray();
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
