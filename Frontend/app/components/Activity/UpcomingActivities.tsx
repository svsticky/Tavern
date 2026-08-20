import { t } from "i18next";
import { useEffect, useRef, useState } from "react";
import type { ActivityResponseDto } from "~/api";
import { NoContentTile } from "../Tiles/NoContentTile";
import ActivityTile from "./ActivityTile/ActivityTile";

const TILE_MIN_WIDTH = 250;
const GAP = 20; // px, matches gap-5
const VERTICAL_STACK_COUNT = 3;

/**
 * A responsive container component that displays a dynamic number of upcoming activities.
 *
 * Features:
 * - **Dynamic Grid Layout**: Uses a `ResizeObserver` to calculate how many activity tiles
 *   can fit side-by-side based on a minimum tile width (`250px`).
 * - **Smart Stacking**: Automatically switches to a vertical stack of 3 items when the
 *   container is too narrow for a multi-column grid (e.g., on mobile devices).
 * - **Automatic Slice**: Ensures that only the number of activities that physically fit the
 *   screen are rendered, preventing layout overflow.
 * - **Empty State**: Renders a `NoContentTile` with a localized message if the activity list is empty.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto[]} props.activities - The array of upcoming activity objects to display.
 *
 * @example
 * ```tsx
 * <UpcomingActivities
 *   activities={upcomingActivitiesData}
 * />
 * ```
 */
export default function UpcomingActivities({
  activities,
}: {
  activities: ActivityResponseDto[];
}) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [numVisible, setNumVisible] = useState(3);
  const [isStacked, setIsStacked] = useState(false);

  useEffect(() => {
    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const width = entry.contentRect.width;

        const fitsSideBySide = Math.floor(
          (width + GAP) / (TILE_MIN_WIDTH + GAP),
        );

        if (fitsSideBySide < 2) {
          setIsStacked(true);
          setNumVisible(VERTICAL_STACK_COUNT);
        } else {
          setIsStacked(false);
          setNumVisible(fitsSideBySide);
        }
      }
    });

    if (containerRef.current) observer.observe(containerRef.current);
    return () => observer.disconnect();
  }, []);

  if (activities.length === 0) {
    return <NoContentTile text={t("no_upcoming_activities")} />;
  }

  const displayActivities = activities.slice(0, numVisible);
  const columnCount = Math.max(displayActivities.length, 1);

  return (
    <div ref={containerRef} className="w-full">
      <div
        className="grid gap-5 justify-start transition-all duration-300"
        style={{
          gridTemplateColumns: isStacked
            ? "1fr"
            : `repeat(${columnCount}, minmax(${TILE_MIN_WIDTH}px, 400px))`,
        }}
      >
        {displayActivities.map((activity) => (
          <ActivityTile
            key={activity.id}
            activity={activity}
            className="w-full max-w-[400px]"
          />
        ))}
      </div>
    </div>
  );
}
