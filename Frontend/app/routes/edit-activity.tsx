import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useLocation, useNavigate, useParams } from "react-router";
import { 
  getApiActivitiesById,
  type ActivityResponseDto,
} from "~/api";
import EditActivityForm from "~/components/EditActivityForm";
import { PageHeader } from "~/components/UI/PageHeader";
import { isInGroupWithId } from "~/util/group.util";
import EditParticipantsTile from "../components/Tiles/edit-participants-tile";
import SendActivityMailComponent from "~/components/SendActivityMailComponent";

export default function ActivityFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const { pathname } = useLocation();

  const { keycloak } = useKeycloak();
  const isBoard = isInGroupWithId(keycloak.tokenParsed, 1);

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
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <EditActivityForm activity={activity} id={id} isBoard={isBoard} /> 

        {isBoard && isEdit && activity && 
          <div className="flex flex-col gap-4">
            <SendActivityMailComponent activityId={activity.id} />
            <EditParticipantsTile activity={activity} />
          </div>
        }
      </div>
    </div>
  );
}