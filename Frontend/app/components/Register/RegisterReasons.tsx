import {
  Book,
  BriefcaseBusiness,
  HeartHandshake,
  PartyPopper,
  UsersRound,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import type { RegisterReasonResponseDto } from "~/api";
import { getEnv } from "~/util/config.utils";
import RegisterReason from "./RegisterReason";

/**
 * A grid-based layout component that displays a collection of reasons to register.
 *
 * It maps through a predefined list of benefits—such as discounts, networking,
 * and career orientation—and renders each using the `RegisterReason` subcomponent.
 * The grid is fully responsive, adjusting columns based on screen size (1 col for mobile,
 * 2 for tablet, 3 for desktop).
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {RegisterReasonResponseDto[]} [props.reasons] - The dynamically loaded reasons from the backend.
 * @param {string} [props.className] - Optional CSS classes to apply to the grid container.
 */
export default function RegisterReasons({
  reasons: dynamicReasons,
  loading = false,
  className,
}: {
  reasons?: RegisterReasonResponseDto[];
  loading?: boolean;
  className?: string;
}) {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");

  const defaultIcons = [
    Book,
    PartyPopper,
    HeartHandshake,
    HeartHandshake,
    BriefcaseBusiness,
    UsersRound,
  ];

  const fallbackReasons = [
    {
      title: t("book_discounts"),
      description: t("book_discounts_description"),
      icon: Book,
      iconUrl: null,
    },
    {
      title: t("cheap_activities"),
      description: t("cheap_activities_description"),
      icon: PartyPopper,
      iconUrl: null,
    },
    {
      title: t("networking"),
      description: t("networking_description"),
      icon: HeartHandshake,
      iconUrl: null,
    },
    {
      title: t("introduction_week"),
      description: t("introduction_week_description"),
      icon: HeartHandshake,
      iconUrl: null,
    },
    {
      title: t("labor_market_orientation"),
      description: t("labor_market_orientation_description"),
      icon: BriefcaseBusiness,
      iconUrl: null,
    },
    {
      title: t("members"),
      description: t("members_description"),
      icon: UsersRound,
      iconUrl: null,
    },
  ];

  if (loading) {
    return (
      <div
        className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 w-full max-w-7xl mx-auto ${className}`}
      >
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            key={index}
            className="border border-gray-100 p-6 rounded-2xl flex flex-col items-start gap-4 bg-white/50 w-full animate-pulse"
          >
            <div className="w-10 h-10 bg-slate-200 rounded-2xl" />
            <div className="space-y-2 w-full">
              <div className="h-5 bg-slate-200 rounded-md w-1/3" />
              <div className="h-4 bg-slate-200 rounded-md w-5/6" />
              <div className="h-4 bg-slate-200 rounded-md w-2/3" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  const reasons =
    dynamicReasons && dynamicReasons.length > 0
      ? dynamicReasons.map((r, idx) => ({
          title: isDutch ? r.titleDutch : r.titleEnglish,
          description: isDutch ? r.descriptionDutch : r.descriptionEnglish,
          iconUrl: r.iconPath
            ? `${getEnv("ApiUrl")}/registerreasons/${r.id}/icon`
            : null,
          icon: defaultIcons[idx] || UsersRound,
        }))
      : fallbackReasons;

  return (
    <div
      className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 w-full max-w-7xl mx-auto ${className}`}
    >
      {reasons.map((reason, index) => (
        <RegisterReason
          key={index}
          title={reason.title}
          description={reason.description}
          icon={reason.icon}
          iconUrl={reason.iconUrl}
        />
      ))}
    </div>
  );
}
