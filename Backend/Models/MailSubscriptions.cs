namespace Backend.Models;

/// <summary>
/// Represents a mail subscription type that members can subscribe to for receiving different categories of email communications. The MailSubscriptions enum defines various subscription options, such as GeneralMemberMeetings, CompanyMails, MondayMorningMails, LecturesAndWorkshops, and TeacherMails. Each subscription type is represented as a flag, allowing members to subscribe to multiple categories of emails by combining the flags using bitwise operations. This entity is used to manage and track the email preferences of members, enabling personalized and relevant email communications based on their selected subscriptions.
/// </summary>
[Flags]
public enum MailSubscriptions : uint
{
    None = 0,
    GeneralMemberMeetings = 1 << 0,          // 1
    CompanyMails = 1 << 1,         // 2
    MondayMorningMails = 1 << 2,  // 4
    LecturesAndWorkshops = 1 << 3,             // 8
    TeacherMails = 1 << 4,             // 16
    All = GeneralMemberMeetings | CompanyMails | MondayMorningMails | LecturesAndWorkshops | TeacherMails
}