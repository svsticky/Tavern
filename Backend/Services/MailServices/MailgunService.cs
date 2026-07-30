using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Mailgun;
using System.Diagnostics.CodeAnalysis;

namespace Backend.Services.MailServices;

/// <summary>
/// Sends emails through the Mailgun provider.
/// </summary>
[ExcludeFromCodeCoverage]
public class MailgunService(
    PostgresDbContext db,
    IPaymentValidationService paymentValidationService,
    IPermissionService permissionService,
    ILogger<MailgunService> logger,
    ILogger<AbstractMailService> baseLogger) : AbstractMailService(db, paymentValidationService, permissionService, baseLogger)
{
    private string PrivateKey => _db.Settings.Find("MailgunToken")?.Value ?? "";
    private string PublicKey => _db.Settings.Find("MailgunPublicKey")?.Value ?? "";
    private string ApiBaseUrl => _db.Settings.Find("MailgunApiBaseUrl")?.Value ?? "";

    /// <summary>
    /// Sends an email through Mailgun.
    /// </summary>
    /// <param name="from">The sender information.</param>
    /// <param name="to">The recipients.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlContent">The HTML email body.</param>
    /// <param name="ct">The cancellation token.</param>
    protected override async Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct)
    {
        logger.LogInformation("Sending Mailgun email from {From} to {RecipientCount} recipients.", from.Mail, to.Length);
        using var client = new MailgunClient(ApiBaseUrl, PrivateKey, PublicKey);
        
        MailgunMessage message = CreateMessage(from, to, subject, htmlContent);

        await client.SendMessageAsync(message);
        logger.LogInformation("Mailgun email sent successfully to {RecipientCount} recipients.", to.Length);
    }

    private MailgunMessage CreateMessage(MailRecipient from, MailRecipient[] to, string subject, string htmlContent)
    {
        var message = new MailgunMessage(from.Mail.Split("@")[1])
        {
            From = new MailgunAddress(from.Mail, from.Name),
            Subject = subject,
            HTML = htmlContent,
            Text = StripHtml(htmlContent),
            TestMode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
        };

        foreach (var recipient in to)
        {
            message.BCC.Add(new MailgunAddress(recipient.Mail, recipient.Name));
        }

        return message;
    }
}
