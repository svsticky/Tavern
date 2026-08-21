using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Backend.Services.MailServices;

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

    /// <summary>
    /// The database context used for retrieving settings and user information necessary for email operations, such as determining the sender's email address based on their role and fetching recipient details. This context allows the mail service to interact with the database to access relevant data required for composing and sending emails effectively.
    /// </summary>
    protected readonly PostgresDbContext _db;

    /// <summary>
    /// The payment validation service used for validating payments when sending emails related to financial transactions, such as outstanding payment notifications. This service allows the mail service to ensure that any payment-related information included in emails is accurate and up-to-date, providing recipients with reliable information about their financial obligations or statuses.
    /// </summary>
    protected readonly IPaymentValidationService _paymentValidationService;

    /// <summary>
    /// The permission service used for enforcing access control on email sending operations, ensuring that only authorized users can send emails through the system. This service allows the mail service to check the permissions of the requesting user before allowing them to send emails, helping to maintain security and prevent unauthorized use of the email functionality within the application.
    /// </summary>
    protected readonly IPermissionService _permissionService;

    /// <summary>
    /// The logger used for logging important events and errors that occur during email operations, providing visibility into the mail service's behavior and facilitating troubleshooting and monitoring of email-related activities. This logger allows the mail service to record significant actions, such as when emails are sent, who initiated the sending, and any issues that arise during the process, helping developers and administrators keep track of email operations and identify any problems that may need attention.
    /// </summary>
    protected readonly ILogger<AbstractMailService> _logger;

    /// <summary>
    /// Initializes a new instance of the AbstractMailService class with the specified database context, payment validation service, permission service, and logger. The constructor sets up the necessary dependencies for the mail service to function correctly, allowing it to interact with the database for retrieving settings and user information, validate payments when necessary, perform permission checks to ensure that only authorized users can send emails, and log important events and errors that occur during email operations for monitoring and debugging purposes. Additionally, the constructor initializes a mapping of role IDs to email addresses based on settings retrieved from the database, which can be used to determine the sender's email address based on their role when sending emails.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="paymentValidationService">The payment validation service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="logger">The logger.</param>
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

        if (dto.Recipients.Length == 0)
        {
            _logger.LogInformation("Skipping mail send by user {UserId} because recipient list is empty.", UserId);
            return;
        }

        MailRecipient? from = await GetSenderInfo(UserId, ct);

        if (from == null)
        {
            throw new InvalidOperationException("Sender information could not be retrieved");
        }

        await SendEmailCoreAsync(from, dto.Recipients, dto.Subject, BuildHtmlEmail(dto.HtmlContent), ct);
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
    /// Sends an enrollment promotion email to a member who has been promoted from the waiting list to an activity, informing them of their successful enrollment and providing relevant details about the activity. The email content is tailored to the member's preferred language, ensuring clear communication and a personalized touch. This method is typically called after a member is moved from the waiting list to the enrolled list for an activity, allowing the organization to promptly notify the member of their updated enrollment status and encourage their participation in the activity.
    /// </summary>
    /// <param name="promotedEnrollment">The enrollment that has been promoted from the waiting list to enrolled status.</param>
    /// <returns>A task representing the asynchronous operation of sending the enrollment promotion email.</returns>
    public virtual async Task SendEnrollmentPromotionEmail(Enrollment promotedEnrollment)
    {
        var sender = _db.Settings.Where(s => s.Name == "ActivityUpdateEmailSender").Select(s => s.Value).FirstOrDefault();
        if (string.IsNullOrEmpty(sender))
        {
            _logger.LogWarning("ActivityUpdateEmailSender setting is not configured. Skipping enrollment promotion mail.");
            return;
        }

        string subject = promotedEnrollment.Member.PreferredLanguage switch
        {
            Language.NL => $"Je inschrijving voor {promotedEnrollment.Activity.Name} is bevestigd!",
            Language.EN => $"Your enrollment for {promotedEnrollment.Activity.Name} is confirmed!",
            _ => throw new InvalidOperationException("Unsupported language")
        };
        string htmlContent = MailTemplateLoader.Render($"{LanguageFolder(promotedEnrollment.Member.PreferredLanguage)}/EnrollmentPromotion.html", new Dictionary<string, string>
        {
            ["FirstName"] = promotedEnrollment.Member.FirstName,
            ["ActivityName"] = promotedEnrollment.Activity.Name
        });

        await SendEmailCoreAsync(new MailRecipient { Mail = sender, Name = sender }, new[] { new MailRecipient { Mail = promotedEnrollment.Member.Email, Name = promotedEnrollment.Member.FirstName } }, subject, BuildHtmlEmail(htmlContent, promotedEnrollment.Member.PreferredLanguage), CancellationToken.None);
    }

    /// <summary>
    /// Builds outstanding-payment email content for members with unpaid enrollments.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of sending outstanding payment emails to members with unpaid enrollments.</returns>
    public async Task SendOutstandingPaymentMails()
    {
        var sender = _db.Settings.Where(s => s.Name == "FinancialEmailSender").Select(s => s.Value).FirstOrDefault();
        if (string.IsNullOrEmpty(sender))
        {
            _logger.LogWarning("FinancialEmailSender setting is not configured. Skipping outstanding payment mails.");
            return;
        }

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

            string htmlContent = MailTemplateLoader.Render($"{LanguageFolder(language)}/OutstandingPayment.html", new Dictionary<string, string>
            {
                ["FirstName"] = member.FirstName,
                ["ActivityList"] = string.Join("", balances.Select(b => $"<li>{b.Enrollment.Activity.Name}: €{b.Balance}</li>")),
                ["HostUrl"] = Environment.GetEnvironmentVariable("HostUrl") ?? ""
            });

            await SendEmailCoreAsync(new MailRecipient { Mail = sender, Name = sender }, new[] { new MailRecipient { Mail = member.Email, Name = $"{member.FirstName} {member.LastName}" } }, subject, BuildHtmlEmail(htmlContent, language), CancellationToken.None);
        }
    }

    /// <summary>
    /// Sends study status update emails to members whose study enrollments are approaching their nominal duration, prompting them to update their study status on the platform. The method retrieves the sender's email address from the settings and identifies members with study enrollments that are nearing their nominal duration based on the current date. For each member identified, an email is composed in their preferred language, encouraging them to visit the platform and update their study status if necessary. This proactive communication helps ensure that members keep their study information up-to-date, which can be important for various administrative and organizational purposes within the system.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported language is encountered.</exception>
    public async Task SendStudyStatusUpdateMails()
    {
        var sender = _db.Settings.Where(s => s.Name == "MainBoardMail").Select(s => s.Value).FirstOrDefault();
        if (string.IsNullOrEmpty(sender))
        {
            _logger.LogWarning("MainBoardMail setting is not configured. Skipping study status update mails.");
            return;
        }

        var potentialMembers = _db.Members
            .Include(m => m.StudyEnrollments)
            .ThenInclude(se => se.Study)
            .Where(m => !m.IsDeleted)
            .ToList();

        var membersWithoutActiveStudy = potentialMembers
            .Where(m => !m.StudyEnrollments.Any(se => se.Status == StudyStatus.Enrolled))
            .ToList();

        foreach (var member in membersWithoutActiveStudy)
        {
            var language = member.PreferredLanguage;

            string subject = language switch
            {
                Language.NL => "Controleer lidmaatschap en studievoortgang",
                Language.EN => "Check membership and study progress",
                _ => throw new InvalidOperationException("Unsupported language")
            };

            string htmlContent = MailTemplateLoader.Render($"{LanguageFolder(language)}/StudyStatusNoActiveStudy.html", new Dictionary<string, string>
            {
                ["FirstName"] = member.FirstName,
                ["HostUrl"] = Environment.GetEnvironmentVariable("HostUrl") ?? ""
            });

            await SendEmailCoreAsync(new MailRecipient { Mail = sender, Name = sender }, new[] { new MailRecipient { Mail = member.Email, Name = $"{member.FirstName} {member.LastName}" } }, subject, BuildHtmlEmail(htmlContent, language), CancellationToken.None);
        }

        var membersWithOutstandingStudies = potentialMembers
            .Where(m =>
                m.StudyEnrollments.Any(se =>
                    se.Status != StudyStatus.Completed &&
                    se.EnrollmentDate.AddYears((int)se.Study.NominalDurationYears) < DateTime.Now))
            .ToList();

        foreach (var member in membersWithOutstandingStudies)
        {
            var language = member.PreferredLanguage;

            string subject = language switch
            {
                Language.NL => "Controleer lidmaatschap en studievoortgang",
                Language.EN => "Check membership and study progress",
                _ => throw new InvalidOperationException("Unsupported language")
            };

            string htmlContent = MailTemplateLoader.Render($"{LanguageFolder(language)}/StudyStatusApproachingDuration.html", new Dictionary<string, string>
            {
                ["FirstName"] = member.FirstName,
                ["HostUrl"] = Environment.GetEnvironmentVariable("HostUrl") ?? ""
            });

            await SendEmailCoreAsync(new MailRecipient { Mail = sender, Name = sender }, new[] { new MailRecipient { Mail = member.Email, Name = $"{member.FirstName} {member.LastName}" } }, subject, BuildHtmlEmail(htmlContent, language), CancellationToken.None);
        }
    }

    /// <summary>
    /// Retrieves the sender information, including email address and name, for the specified user ID by checking their group memberships and associated roles. The method ensures that the user has the necessary permissions to send emails by verifying their membership in either the board group or candidate board group for the current financial year. If the user is authorized, their email address is determined based on their role using a predefined mapping, and their full name is constructed from their first and last name. If the user does not have permission to send emails or if their information cannot be retrieved, appropriate exceptions are thrown or null is returned.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to retrieve sender information.</param>
    /// <param name="ct">The cancellation token for the asynchronous operation.</param>
    /// <returns>The mail recipient information for the sender, or null if not found.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user does not have permission to send emails.</exception>
    protected async Task<MailRecipient?> GetSenderInfo(Guid userId, CancellationToken ct = default)
    {
        int boardGroupId = _db.Settings.Where(s => s.Name == "BoardGroupId").Select(s => int.Parse(s.Value)).FirstOrDefault();
        int candidateBoardGroupId = _db.Settings.Where(s => s.Name == "CandidateBoardGroupId").Select(s => int.Parse(s.Value)).FirstOrDefault();

        Role? role = await _db.GroupMemberships
            .Where(gm => gm.MemberId == userId && (gm.GroupId == boardGroupId || gm.GroupId == candidateBoardGroupId) && gm.MembershipYear == YearUtils.GetBoardYear(_db))
            .Select(gm => gm.RoleAlias != null ? gm.RoleAlias.Role : null)
            .FirstOrDefaultAsync(ct);

        if (role == null)
        {
            throw new UnauthorizedAccessException("User does not have permission to send mails");
        }

        Member? sender = await _db.Members.FindAsync(userId, ct);
        if (sender == null)
        {
            return null;
        }

        return new MailRecipient { Mail = _roleMailMap[role.Id], Name = $"{sender.FirstName} {sender.LastName}" };
    }

    /// <summary>
    /// Retrieves the recipients for an activity-based email by fetching the enrollments for the specified activity and determining the email addresses of the enrolled members. The method takes into account whether to include members on the waiting list based on the provided flag and constructs an array of mail recipients with their email addresses and names. If the activity is not found, an exception is thrown. This method is used to gather the appropriate recipients when sending emails related to specific activities, such as updates or notifications for enrolled members.
    /// </summary>
    /// <param name="activityId">The ID of the activity for which to retrieve recipients.</param>
    /// <param name="includeWaitingList">A flag indicating whether to include members on the waiting list.</param>
    /// <param name="ct">The cancellation token for the asynchronous operation.</param>
    /// <returns>An array of mail recipients for the specified activity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity is not found.</exception>
    protected async Task<MailRecipient[]> GetRecipientsFromActivity(uint activityId, bool includeWaitingList, CancellationToken ct)
    {
        MailRecipient[] resultRecipients = Array.Empty<MailRecipient>();

        Activity? activity = await _db.Activities.Where(a => a.Id == activityId).Include(a => a.Enrollments.Where(e => includeWaitingList || !e.IsOnWaitingList)).ThenInclude(e => e.Member).FirstOrDefaultAsync(ct);
        if (activity == null)
        {
            throw new InvalidOperationException("Activity not found");
        }

        foreach (var enrollment in activity.Enrollments)
        {
            resultRecipients = resultRecipients.Append(new MailRecipient { Mail = enrollment.Member.Email, Name = $"{enrollment.Member.FirstName} {enrollment.Member.LastName}" }).ToArray();
        }
        return resultRecipients;
    }

    /// <summary>
    /// Wraps a fragment of email body HTML in the association's branded layout: a colored header bar showing the board logo, the body content, and a footer linking to the frontend host and the main board mailbox. Colors and addresses are read from settings/environment on every call so that board-configured branding (e.g. after an admin changes BoardPrimary) is always reflected without a restart.
    /// </summary>
    /// <param name="bodyContent">The inner HTML for the message body.</param>
    /// <param name="language">The language to render the footer copy in.</param>
    /// <returns>A complete, styled HTML document ready to be used as an email body.</returns>
    protected string BuildHtmlEmail(string bodyContent, Language language = Language.NL)
    {
        string? boardPrimarySetting = _db.Settings.Where(s => s.Name == "BoardPrimary").Select(s => s.Value).FirstOrDefault();
        string boardPrimary = string.IsNullOrEmpty(boardPrimarySetting) ? "#fa6b20" : boardPrimarySetting;

        string mainBoardMail = _db.Settings.Where(s => s.Name == "MainBoardMail").Select(s => s.Value).FirstOrDefault() ?? "";
        string hostUrl = Environment.GetEnvironmentVariable("HostUrl") ?? "";
        string logoUrl = Environment.GetEnvironmentVariable("LOGO_URL") ?? "";

        string hostDisplay = Regex.Replace(hostUrl, @"^https?:\/\/", "");

        string headerContent = string.IsNullOrEmpty(logoUrl)
            ? string.Empty
            : $"<img src='{logoUrl}' alt='Logo' style='max-height: 40px; vertical-align: middle;' />";

        string languageFolder = LanguageFolder(language);

        string footerText = MailTemplateLoader.Render($"{languageFolder}/Footer.html", new Dictionary<string, string>
        {
            ["HostUrl"] = hostUrl,
            ["HostDisplay"] = hostDisplay,
            ["MainBoardMail"] = mainBoardMail
        });

        return MailTemplateLoader.Render("Layout.html", new Dictionary<string, string>
        {
            ["BoardPrimary"] = boardPrimary,
            ["HeaderContent"] = headerContent,
            ["BodyContent"] = bodyContent,
            ["FooterText"] = footerText
        });
    }

    /// <summary>
    /// Maps a member's preferred language to the corresponding mail-template folder name.
    /// </summary>
    private static string LanguageFolder(Language language) => language switch
    {
        Language.NL => "nl",
        Language.EN => "en",
        _ => throw new InvalidOperationException("Unsupported language")
    };

    /// <summary>
    /// Strips HTML tags from the provided text and converts it to plain text by replacing line breaks and multiple spaces with appropriate formatting. This method is useful for generating plain text versions of email content that may originally be in HTML format, ensuring that the resulting text is clean and readable without any HTML tags or excessive whitespace. The method uses regular expressions to identify and replace HTML elements and whitespace patterns effectively.
    /// </summary>
    /// <param name="text">The text from which to strip HTML tags.</param>
    /// <returns>The plain text version of the input text.</returns>
    protected string StripHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Replace breaks, closing paragraphs, opening paragraphs, and closing headers with a newline
        text = Regex.Replace(text, @"<(?:br\/?|\/p|<p>|\/h[1-6])>", "\r\n", RegexOptions.IgnoreCase);

        // Strip out all remaining HTML tags
        text = Regex.Replace(text, @"<[^>]*>", string.Empty);

        // Clean up excessive spacing
        text = Regex.Replace(text, @" +", " ");
        return Regex.Replace(text, @"[\r\n]{2,}", "\r\n").Trim();
    }
}
