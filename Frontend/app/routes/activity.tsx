import { useKeycloak } from "@react-keycloak/web";
import { getApiActivitiesById, type ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Tiles/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Tiles/ActivityParticipantsTile";
import { useEffect, useState } from "react";
import type { Route } from "./+types/activity";
import Button from "~/components/UI/Button";
import { useNavigate } from "react-router";
import { t } from "i18next";
import { PencilIcon } from "lucide-react";
import { isInGroupWithId } from "~/util/group.util";
import { PageHeader } from "~/components/UI/PageHeader";

export default function ActivityPage({ params }: Route.LoaderArgs) {
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);


  useEffect(() => {
    async function loadData() {
      if (!initialized || !keycloak.authenticated) return;

      try {
        setLoading(true);
        const activitiesResponse = await getApiActivitiesById({
          path: { id: Number(params.id) }
        });

        if (activitiesResponse.data) {
          setActivity(activitiesResponse.data);
        }
      } catch (error) {
        console.error("Error while loading data:", error);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [initialized, keycloak.authenticated, params.id]);

  if(loading) return t("loading");
  
  if(activity == null) return t("failed_fetching");
  
  const canEdit = isInGroupWithId(keycloak.tokenParsed, import.meta.env.BOARD_GROUP_ID) || (!activity.showInKoala && !activity.showOnWebsite && activity.organizerId && isInGroupWithId(keycloak.tokenParsed, activity.organizerId) && new Date(activity.dateTimeStart) > new Date(Date.now()));

  return (
    <div className="flex flex-col w-full">
      <PageHeader 
        title={activity.name} 
        backTo="/activities"
        action={canEdit && activity && (
          <Button 
            onClick={() => navigate(`/activities/edit/${activity.id}`)}
            variant="secondary"
            className="flex items-center px-2"
          >
            <PencilIcon size={18} />
          </Button>
        )}
      />

        <div className="space-y-6">
          <ActivityDetailsTile activity={activity} setActivity={setActivity} />
          
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <ActivityParticipantsTile 
              members={!activity.areParticipantsVisible ? [] : activity.enrollments.filter(e => !e.isOnWaitingList).map(e => e.member) ?? []} 
            />
            <ActivityParticipantsTile 
              title={t("waiting_list")} 
              members={!activity.areParticipantsVisible ? [] : activity.enrollments.filter(e => e.isOnWaitingList).map(e => e.member) ?? []} 
            />
          </div>
        </div>
    </div>
  );
}