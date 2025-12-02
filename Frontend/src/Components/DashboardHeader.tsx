import {
  Calendar,
  CircleCheckBig,
  Clock,
  TrendingUp,
  UsersRound,
} from "lucide-vue-next";
import { defineComponent } from "vue";
import Button from "@/Components/UI/Button";
import Tile from "@/Components/UI/Tile/Tile";
import type { Activity } from "@/Types/Activity";

export default defineComponent({
  name: "DashboardHeader",
  props: {
    name: { type: String, required: true },
    nextActivity: { type: Object as () => Activity, required: false },
  },
  setup(props) {
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
                <p>Aanmeldingen</p>
                <div class="flex items-center gap-2">
                  <p class="text-2xl">3</p> <CircleCheckBig />
                </div>
              </Tile>

              {/* Attended Activities */}
              <Tile class="bg-(--theme-460) border-2 border-white/20 grow">
                <p>Bijgewoond</p>
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
                  <p>Openstaand</p>
                  <p>€ 45,00</p>
                </div>
                <Button>Betalen</Button>
              </div>
            </Tile>
          </div>

          {/* Next Activity Details */}
          {props.nextActivity && (
            <Tile class="flex flex-col gap-4 bg-(--theme-460) border-2 border-white/20 grow basis-0">
              <div class="flex items-center gap-2">
                <Clock /> Eerstvolgende activiteit
              </div>
              <p>{props.nextActivity.title}</p>
              <div class="flex items-center gap-2">
                <Calendar />{" "}
                {props.nextActivity.startdate.toLocaleDateString("default", {
                  day: "numeric",
                  month: "long",
                  year: "numeric",
                  hour: "2-digit",
                  minute: "2-digit",
                  hour12: false,
                })}
              </div>
              <div class="flex items-center gap-2">
                <UsersRound />{" "}
                {props.nextActivity.maxParticipants
                  ? props.nextActivity.maxParticipants -
                    props.nextActivity.numberOfParticipants
                  : 0}{" "}
                van de {props.nextActivity.maxParticipants} vrij{" "}
              </div>
              <Button showArrow={true}>Bekijk details</Button>
            </Tile>
          )}
        </div>
      </Tile>
    );
  },
});
