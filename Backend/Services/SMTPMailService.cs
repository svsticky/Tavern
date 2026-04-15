using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services;

public class SMTPMailService(PostgresDbContext db) : AbstractMailService(db)
{
    private readonly string _host = Environment.GetEnvironmentVariable("SMTP_HOST")!;
    private readonly int _port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
    private readonly bool _startTls = Environment.GetEnvironmentVariable("SMTP_STARTTLS") == "true";
    private readonly string? _user = Environment.GetEnvironmentVariable("SMTP_USER");
    private readonly string? _pass = Environment.GetEnvironmentVariable("SMTP_PASS");

    public override async Task SendEmailAsync(PostMailDTO dto, Guid userId, CancellationToken ct)
    {
        MailRecipient[] recipients = await ExtractRecipients(dto.Recipients, dto.ActivityId, ct);

        if (recipients.Length == 0) return;

        MailRecipient? from = await GetSenderInfo(userId, ct);

        if(from == null)
        {
            throw new InvalidOperationException("Sender information could not be retrieved");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from.Name, from.Mail));
        message.Subject = dto.Subject;

        foreach (var recipient in recipients)
        {
            message.Bcc.Add(new MailboxAddress(recipient.Name, recipient.Mail));
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = dto.HtmlContent,
            TextBody = StripHtml(dto.HtmlContent)
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        MailKit.Security.SecureSocketOptions secureSocketOptions = _startTls ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto;
        await client.ConnectAsync(_host, _port, secureSocketOptions, ct);
        
        if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_pass))
        {
            await client.AuthenticateAsync(_user, _pass, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}