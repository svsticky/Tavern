import { useEffect, useRef, useState } from "react";
import ActivityTile from "./ActivityTile";
import type { ActivityResponseDto } from "~/api";
import { t } from "i18next";
import { NoContentTile } from "../Tiles/NoContentTile";

interface UpcomingActivitiesProps {
  activities: ActivityResponseDto[];
}

const TILE_MIN_WIDTH = 250; 
const VERTICAL_STACK_COUNT = 3;

export default function UpcomingActivities({ activities }: UpcomingActivitiesProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [numVisible, setNumVisible] = useState(3);
  const [isStacked, setIsStacked] = useState(false);

  useEffect(() => {
    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const width = entry.contentRect.width;
        
        const fitsSideBySide = Math.floor(width / TILE_MIN_WIDTH);

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
  }, [activities.length]);

  if (activities.length === 0) {
    return <NoContentTile text={t("no_upcoming_activities")} />;
  }

  const displayActivities = activities.slice(0, numVisible);

  return (
    <div ref={containerRef} className="w-full">
      <div 
        className="grid gap-5 transition-all duration-300"
        style={{
          gridTemplateColumns: isStacked 
            ? "1fr" 
            : `repeat(${numVisible}, 1fr)`
        }}
      >
        {displayActivities.map((activity) => (
          <ActivityTile
            key={activity.id}
            activity={activity}
            className="w-full"
          />
        ))}
      </div>
    </div>
  );
}