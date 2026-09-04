namespace Backend.Models;

/// <summary>
/// Represents a mail recipient with their email address and name. This entity is used to manage and store information about individuals who are recipients of email communications within the system. The MailRecipient class includes properties for the recipient's email address (Mail) and their name (Name), allowing for personalized email communications and better organization of recipient information for various email-related functionalities, such as notifications, newsletters, or other forms of communication sent to members or users of the system.
/// </summary>
public class MailRecipient
{
    /// <summary>
    /// The email address of the mail recipient. This property is required and is used to identify the recipient for email communications. It should be a valid email address format, and it serves as the primary means of contacting the recipient through email.
    /// </summary>
    public required string Mail { get; set; }

    /// <summary>
    /// The recipient's first name, shown in mail clients as the sender/recipient name (e.g. in the From/To headers). Mails never address recipients by last name, so this holds the first name only.
    /// </summary>
    public required string Name { get; set; }
}
