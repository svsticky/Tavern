import {
  Calendar,
  CircleCheckBig,
  Clock,
  TrendingUp,
  UsersRound,
} from "lucide-vue-next";
import { defineComponent } from "vue";
import { useI18n } from "vue-i18n";
import Button from "@/Components/UI/Button";
import Tile from "@/Components/UI/Tile/Tile";
import { formatDate } from "@/lib/dates.utils";
import type { Activity } from "@/Types/Activity";

export default defineComponent({
  name: "DashboardHeader",
  props: {
    name: { type: String, required: true },
    nextActivity: { type: Object as () => Activity, required: false },
  },
  setup(props) {
    const { t } = useI18n();

    return () => (
      <Tile class="w-full m-0 bg-[linear-gradient(var(--theme-475),var(--theme))] text-white">
        <div class="flex lg:flex-row flex-col gap-5">
          <div class="flex flex-col gap-5 grow basis-0">
            {/* Greeting */}
            <p class="text-2xl font-semibold">Hey {props.name}!</p>

            {/* Stats */}
            <div class="flex gap-5">
              {/* Activity Enrollments */}
              <Tile class="bg-(--theme-460) border-2 border-white/20 grow">
                <p>{t("enrollments")}</p>
                <div class="flex items-center gap-2">
                  <p class="text-2xl">3</p> <CircleCheckBig />
                </div>
              </Tile>

              {/* Attended Activities */}
              <Tile class="bg-(--theme-460) border-2 border-white/20 grow">
                <p>{t("attended")}</p>
                <div class="flex items-center gap-2">
                  <p class="text-2xl">12</p>
                  <TrendingUp />
                </div>
              </Tile>
            </div>

            {/* Outstanding Payments */}
            <Tile class="bg-(--theme-460) border-2 border-white/20 grow">
              <div class="flex justify-between items-center">
                <div>
                  <p>{t("outstanding_payments")}</p>
                  <p>€45,00</p>
                </div>
                <Button>{t("pay")}</Button>
              </div>
            </Tile>
          </div>

          {/* Next Activity Details */}
          {props.nextActivity && (
            <Tile class="flex flex-col gap-4 bg-(--theme-460) border-2 border-white/20 grow basis-0">
              <div class="flex items-center gap-2">
                <Clock /> {t("upcomming_activity")}
              </div>
              <p>{props.nextActivity.title}</p>
              <div class="flex items-center gap-2">
                <Calendar />{" "}
                {formatDate(props.nextActivity.startdate, "fullDateTime")}
              </div>
              <div class="flex items-center gap-2">
                <UsersRound />{" "}
                {props.nextActivity.maxParticipants
                  ? props.nextActivity.maxParticipants -
                    props.nextActivity.numberOfParticipants
                  : 0}{" "}
                {t("of_the")} {props.nextActivity.maxParticipants}{" "}
                {t("available")}{" "}
              </div>
              <Button showArrow={true}>{t("view_details")}</Button>
            </Tile>
          )}
        </div>
      </Tile>
    );
  },
});
