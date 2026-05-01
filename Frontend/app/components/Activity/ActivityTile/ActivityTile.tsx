import { useKeycloak } from "@react-keycloak/web";
import {
  Calendar,
  ImageIcon,
  MapPin,
  PencilIcon,
  UsersRound,
} from "lucide-react"; // PencilIcon toegevoegd
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useNavigate } from "react-router"; // useNavigate toegevoegd
import type { ActivityResponseDto } from "~/api";
import { formatDate } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import Tile from "../../Tiles/Tile";
import { handleEditClick } from "./ActivityTile.handlers";

/**
 * A preview card component for an Activity, typically used in grids or lists.
 * It provides a visual summary including the poster, pricing, and key metadata.
 *
 * Key Features:
 * - **Smart Edit Permissions**: Displays a floating edit button if the user is a board member
 *   or the organizer of an un-published, future activity.
 * - **Image Lifecycle**: Manages loading, success, and error states for posters with smooth transitions.
 * - **Availability Logic**: Dynamically displays remaining spots if a participant limit is set,
 *   otherwise shows the current participant count.
 * - **Event Handling**: Uses a nested edit button that prevents parent `Link` navigation.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto} props.activity - The activity data object containing all display information.
 * @param {string} [props.className] - Optional additional CSS classes for custom styling or positioning.
 *
 * @example
 * ```tsx
 * <ActivityTile
 *   activity={activityData}
 *   className="m-4"
 * />
 * ```
 */
export default function ActivityTile({
  activity,
  className,
}: {
  activity: ActivityResponseDto;
  className?: string;
}) {
  const { t } = useTranslation();
  const { keycloak } = useKeycloak();
  const navigate = useNavigate();

  const [status, setStatus] = useState<"loading" | "loaded" | "error">(
    "loading",
  );

  const canEdit =
    isBoardOrCandidateBoard(keycloak.tokenParsed) ||
    (!activity.showInKoala &&
      !activity.showOnWebsite &&
      activity.organizerId &&
      new Date(activity.dateTimeStart) > new Date(Date.now()));
  const posterUrl = `${import.meta.env.ApiUrl}/api/activities/${activity.id}/poster`;
  const hasPoster = !!activity.posterFileName;

  const startDate = new Date(activity.dateTimeStart);
  const endDate = new Date(activity.dateTimeEnd);

  return (
    <Link
      to={`/activities/${activity.id}`}
      className="no-underline text-inherit"
    >
      <Tile
        className={cn(
          "group relative block w-60 cursor-pointer overflow-hidden p-0 transition-all hover:shadow-md",
          className,
        )}
      >
        {/* Floating Edit Button */}
        {canEdit && (
          <button
            onClick={(e) => handleEditClick(e, navigate, activity)}
            className="absolute right-2 top-2 z-20 rounded-full bg-white/90 p-2 text-gray-600 shadow-sm backdrop-blur-sm transition-all hover:scale-110 hover:bg-white hover:text-(--board-primary) hover:cursor-pointer"
          >
            <PencilIcon size={16} />
          </button>
        )}

        {/* Poster image */}
        <div className="relative aspect-[1/1.414] w-full overflow-hidden bg-gray-100">
          {/* Status states (Loading, No poster, Error) - ongewijzigd */}
          {status === "loading" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
              <div className="h-12 w-12 animate-spin rounded-full border-b-2 border-(--board-primary-light)" />
            </div>
          )}

          {!hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
              <ImageIcon className="mb-2 text-slate-400" size={48} />
              <span className="text-sm font-medium text-slate-400">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Poster */}
          {hasPoster && (
            <img
              src={posterUrl}
              alt={activity.name}
              loading="lazy"
              onLoad={() => setStatus("loaded")}
              onError={() => setStatus("error")}
              className={cn(
                "h-full w-full object-cover transition-all duration-500 group-hover:scale-105",
                status === "loading" ? "opacity-0" : "opacity-100",
              )}
            />
          )}
        </div>

        {/* Activity details */}
        <div className="rounded-b-2xl border border-t-0 border-gray-200 p-3 bg-white">
          <div className="mb-1 mt-1.5 flex w-full justify-between text-[18px] font-bold">
            <p className="truncate transition-colors duration-300 group-hover:text-(--board-primary)">
              {activity.name}
            </p>
            <p className="shrink-0 text-nowrap text-(--board-primary)">
              {(activity.price ?? 0) > 0 ? `€${activity.price}` : t("free")}
            </p>
          </div>

          <div className="mt-0 flex flex-col text-[14px] text-gray-500">
            <div>
              <Calendar size={12} />
              {startDate.getDate()} {formatDate(startDate, "monthShort")} •{" "}
              {formatDate(startDate, "timeOnly")}
              {" - "}
              {startDate.toDateString() !== endDate.toDateString() && (
                <>
                  {endDate.getDate()} {formatDate(endDate, "monthShort")} •{" "}
                </>
              )}
              {formatDate(endDate, "timeOnly")}
            </div>

            <div className="mt-1 flex items-center gap-1.5">
              <MapPin size={12} />
              <span className="truncate">{activity.location}</span>
            </div>

            <div className="mt-1 flex items-center gap-1.5">
              <UsersRound size={12} />
              {activity.participantLimit
                ? `${activity.participantLimit - activity.enrollments.length} ${t("places_available")}`
                : `${activity.enrollments.filter((e) => !e.isOnWaitingList).length} ${t("participants")}`}
            </div>
          </div>
        </div>
      </Tile>
    </Link>
  );
}
