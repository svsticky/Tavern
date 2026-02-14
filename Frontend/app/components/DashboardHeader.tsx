import {
  Calendar,
  CircleCheckBig,
  Clock,
  TrendingUp,
  UsersRound,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import type { Activity } from "~/types/Activity";
import { formatDate } from "~/util/date.util";
import Tile from "./Tiles/Tile";
import Button from "./UI/Button";

type DashboardHeaderProps = {
  name: string;
  nextActivity?: Activity;
};

export default function DashboardHeader({
  name,
  nextActivity,
}: DashboardHeaderProps) {
  const { t } = useTranslation();

  return (
    <Tile className="w-full m-0 bg-[linear-gradient(color-mix(in_srgb,var(--board-primary),white_20%),var(--board-primary))] text-white">
      <div className="flex lg:flex-row flex-col gap-5">
        <div className="flex flex-col gap-5 grow basis-0">
          {/* Greeting */}
          <p className="text-2xl font-semibold">Hey {name}!</p>

          {/* Stats */}
          <div className="flex gap-5">
            {/* Activity Enrollments */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("enrollments")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">3</p>
                <CircleCheckBig />
              </div>
            </Tile>

            {/* Attended Activities */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("attended")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">12</p>
                <TrendingUp />
              </div>
            </Tile>
          </div>

          {/* Outstanding Payments */}
          <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
            <div className="flex justify-between items-center">
              <div>
                <p>{t("outstanding_payments")}</p>
                <p>€45,00</p>
              </div>
              <Button>{t("pay")}</Button>
            </div>
          </Tile>
        </div>

        {/* Next Activity Details */}
        {nextActivity && (
          <Tile className="flex flex-col gap-4 bg-(--board-primary-light) border-2 border-white/20 grow basis-0">
            <div className="flex items-center gap-2">
              <Clock /> {t("upcoming_activity")}
            </div>
            <p className="truncate">{nextActivity.title}</p>
            <div className="flex items-center gap-2">
              <Calendar /> {formatDate(nextActivity.startdate, "fullDateTime")}
            </div>
            <div className="flex items-center gap-2">
              <UsersRound />{" "}
              {nextActivity.maxParticipants
                ? nextActivity.maxParticipants -
                  nextActivity.numberOfParticipants
                : 0}{" "}
              {t("of_the")} {nextActivity.maxParticipants} {t("available")}
            </div>
            <Button showArrow={true}>{t("view_details")}</Button>
          </Tile>
        )}
      </div>
    </Tile>
  );
}
