namespace Backend.Models;

/// <summary>
/// Represents a mail subscription type that members can subscribe to for receiving different categories of email communications. The MailSubscriptions enum defines various subscription options, such as GeneralMemberMeetings, CompanyMails, MondayMorningMails, LecturesAndWorkshops, and TeacherMails. Each subscription type is represented as a flag, allowing members to subscribe to multiple categories of emails by combining the flags using bitwise operations. This entity is used to manage and track the email preferences of members, enabling personalized and relevant email communications based on their selected subscriptions.
/// </summary>
[Flags]
public enum MailSubscriptions : uint
{
    /// <summary>
    /// Represents no mail subscriptions, indicating that the member has not subscribed to any categories of email communications. This value can be used when a member has explicitly chosen not to receive any emails or when their subscription preferences have not been set, allowing for a clear indication of their email communication preferences within the system.
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents a subscription to general member meeting emails, indicating that the member has opted to receive communications related to general member meetings. This value can be used to ensure that members who are interested in staying informed about general member meetings receive relevant updates and notifications, while those who are not interested can choose not to subscribe to this category of emails.
    /// </summary>
    GeneralMemberMeetings = 1 << 0,          // 1

    /// <summary>
    /// Represents a subscription to company-related emails, indicating that the member has opted to receive communications related to companies, such as job opportunities, company events, or other relevant information. This value can be used to ensure that members who are interested in staying informed about company-related news and opportunities receive relevant updates and notifications, while those who are not interested can choose not to subscribe to this category of emails.
    /// </summary>
    CompanyMails = 1 << 1,         // 2

    /// <summary>
    /// Represents a subscription to Monday morning emails, indicating that the member has opted to receive weekly communications sent on Monday mornings, which may include updates, announcements, or other relevant information. This value can be used to ensure that members who are interested in receiving regular updates at the start of the week receive these communications, while those who are not interested can choose not to subscribe to this category of emails.
    /// </summary>
    MondayMorningMails = 1 << 2,  // 4

    /// <summary>
    /// Represents a subscription to emails related to lectures and workshops, indicating that the member has opted to receive communications about upcoming lectures, workshops, or similar events. This value can be used to ensure that members who are interested in attending or staying informed about educational events receive relevant updates and notifications, while those who are not interested can choose not to subscribe to this category of emails.
     ///
    /// </summary>
    LecturesAndWorkshops = 1 << 3,             // 8

    /// <summary>
    /// Represents a subscription to teacher-related emails, indicating that the member has opted to receive communications related to teachers, which may include updates about teacher activities, announcements, or other relevant information. This value can be used to ensure that members who are interested in staying informed about teacher-related news and updates receive relevant communications, while those who are not interested can choose not to subscribe to this category of emails.
    /// </summary>
    TeacherMails = 1 << 4,             // 16

    /// <summary>
    /// Represents a subscription to all categories of emails, indicating that the member has opted to receive communications for all available categories, including general member meetings, company-related emails, Monday morning emails, lectures and workshops, and teacher-related emails. This value can be used when a member wants to receive all types of email communications without having to select individual categories, allowing for maximum engagement and information sharing across all relevant topics within the system.
    /// </summary>
    All = GeneralMemberMeetings | CompanyMails | MondayMorningMails | LecturesAndWorkshops | TeacherMails
}