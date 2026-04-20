import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useLocation, useParams } from "react-router";
import { 
  getApiActivitiesById,
  type ActivityResponseDto,
} from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm";
import { PageHeader } from "~/components/UI/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import EditParticipantsTile from "../components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent";
import { cn } from "~/util/tailwind.util";

export default function ActivityFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const { pathname } = useLocation();

  const { keycloak } = useKeycloak();
  const isBoard = isBoardOrCandidateBoard(keycloak.tokenParsed);

  const [loading, setLoading] = useState<boolean>(true);

  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        if (isEdit) {
          const activityRes = await getApiActivitiesById({ path: { id: Number(id) } });
          if (activityRes.data) {
            setActivity(activityRes.data);
          }
        }
      } catch (error) {
        console.error("Error loading data:", error);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }
    
    loadData();
  }, [id, isEdit]);

  if (loading) return t("loading");

  if (isEdit && !activity) return t("failed_fetching");

  return (
    <div className="">
      <PageHeader 
        title={isEdit ? t("edit_activity") : t("create_activity")} 
        backTo={`${pathname.startsWith("/admin") ? '/admin' : ''}${isEdit ? `/activities/${id}` : "/activities"}`} 
      />
      
      <div className={cn("grid grid-cols-1 gap-8", isBoard && isEdit&& "lg:grid-cols-3")}>
        <EditActivityForm activity={activity} id={id} isBoard={isBoard} /> 

        {isBoard && isEdit && activity && 
          <div className="flex flex-col gap-4">
            <SendActivityMailComponent activityId={activity.id} />
            <EditParticipantsTile activity={activity} setActivity={setActivity} />
          </div>
        }
      </div>
    </div>
  );
}