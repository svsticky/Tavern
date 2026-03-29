import { Calendar, ImageIcon, MapPin, UsersRound } from "lucide-react";
import { useTranslation } from "react-i18next";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";
import { getApiActivitiesByIdPoster, type Activity, type ActivityResponseDto } from "~/api";
import { useEffect, useState } from "react";
import { useKeycloak } from "@react-keycloak/web";
import { Link } from "react-router";

type ActivityTileProps = {
  activity: ActivityResponseDto;
  className?: string;
};

export default function ActivityTile({
  activity,
  className,
}: ActivityTileProps) {
  const { t } = useTranslation();
  const { keycloak, initialized } = useKeycloak();

  const [status, setStatus] = useState<"loading" | "loaded" | "error">("loading");

  const posterUrl = `${import.meta.env.ApiUrl}/api/activities/${activity.id}/poster`;
  const isPdf = activity.posterFileName?.toLowerCase().endsWith(".pdf");
  const hasPoster = !!activity.posterFileName;

  return (
    <Link to={`/activities/${activity.id}`} className="no-underline text-inherit">
      <Tile
        className={cn(
          "group block w-60 cursor-pointer overflow-hidden rounded-2xl p-0",
          className,
        )}
      >
        {/* Poster image */}
        <div className="relative w-full aspect-[1/1.414] overflow-hidden bg-gray-100">
  
  {/* Loading */}
  {status === "loading" && hasPoster && (
    <div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)" />
      <p className="text-gray-500 animate-pulse">{t("loading")}</p>
    </div>
  )}

  {/* No poster */}
  {!hasPoster && (
    <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
      <ImageIcon className="text-slate-400 mb-2" size={48} />
      <span className="text-slate-400 text-sm font-medium">
        {t("no_poster")}
      </span>
    </div>
  )}

  {/* Error fallback */}
  {status === "error" && hasPoster && (
    <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
      <ImageIcon className="text-slate-400 mb-2" size={48} />
      <span className="text-slate-400 text-sm font-medium">
        {t("no_poster")}
      </span>
    </div>
  )}

  {/* Content */}
  {hasPoster && (
    isPdf ? (
      <iframe
        src={`${posterUrl}#toolbar=0&navpanes=0&scrollbar=0`}
        onLoad={() => setStatus("loaded")}
        className="w-full h-full border-none group-hover:scale-105 transition-transform duration-500"
        title="Poster PDF"
      />
    ) : (
      <img
        src={posterUrl}
        alt={activity.name ?? ""}
        loading="lazy"
        onLoad={() => setStatus("loaded")}
        onError={() => setStatus("error")}
        className={cn(
          "w-full h-full object-cover transition-all duration-500 group-hover:scale-105",
          status === "loading" ? "opacity-0" : "opacity-100"
        )}
      />
    )
  )}
</div>

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
              {new Date(activity.dateTimeStart ?? Date.now()).getDate()}{" "}
              {formatDate(new Date(activity.dateTimeStart ?? Date.now()), "monthShort")} •{" "}
              {formatDate(new Date(activity.dateTimeStart ?? Date.now()), "timeOnly")}
            </div>

            {/* Location */}
            <div className="mt-1 flex items-center gap-1.5">
              <MapPin size={12} />
              {activity.location}
            </div>

            {/* Available spots */}
            <div className="mt-1 flex items-center gap-1.5">
              <UsersRound size={12} />
              {activity.participantLimit ? (activity.participantLimit - (activity.enrollments?.length ?? 0)) + " " + t("places_available") : (activity.enrollments?.filter((e) => !e.isOnWaitingList).length ?? 0) + " " + t("participants")}
            </div>
          </div>
        </div>
      </Tile>
    </Link>
  );
}
