namespace Backend.Models;

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