import { useKeycloak } from "@react-keycloak/web";
import { getApiActivitiesById, type Activity, type ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Tiles/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Tiles/ActivityParticipantsTile";
import { useEffect, useState } from "react";
import type { Route } from "./+types/activity";
import Button from "~/components/UI/Button";
import { useNavigate } from "react-router";
import { t } from "i18next";

export default function ActivityPage({params}: Route.LoaderArgs) {
  const {keycloak, initialized} = useKeycloak();

  const [loading, setLoading] = useState(true);
  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);

  const navigate = useNavigate();

   useEffect(() => {
        async function loadData() {
          if (!initialized || !keycloak.authenticated) return;
    
          try {
            setLoading(true);
            const activitiesResponse = await getApiActivitiesById({
              path: {
                id: Number(params.id)
              }
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
      }, [initialized, keycloak.authenticated]);

  return (
    <>
      <Button
        showArrow
        arrowDirection="left"
        className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
        onClick={() => navigate("/activities")}
      >
        {t("back")}
      </Button>
      {loading ? (
        <><br />{t("loading")}</>
      ) : !activity ? (
        <><br />{t("activity_not_found")}</>
      ) : (
        <>
          <ActivityDetailsTile activity={activity} setActivity={setActivity} />
          <ActivityParticipantsTile members={!activity.areParticipantsVisible ? [] : activity.enrollments?.filter(e => !e.isOnWaitingList).map(e => e.member) ?? []} />
          <ActivityParticipantsTile title={t("waiting_list")} members={!activity.areParticipantsVisible ? [] : activity.enrollments?.filter(e => e.isOnWaitingList).map(e => e.member) ?? []} />
        </>
      )}
    </>
  );
}