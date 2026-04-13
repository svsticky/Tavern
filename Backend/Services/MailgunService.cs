using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Mailgun;
using System.Text.RegularExpressions;

namespace Backend.Services;

public class MailgunService(PostgresDbContext db) : AbstractMailService(db)
{
    private readonly string _privateKey = Environment.GetEnvironmentVariable("MAILGUN_TOKEN")!;
    private readonly string _publicKey = Environment.GetEnvironmentVariable("MAILGUN_PUBLIC_KEY")!;
    private readonly string _apiBaseUrl = Environment.GetEnvironmentVariable("MAILGUN_API_BASE_URL")!;

    public override async Task SendEmailAsync(PostMailDTO dto, Guid userId, CancellationToken ct)
    {
        MailRecipient[] recipients = await base.ExtractRecipients(dto.Recipients, dto.ActivityId, ct);

        if(recipients.Length == 0)
        {
            return;
        }

        MailRecipient from = await base.GetSenderInfo(userId, ct);

        using var client = new MailgunClient(_apiBaseUrl, _privateKey, _publicKey);
        
        MailgunMessage message = CreateMessage(from, recipients, dto.Subject, dto.HtmlContent);

        await client.SendMessageAsync(message);
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
            message.To.Add(new MailgunAddress(recipient.Mail, recipient.Name));
        }

        return message;
    }

    private string StripHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        text = Regex.Replace(text, @"<(?:br\/?|\/p)>", "\r\n", RegexOptions.IgnoreCase);
        
        text = Regex.Replace(text, @"<[^>]*>", string.Empty);
        
        text = Regex.Replace(text, @" +", " ");
        
        return Regex.Replace(text, @"[\r\n]{2,}", "\r\n").Trim();
    }
}