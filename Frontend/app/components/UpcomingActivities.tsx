import { useEffect, useRef, useState } from "react";
import { cn } from "~/util/tailwind.util";
import ActivityTile from "./Tiles/ActivityTile";
import type { Activity } from "~/api";
import Tile from "./Tiles/Tile";
import { NoContentTile } from "./Tiles/NoContentTile";

interface UpcomingActivitiesProps {
  activities: Activity[];
}

const TILE_WIDTH = 260; // Width of a single activity tile in pixels
const VERTICAL_COUNT = 3; // Number of activities to show when stacked vertically

export default function UpcomingActivities({
  activities,
}: UpcomingActivitiesProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [visibleActivities, setVisibleActivities] = useState<Activity[]>([]);
  const [stackVertically, setStackVertically] = useState(false);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const updateVisible = () => {
      if (!container) return;

      const containerWidth = container.getBoundingClientRect().width;
      const tilesBesideEachOther = Math.floor(containerWidth / TILE_WIDTH);

      const shouldStack = tilesBesideEachOther <= 1;
      setStackVertically(shouldStack);

      const count = shouldStack ? VERTICAL_COUNT : tilesBesideEachOther;
      setVisibleActivities(activities.slice(0, count));
    };

    // Use ResizeObserver for automatic updates
    const observer = new ResizeObserver(updateVisible);
    observer.observe(container);

    // Initial call
    updateVisible();

    return () => {
      observer.disconnect();
    };
  }, [activities]);

  if (activities.length === 0) {
    return (
      <NoContentTile text="Er zijn momenteel geen aankomende activiteiten." />
    );
  }

  return (
    <div
      ref={containerRef}
      className={cn(
        "flex p-2 gap-5",
        stackVertically ? "flex-col" : "flex-row",
      )}
      style={{ maxWidth: TILE_WIDTH * activities.length }}
    >
      {visibleActivities.map((activity) => (
        <ActivityTile
          key={activity.id}
          className="w-full"
          activity={activity}
        />
      ))}
    </div>
  );
}
