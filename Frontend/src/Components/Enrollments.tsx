import { CircleCheckBig } from "lucide-vue-next";
import { defineComponent } from "vue";
import ListTile from "@/Components/UI/Tile/ListTile";
import { formatDate } from "@/lib/utils";
import type { Activity } from "@/Types/Activity";

export default defineComponent({
  name: "Enrollments",
  props: {
    enrolledActivities: { type: Array as () => Activity[], required: true },
  },

  setup(props) {
    return () => (
      <ListTile class="w-full">
        {props.enrolledActivities.map((activity) => (
          <div key={activity.id} class="flex p-2 gap-2">
            {/* Icon Container */}
            <div class="bg-(--theme-200) rounded-xl w-10 h-10">
              <CircleCheckBig class="text-(--theme) h-full m-auto" />
            </div>

            {/* Activity Details */}
            <div>
              <p>{activity.title}</p>
              <p class="text-gray-500">
                {formatDate(activity.startdate, "shortDate")}
              </p>
            </div>
          </div>
        ))}
      </ListTile>
    );
  },
});
