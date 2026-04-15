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

    protected override async Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from.Name, from.Mail));
        message.Subject = subject;

        foreach (var recipient in to)
        {
            message.Bcc.Add(new MailboxAddress(recipient.Name, recipient.Mail));
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlContent,
            TextBody = StripHtml(htmlContent)
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