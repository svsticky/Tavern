import { useKeycloak } from "@react-keycloak/web";
import { useEffect, useState } from "react";
import { getApiActivities, getApiAnnouncements, type Activity } from "~/api";
import ActivityTile from "~/components/Tiles/ActivityTile";
import { NoContentTile } from "~/components/Tiles/NoContentTile";

export default function ActivitiesPage() {
  const { keycloak, initialized } = useKeycloak();

  const [loading, setLoading] = useState(true);
  const [activities, setActivities] = useState<Activity[]>([]);
  useEffect(() => {
      async function loadData() {
        if (!initialized || !keycloak.authenticated) return;
  
        try {
          setLoading(true);
          const activitiesResponse = await getApiActivities({
            query: {
              includePast: false
            }
          });``
        } catch (error) {
          console.error("Error while loading data:", error);
        } finally {
          setLoading(false);
        }
      }
  
      loadData();
    }, [initialized, keycloak.authenticated]);

  return (
    <div className="flex flex-col gap-5 w-full">
      <p className="text-2xl font-bold">Activiteiten</p>
      {loading ? (
        'Loading...'
      ) : (
      activities.length === 0 ? (
          <NoContentTile text="Er zijn momenteel geen aankomende activiteiten." />
        ) : (
          <div className="grid gap-4 justify-center grid-cols-[repeat(auto-fill,minmax(250px,1fr))] w-full">
            {activities.map((activity) => (
              <ActivityTile
                key={activity.id}
                className="w-auto"
                activity={activity}
              />
            ))}
          </div>
        ))}
    </div>
  );
}
