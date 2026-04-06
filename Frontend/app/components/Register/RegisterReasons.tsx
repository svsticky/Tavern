import { t } from "i18next";
import { Book, BriefcaseBusiness, HeartHandshake, PartyPopper, UsersRound } from "lucide-react";
import RegisterReason from "./RegisterReason";

export default function RegisterReasons( { className }: { className?: string }) {
    const reasons = [
        {
            title: t("book_discounts"),
            description: t("book_discounts_description"),
            icon: Book
        },
        {
            title: t("cheap_activities"),
            description: t("cheap_activities_description"),
            icon: PartyPopper
        },
        {
            title: t("networking"),
            description: t("networking_description"),
            icon: HeartHandshake
        },
        {
            title: t("introduction_week"),
            description: t("introduction_week_description"),
            icon: HeartHandshake
        },
        {
            title: t("labor_market_orientation"),
            description: t("labor_market_orientation_description"),
            icon: BriefcaseBusiness
        },
        {
            title: t("members"),
            description: t("members_description"),
            icon: UsersRound
        }

    ];

    return (
        <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 w-full max-w-7xl mx-auto ${className}`}>
            {reasons.map((reason, index) => (
                <RegisterReason
                    key={index}
                    title={reason.title}
                    description={reason.description}
                    icon={reason.icon}
                />
            ))}
        </div>
    );
}