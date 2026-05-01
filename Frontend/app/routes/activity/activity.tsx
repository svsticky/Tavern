import { useKeycloak } from "@react-keycloak/web";
import { type ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";
import { useEffect, useState } from "react";
import type { Route } from "./+types/activity";
import Button from "~/components/UI/Button";
import { useNavigate } from "react-router";
import { t } from "i18next";
import { PencilIcon } from "lucide-react";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { canEditActivity, getActivityBackPath, handleEditActivityClick, loadActivityData } from "./activity.handlers";

/**
 * Detailed view for a specific activity, including description and participant lists.
 * 
 * This page serves as the single source of truth for an activity's information. 
 * It manages:
 * - **Data Hydration**: Fetches activity details based on the URL `id` parameter.
 * - **Enrollment Management**: Passes state-updating functions to child tiles 
 *   to allow immediate UI feedback after joining/leaving.
 * - **Participant Visibility**: Filters and displays the participant list and 
 *   waiting list, respecting the `areParticipantsVisible` privacy flag.
 * - **Contextual Navigation**: Determines the "Back" path based on whether 
 *   the user arrived via an admin route or the standard member list.
 * - **Permissions**: Shows an edit action only for authorized users (Board or Organizers).
 * 
 * @page
 * @component
 * @param {Route.LoaderArgs} props - Route parameters provided by the framework, including the activity ID.
 */
export default function ActivityPage({ params }: Route.LoaderArgs) {
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();
  const { pathname } = window.location;

  const [loading, setLoading] = useState(true);
  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);


  useEffect(() => {
    loadActivityData({
      initialized,
      authenticated: keycloak.authenticated,
      activityId: Number(params.id),
      setLoading,
      setActivity: (next) => setActivity(next)
    });
  }, [initialized, keycloak.authenticated, params.id]);

  if(loading) return t("loading");
  
  if(activity == null) return t("failed_fetching");
  
  const canEdit = canEditActivity(activity, keycloak.tokenParsed);

  return (
    <div className="flex flex-col w-full">
      <PageHeader 
        title={activity.name} 
        backTo={getActivityBackPath(pathname)}
        action={canEdit && activity && (
          <Button 
            onClick={() => handleEditActivityClick(navigate, pathname, activity.id)}
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
