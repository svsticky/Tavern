import { useKeycloak } from "@react-keycloak/web";
import { getApiActivitiesById, type ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";
import { useEffect, useState } from "react";
import type { Route } from "./+types/activity";
import Button from "~/components/UI/Button";
import { useNavigate } from "react-router";
import { t } from "i18next";
import { PencilIcon } from "lucide-react";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { PageHeader } from "~/components/UI/PageHeader";
import toast from "react-hot-toast";

export default function ActivityPage({ params }: Route.LoaderArgs) {
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();
  const { pathname } = window.location;

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
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [initialized, keycloak.authenticated, params.id]);

  if(loading) return t("loading");
  
  if(activity == null) return t("failed_fetching");
  
  const canEdit = isBoardOrCandidateBoard(keycloak.tokenParsed) || (!activity.showInKoala && !activity.showOnWebsite && activity.organizerId && new Date(activity.dateTimeStart) > new Date(Date.now()));

  return (
    <div className="flex flex-col w-full">
      <PageHeader 
        title={activity.name} 
        backTo={`${pathname.startsWith("/admin") ? '/admin' : ''}/activities`}
        action={canEdit && activity && (
          <Button 
            onClick={() => navigate(`${pathname.startsWith("/admin") ? '/admin' : ''}/activities/edit/${activity.id}`)}
            variant="secondary"
            className="flex items-center px-2"
          >
            <PencilIcon size={18} />
          </Button>
        )}
      />

      <div className="space-y-6 w-full">
        <ActivityDetailsTile activity={activity} setActivity={setActivity} />
        
        <ActivityParticipantsTile 
          enrollments={!activity.areParticipantsVisible ? [] : activity.enrollments.filter(e => !e.isOnWaitingList) ?? []} 
        />
        <ActivityParticipantsTile 
          title={t("waiting_list")} 
          enrollments={!activity.areParticipantsVisible ? [] : activity.enrollments.filter(e => e.isOnWaitingList) ?? []} 
        />
      </div>
    </div>
  );
}