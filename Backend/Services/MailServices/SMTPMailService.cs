using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System.Diagnostics.CodeAnalysis;

namespace Backend.Services.MailServices;

/// <summary>
/// Sends emails through an SMTP server.
/// </summary>
[ExcludeFromCodeCoverage]
public class SMTPMailService(
    PostgresDbContext db,
    IPaymentValidationService paymentValidationService,
    IPermissionService permissionService,
    ILogger<SMTPMailService> logger,
    ILogger<AbstractMailService> baseLogger) : AbstractMailService(db, paymentValidationService, permissionService, baseLogger)
{
    private string Host => _db.Settings.Find("SmtpHost")?.Value ?? "";
    private int Port => int.TryParse(_db.Settings.Find("SmtpPort")?.Value, out var port) ? port : 587;
    private bool StartTls => _db.Settings.Find("SmtpStartTls")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;
    private string? User => _db.Settings.Find("SmtpUser")?.Value;
    private string? Pass => _db.Settings.Find("SmtpPass")?.Value;
    private int? MaxBatchSize => int.TryParse(_db.Settings.Find("SmtpMaxBatchSize")?.Value, out var size) && size > 0 ? size : null;

    /// <summary>
    /// Sends an email through SMTP.
    /// </summary>
    /// <param name="from">The sender information.</param>
    /// <param name="to">The recipients.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlContent">The HTML email body.</param>
    /// <param name="ct">The cancellation token.</param>
    protected override async Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct)
    {
        if (to.Length == 0)
        {
            return;
        }

        logger.LogInformation("Sending SMTP email from {From} to {RecipientCount} recipients.", from.Mail, to.Length);

        using var client = new SmtpClient();

        MailKit.Security.SecureSocketOptions secureSocketOptions = StartTls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto;
        await client.ConnectAsync(Host, Port, secureSocketOptions, ct);

        if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Pass))
        {
            await client.AuthenticateAsync(User, Pass, ct);
        }

        foreach (var batch in to.Chunk(MaxBatchSize ?? to.Length))
        {
            var personalizedHtml = ApplyNamePlaceholder(htmlContent, batch);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(from.Name, from.Mail));
            message.Subject = subject;

            // A single recipient goes in To so their mail client shows who the mail was sent to; multiple
            // recipients (e.g. an activity mail to a whole group) go in Bcc so they don't see each other's address.
            if (batch.Length == 1)
            {
                message.To.Add(new MailboxAddress(batch[0].Name, batch[0].Mail));
            }
            else
            {
                foreach (var recipient in batch)
                {
                    message.Bcc.Add(new MailboxAddress(recipient.Name, recipient.Mail));
                }
            }

            message.Body = new BodyBuilder
            {
                HtmlBody = personalizedHtml,
                TextBody = StripHtml(personalizedHtml)
            }.ToMessageBody();

            await client.SendAsync(message, ct);
        }

        await client.DisconnectAsync(true, ct);
        logger.LogInformation("SMTP email sent successfully to {RecipientCount} recipients.", to.Length);
    }

    /// <summary>
    /// Removes the "%name%" placeholder from the mail body. SMTP sends a single message per batch (To for one
    /// recipient, Bcc for several), so there is no single name to substitute per recipient - the placeholder is
    /// simply stripped rather than personalized. Mailgun personalizes per recipient instead via its own
    /// recipient-variables mail merge.
    /// </summary>
    /// <param name="htmlContent">The mail body possibly containing a "%name%" placeholder.</param>
    /// <param name="batch">The recipients the resulting body will be sent to.</param>
    /// <returns>The mail body with the placeholder removed.</returns>
    private static string ApplyNamePlaceholder(string htmlContent, MailRecipient[] batch)
    {
        return htmlContent
            .Replace(" %name% ", " ")
            .Replace(" %name%", "")
            .Replace("%name% ", "")
            .Replace("%name%", "");
    }
}
