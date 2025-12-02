import { Calendar, Megaphone } from "lucide-vue-next";
import { defineComponent } from "vue";
import { cn } from "@/lib/utils";
import type { Announcement } from "@/Types/Announcement";
import Tile from "./Tile";

export default defineComponent({
  props: {
    announcement: {
      type: Object as () => Announcement,
      required: true,
    },
    class: {
      type: String,
      required: false,
    },
  },

  setup(props) {
    return () => (
      <Tile class={cn("rounded-2xl border border-gray-200", props.class)}>
        {/* Title and date */}
        <div class="flex w-full justify-between">
          <p class="mb-2">{props.announcement.title}</p>
          <p class="flex text-gray-600 gap-1 text-sm text-nowrap">
            <Calendar class="h-5" />{" "}
            {props.announcement.date.toLocaleDateString()}
          </p>
        </div>

        {/* Announcement content */}
        <p class="text-gray-600">{props.announcement.announcement}</p>

        {/* Divider */}
        <div class="h-[0.5px] w-full my-2 bg-gray-200"></div>

        {/* Announcer */}
        <p class="text-gray-600 flex gap-2 items-center">
          <Megaphone class="h-5" /> {props.announcement.announcer}
        </p>
      </Tile>
    );
  },
});
