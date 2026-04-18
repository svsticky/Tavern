import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import { getApiActivities, type ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";

export default function ActivitiesPage() {
  const { keycloak, initialized } = useKeycloak();

  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  useEffect(() => {
      async function loadData() {
        if (!initialized || !keycloak.authenticated) return;
  
        try {
          setLoading(true);
          const activitiesResponse = await getApiActivities({
            query: {
              IncludePast: false,
              IncludeFuture: true,
            }
          });

          if (activitiesResponse.data) {
            setActivities(activitiesResponse.data as ActivityResponseDto[]);
          }
        } catch (error) {
          console.error("Error while loading data:", error);
          toast.error(t("loading_failed"));
        } finally {
          setLoading(false);
        }
      }
  
      loadData();
    }, [initialized, keycloak.authenticated]);

  const isInGroup = (keycloak.tokenParsed?.group_memberships ?? []).length > 0;

  return (
    <>
      <div className="flex justify-between items-center">
        <PageHeader title={t("activities")} 
          action={isInGroup && (
          <Button 
            variant="secondary"
            onClick={() => (navigate("/activities/create"))}
            className="flex items-center gap-2 px-3 py-1 rounded-lg transition-colors font-medium shadow-sm"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        )} />
      </div>
      {loading ? (
        'Loading...'
      ) : (
      activities.length === 0 ? (
          <NoContentTile text={t("no_upcoming_activities")} />
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
    </>
  );
}
