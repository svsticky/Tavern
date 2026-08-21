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
        logger.LogInformation("Sending SMTP email from {From} to {RecipientCount} recipients.", from.Mail, to.Length);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from.Name, from.Mail));
        message.Subject = subject;

        // A single recipient goes in To so their mail client shows who the mail was sent to; multiple
        // recipients (e.g. an activity mail to a whole group) go in Bcc so they don't see each other's address.
        if (to.Length == 1)
        {
            message.To.Add(new MailboxAddress(to[0].Name, to[0].Mail));
        }
        else
        {
            foreach (var recipient in to)
            {
                message.Bcc.Add(new MailboxAddress(recipient.Name, recipient.Mail));
            }
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlContent,
            TextBody = StripHtml(htmlContent)
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        MailKit.Security.SecureSocketOptions secureSocketOptions = StartTls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto;
        await client.ConnectAsync(Host, Port, secureSocketOptions, ct);

        if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Pass))
        {
            await client.AuthenticateAsync(User, Pass, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        logger.LogInformation("SMTP email sent successfully to {RecipientCount} recipients.", to.Length);
    }
}
