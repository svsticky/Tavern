import { Calendar, MapPin, UsersRound } from "lucide-vue-next";
import { defineComponent } from "vue";
import { cn } from "@/lib/utils";
import type { Activity } from "@/Types/Activity";
import Tile from "./Tile";

export default defineComponent({
  props: {
    activity: {
      type: Object as () => Activity,
      required: true,
    },
    class: {
      type: String,
      required: false,
    },
  },

  setup(props) {
    return () => (
      <Tile
        class={cn(
          "inline-block w-60 rounded-2xl p-0 overflow-hidden cursor-pointer group",
          props.class,
        )}
      >
        {/* Poster image */}
        <img
          src={props.activity.image}
          alt={props.activity.title}
          class="rounded-t-2xl w-full transition-transform duration-300 group-hover:scale-105"
        />

        {/* Activity details */}
        <div class="p-3 border-r border-l border-b rounded-b-2xl border-gray-200">
          {/* Title */}
          <p class="text-[18px] font-bold mt-1.5 mb-1 transition-colors duration-300 group-hover:text-orange-500">
            {props.activity.title}
          </p>

          <div class="flex flex-col text-[14px] text-gray-500 mt-0">
            {/* Date and time */}
            <div class="flex items-center gap-1.5 mt-1">
              <Calendar size={12} />
              {props.activity.startdate.getDate()}{" "}
              {props.activity.startdate.toLocaleDateString("default", {
                month: "short",
              })}{" "}
              •{" "}
              {props.activity.startdate.toLocaleTimeString("default", {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </div>

            {/* Location */}
            <div class="flex items-center gap-1.5 mt-1">
              <MapPin size={12} /> {props.activity.location}
            </div>

            {/* Available spots */}
            <div class="flex items-center gap-1.5 mt-1">
              <UsersRound size={12} />{" "}
              {props.activity.maxParticipants -
                props.activity.numberOfParticipants}{" "}
              plaatsen vrij
            </div>
          </div>
        </div>
      </Tile>
    );
  },
});
