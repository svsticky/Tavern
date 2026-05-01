using Backend.Models;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTOs for mail-related operations, including posting mails and activity mails. The PostMailDTO is used to transfer data from the client to the server when creating a new mail, containing the necessary information such as subject, HTML content, and recipients. The PostActivityMailDTO is used to transfer data for creating an activity-related mail, including the activity ID and an option to include waiting list members. These DTOs ensure that all required information is provided and validated appropriately for the mail creation process, allowing for effective communication through email based on the specified criteria and content.
/// </summary>
public abstract class AbstractPostMailDTO
{
    /// <summary>
    /// The subject of the mail to be sent. This field is required to provide a clear and concise subject line for the email, allowing recipients to understand the purpose and content of the mail at a glance. The subject should be relevant to the content of the mail and should effectively convey the main message or topic being communicated in the email.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// The HTML content of the mail to be sent. This field is required to provide the actual body of the email in HTML format, allowing for rich formatting and presentation of the mail content. The HTML content can include various elements such as text, images, links, and other formatting options to enhance the visual appeal and readability of the email, ensuring that the message is effectively communicated to the recipients based on the specified content and formatting. The HTML content should be well-structured and properly formatted to ensure that it renders correctly in different email clients and devices, providing a consistent and engaging experience for the recipients of the mail.
    /// </summary>
    public required string HtmlContent { get; set; }
}

/// <summary>
/// Defines the DTO for posting a mail, containing the necessary information for creating a new mail, including its subject, HTML content, and recipients. The PostMailDTO is used to transfer data from the client to the server when creating a new mail, ensuring that all required information is provided and validated appropriately for the creation process. The PostMailDTO allows for the specification of multiple recipients for the mail, enabling effective communication with multiple individuals or groups based on the provided recipient information in the request payload. The recipients can be specified using the MailRecipient class, which includes properties such as the recipient's email address and name, allowing for personalized and targeted communication through email based on the specified recipient details. The PostMailDTO ensures that the mail is created with the necessary information to effectively communicate the intended message to the specified recipients, providing a structured and validated approach to mail creation in the application.
/// </summary>
public class PostMailDTO : AbstractPostMailDTO
{
    /// <summary>
    /// The recipients of the mail to be sent. This field is required to specify the individuals or groups who will receive the email, allowing for effective communication with the intended audience based on the provided recipient information in the request payload. The recipients can be specified using the MailRecipient class, which includes properties such as the recipient's email address and name, enabling personalized and targeted communication through email based on the specified recipient details. The recipients field should contain valid email addresses and relevant recipient information to ensure that the mail is delivered successfully to the intended recipients and that the communication is effective and meaningful based on the provided recipient data in the request payload.
    /// </summary>
    public required MailRecipient[] Recipients { get; set; }
}

/// <summary>
/// Defines the DTO for posting an activity mail, containing the necessary information for creating a new mail related to a specific activity, including the activity ID and an option to include waiting list members. The PostActivityMailDTO is used to transfer data from the client to the server when creating an activity-related mail, ensuring that all required information is provided and validated appropriately for the creation process. The PostActivityMailDTO allows for the specification of whether to include waiting list members in the mail, enabling effective communication with both confirmed participants and those on the waiting list based on the provided criteria in the request payload. This DTO ensures that the activity mail is created with the necessary information to effectively communicate relevant updates or information about the specified activity to the intended recipients, providing a structured and validated approach to activity mail creation in the application.
/// </summary>
public class PostActivityMailDTO : AbstractPostMailDTO
{
    /// <summary>
    /// The unique identifier of the activity for which the mail is being created. This field is required to associate the mail with the correct activity within the system, allowing for effective communication about specific activities based on the provided activity ID in the request payload. The activity ID should correspond to an existing activity in the system to ensure that the mail is relevant and targeted to the appropriate audience based on their involvement or interest in the specified activity. The PostActivityMailDTO ensures that the mail is created with the necessary information to effectively communicate updates or information about the specified activity to the intended recipients, providing a structured and validated approach to activity mail creation in the application.
    /// </summary>
    public required uint ActivityId { get; set; }

    /// <summary>
    /// Indicates whether to include waiting list members in the mail. If set to true, both confirmed participants and those on the waiting list will receive the mail; if set to false, only confirmed participants will receive the mail. This field allows for filtering recipients based on their involvement in the activity, enabling effective communication with both confirmed participants and those on the waiting list based on the provided criteria in the request payload. The PostActivityMailDTO ensures that the activity mail is created with the necessary information to effectively communicate relevant updates or information about the specified activity to the intended recipients, providing a structured and validated approach to activity mail creation in the application.
    /// </summary>
    public bool IncludeWaitingList { get; set; }
}