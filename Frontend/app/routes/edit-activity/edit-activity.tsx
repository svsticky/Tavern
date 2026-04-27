import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import { useLocation, useParams } from "react-router";
import { 
  type ActivityResponseDto,
} from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import EditParticipantsTile from "../../components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent";
import { cn } from "~/util/tailwind.util";
import { getEditActivityBackPath, loadEditActivityData } from "./edit-activity.handlers";

export default function ActivityFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const { pathname } = useLocation();

  const { keycloak } = useKeycloak();
  const isBoard = isBoardOrCandidateBoard(keycloak.tokenParsed);

  const [loading, setLoading] = useState<boolean>(true);

  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);

  useEffect(() => {
    loadEditActivityData({
      isEdit,
      id,
      setActivity: (next) => setActivity(next),
      setLoading
    });
  }, [id, isEdit]);

  if (loading) return t("loading");

  if (isEdit && !activity) return t("failed_fetching");

  return (
    <div className="">
      <PageHeader 
        title={isEdit ? t("edit_activity") : t("create_activity")} 
        backTo={getEditActivityBackPath(pathname, isEdit, id)} 
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
