import type { MailSubscriptions, TargetAudience } from "~/api";

export const mailSubscriptionMap: Record<number, MailSubscriptions> = {
    0: 'None',
    1: 'GeneralMemberMeetings',
    2: 'CompanyMails',
    4: 'MondayMorningMails',
    8: 'LecturesAndWorkshops',
    16: 'TeacherMails'
};