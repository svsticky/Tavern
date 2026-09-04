using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

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
    ILogger<AbstractMailService> baseLogger,
    IHttpClientFactory httpClientFactory) : AbstractMailService(db, paymentValidationService, permissionService, baseLogger)
{
    private string PrivateKey => _db.Settings.Find("MailgunToken")?.Value ?? "";
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
        if (to.Length == 0)
        {
            return;
        }

        logger.LogInformation("Sending Mailgun email from {From} to {RecipientCount} recipients.", from.Mail, to.Length);

        var domain = from.Mail.Split("@")[1];
        var baseUrl = ApiBaseUrl.TrimEnd('/');
        var endpoint = $"{baseUrl}/{domain}/messages";
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var httpClient = httpClientFactory.CreateClient();
        var authBytes = Encoding.ASCII.GetBytes($"api:{PrivateKey}");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var fromFormatted = string.IsNullOrWhiteSpace(from.Name) ? from.Mail : $"{from.Name} <{from.Mail}>";

        foreach (var recipient in to)
        {
            using var formData = new MultipartFormDataContent();

            var recipientName = recipient.Name ?? "";
            var personalizedHtml = htmlContent.Replace("%name%", recipientName);
            var personalizedSubject = subject.Replace("%name%", recipientName);

            var toFormatted = string.IsNullOrWhiteSpace(recipient.Name)
                ? recipient.Mail
                : $"{recipient.Name} <{recipient.Mail}>";

            formData.Add(new StringContent(fromFormatted), "from");
            formData.Add(new StringContent(toFormatted), "to");
            formData.Add(new StringContent(personalizedSubject), "subject");
            formData.Add(new StringContent(personalizedHtml), "html");
            formData.Add(new StringContent(StripHtml(personalizedHtml)), "text");

            if (isDevelopment)
            {
                formData.Add(new StringContent("true"), "o:testmode");
            }

            var response = await httpClient.PostAsync(endpoint, formData, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Mailgun API error ({StatusCode}) for {Recipient}: {ErrorBody}",
                    response.StatusCode, recipient.Mail, errorBody);
                response.EnsureSuccessStatusCode();
            }
        }

        logger.LogInformation("Mailgun email sent successfully to {RecipientCount} recipients.", to.Length);
    }
}
