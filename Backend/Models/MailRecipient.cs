namespace Backend.Models;

/// <summary>
/// Represents a mail recipient with their email address and name. This entity is used to manage and store information about individuals who are recipients of email communications within the system. The MailRecipient class includes properties for the recipient's email address (Mail) and their name (Name), allowing for personalized email communications and better organization of recipient information for various email-related functionalities, such as notifications, newsletters, or other forms of communication sent to members or users of the system.
/// </summary>
public class MailRecipient
{
    /// <summary>
    /// The unique identifier of the mail recipient, assigned incrementally.
    /// </summary>
    public string Mail { get; set; } = null!;

    /// <summary>
    /// The name of the recipient, which can be used for personalization in email communications. The Name property allows for a more personalized and engaging experience when sending emails, as it can be used to address the recipient directly in the email content, making the communication feel more tailored and relevant to the individual recipient. This can help improve engagement and response rates for email campaigns or notifications sent to recipients.
    /// </summary>
    public string Name { get; set; } = null!;
}
