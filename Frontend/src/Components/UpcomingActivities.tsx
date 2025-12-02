import {
  defineComponent,
  nextTick,
  onBeforeUnmount,
  onMounted,
  ref,
} from "vue";
import ActivityTile from "@/Components/UI/Tile/ActivityTile";
import { cn } from "@/lib/utils";
import type { Activity } from "@/Types/Activity";

export default defineComponent({
  name: "UpcomingActivities",
  props: {
    activities: { type: Array as () => Activity[], required: true },
  },

  setup(props) {
    const containerRef = ref<HTMLDivElement | null>(null); // The ref for the container div
    const visibleActivities = ref<Activity[]>([]); // The activities currently visible based on container width
    const comingActivitiesBelowEachother = ref(false); // Whether activities are stacked vertically

    // Function to update visible activities based on container width
    const updateVisible = () => {
      if (!containerRef.value) return;
      const containerWidth = containerRef.value.getBoundingClientRect().width;
      const tileWidth = 200;
      const possibleTileBesidesEachother = Math.floor(
        containerWidth / tileWidth,
      );
      comingActivitiesBelowEachother.value = possibleTileBesidesEachother === 1;
      const count = comingActivitiesBelowEachother.value
        ? 3
        : possibleTileBesidesEachother;
      visibleActivities.value = props.activities.slice(0, count);
    };

    // Set up event listeners
    onMounted(() => {
      nextTick(updateVisible);
      window.addEventListener("resize", updateVisible);
    });

    // Clean up event listeners
    onBeforeUnmount(() => {
      window.removeEventListener("resize", updateVisible);
    });

    return () => (
      <div
        ref={containerRef}
        class={cn(
          "flex p-2 gap-5",
          comingActivitiesBelowEachother.value ? "flex-col" : "flex-row",
        )}
      >
        {visibleActivities.value.map((activity) => (
          <ActivityTile key={activity.id} class="w-full" activity={activity} />
        ))}
      </div>
    );
  },
});
