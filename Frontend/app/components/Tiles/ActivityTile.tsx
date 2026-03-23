import { Calendar, MapPin, UsersRound } from "lucide-react";
import { useTranslation } from "react-i18next";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";
import type { Activity } from "~/api";

type ActivityTileProps = {
  activity: Activity;
  className?: string;
};

export default function ActivityTile({
  activity,
  className,
}: ActivityTileProps) {
  const { t } = useTranslation();

  return (
    <Tile
      className={cn(
        "group inline-block w-60 cursor-pointer overflow-hidden rounded-2xl p-0",
        className,
      )}
    >
      {/* Poster image */}
      <img
        alt={activity.name ?? ""}
        className="w-full rounded-t-2xl transition-transform duration-300 group-hover:scale-105"
      />

      {/* Activity details */}
      <div className="rounded-b-2xl border border-t-0 border-gray-200 p-3">
        {/* Title and price */}
        <div className="mb-1 mt-1.5 flex w-full justify-between text-[18px] font-bold">
          <p className="transition-colors duration-300 group-hover:text-(--board-primary) truncate">
            {activity.name}
          </p>
          <p className="text-nowrap text-(--board-primary)">
            {activity.price ?? 0 > 0 ? `€${activity.price}` : t("free")}
          </p>
        </div>

        <div className="mt-0 flex flex-col text-[14px] text-gray-500">
          {/* Date and time */}
          <div className="mt-1 flex items-center gap-1.5">
            <Calendar size={12} />
            {new Date(activity.dateTimeStart ?? new Date()).getDate()}{" "}
            {formatDate(new Date(activity.dateTimeStart ?? new Date()), "monthShort")} •{" "}
            {formatDate(new Date(activity.dateTimeStart ?? new Date()), "timeOnly")}
          </div>

          {/* Location */}
          <div className="mt-1 flex items-center gap-1.5">
            <MapPin size={12} />
            {activity.location}
          </div>

          {/* Available spots */}
          <div className="mt-1 flex items-center gap-1.5">
            <UsersRound size={12} />
            {activity.participantLimit ?? 0 - (activity.enrollments?.length ?? 0)}{" "}
            {t("places_available")}
          </div>
        </div>
      </div>
    </Tile>
  );
}
