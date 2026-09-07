import { SlidersHorizontal } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import type {
  ActivityResponseDto,
  GetAnnouncementResponseDto,
  GroupMembershipResponseDto,
} from "~/api";
import ActivityEnrollmentOverview from "~/components/Activity/ActivityEnrollmentOverview";
import UpcomingActivities from "~/components/Activity/UpcomingActivities";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import PersonaliseDashboardModal from "~/components/Dashboard/PersonaliseDashboardModal";
import DashboardHeader from "~/components/DashboardHeader";
import GroupMembershipOverview from "~/components/Group/GroupMembershipOverview";
import Button from "~/components/UI/Button";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import {
  type DashboardWidgetConfig,
  type DashboardWidgetId,
  DEFAULT_DASHBOARD_WIDGETS,
  loadDashboardWidgetConfig,
  resetDashboardWidgetConfig,
  saveDashboardWidgetConfig,
} from "~/util/dashboardWidgets";
import { loadHomePageData } from "./home.handlers";

/**
 * The main application landing page for authenticated members.
 *
 * This component acts as a high-level summary of the user's association life. It aggregates:
 * - **Personalized Greeting**: Welcomes the user and highlights their next upcoming activity.
 * - **Latest Announcements**: Shows a preview of the most recent association-wide updates.
 * - **Activity Feed**: Displays a comprehensive list of upcoming events.
 * - **Personal Overview**: A sidebar containing the user's current activity enrollments and
 *   their committee/group memberships.
 *
 * Features:
 * - **Contextual Loading**: Displays a skeleton-style loading state while coordinating multiple API requests.
 * - **Auth Integration**: Deeply integrates with auth service to filter data based on the user's `UserId`.
 * - **Personalization**: Allows users to configure which widgets appear on their dashboard and reorder them.
 * - **Responsive Layout**: Uses a dynamic grid system adapting based on visible main and sidebar widgets.
 *
 * @page
 * @component
 */
export default function DashboardPage() {
  const { t } = useTranslation();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [authenticated, setAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    const loadTokenAndAuth = async () => {
      if (!authService.isReady()) return;

      const tokenParsed = await authService.getTokenParsed();
      const authenticated = await authService.isAuthenticated();
      setTokenParsed(tokenParsed);
      setAuthenticated(authenticated);
    };
    loadTokenAndAuth();
  }, [authService]);

  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  const [enrolledActivities, setEnrolledActivities] = useState<
    ActivityResponseDto[]
  >([]);
  const [announcements, setAnnouncements] = useState<
    GetAnnouncementResponseDto[]
  >([]);
  const [groupMemberships, setGroupMemberships] = useState<
    GroupMembershipResponseDto[]
  >([]);

  const [widgets, setWidgets] = useState<DashboardWidgetConfig[]>(
    DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w })),
  );
  const [isPersonaliseModalOpen, setIsPersonaliseModalOpen] = useState(false);

  useEffect(() => {
    if (tokenParsed?.UserId) {
      setWidgets(loadDashboardWidgetConfig(tokenParsed.UserId));
    }
  }, [tokenParsed?.UserId]);

  const handleSaveWidgets = (updated: DashboardWidgetConfig[]) => {
    setWidgets(updated);
    saveDashboardWidgetConfig(updated, tokenParsed?.UserId);
    toast.success(t("widgets_saved"));
  };

  const handleResetWidgets = () => {
    const reset = resetDashboardWidgetConfig(tokenParsed?.UserId);
    setWidgets(reset);
    toast.success(t("widgets_saved"));
  };

  const [loading, setLoading] = useState(true);
  useEffect(() => {
    if (authenticated === null || tokenParsed === null) {
      return;
    }

    if (!authenticated) return;

    loadHomePageData({
      authenticated: authenticated,
      userId: tokenParsed.UserId,
      setLoading,
      setActivities,
      setAnnouncements,
      setGroupMemberships,
      setEnrolledActivities,
    });
  }, [authenticated, tokenParsed]);

  if (!tokenParsed) {
    return null;
  }

  const visibleMainWidgets = widgets
    .filter((w) => w.column === "main" && w.visible)
    .sort((a, b) => a.order - b.order);

  const visibleSidebarWidgets = widgets
    .filter((w) => w.column === "sidebar" && w.visible)
    .sort((a, b) => a.order - b.order);

  const hasAnyWidgets =
    visibleMainWidgets.length > 0 || visibleSidebarWidgets.length > 0;

  const renderWidget = (widgetId: DashboardWidgetId, inSidebar: boolean) => {
    switch (widgetId) {
      case "announcements":
        return (
          <div key="announcements" className="flex flex-col w-full gap-y-3">
            <div className="flex w-full justify-between items-center">
              <p className="font-semibold text-lg">
                {t("latest_announcements")}
              </p>
              <Button
                showArrow
                className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                href="/announcements"
              >
                {t("show_all")}
              </Button>
            </div>
            <AnnouncementsList
              announcements={announcements.slice(0, inSidebar ? 1 : 2)}
            />
          </div>
        );
      case "upcoming_activities":
        return (
          <div
            key="upcoming_activities"
            className="flex flex-col w-full gap-y-3"
          >
            <div className="flex w-full justify-between items-center">
              <p className="font-semibold text-lg">
                {t("upcoming_activities")}
              </p>
              <Button
                showArrow
                className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                href="/activities"
              >
                {t("show_all")}
              </Button>
            </div>
            <UpcomingActivities activities={activities} />
          </div>
        );
      case "my_enrollments":
        return (
          <div key="my_enrollments" className="flex flex-col w-full gap-3">
            <p className="text-md font-medium">{t("my_enrollments")}</p>
            <ActivityEnrollmentOverview
              enrolledActivities={enrolledActivities}
            />
          </div>
        );
      case "my_groups":
        return (
          <div key="my_groups" className="flex flex-col w-full gap-3">
            <p className="text-md font-medium">{t("my_groups")}</p>
            <GroupMembershipOverview groupMemberships={groupMemberships} />
          </div>
        );
      default:
        return null;
    }
  };

  return (
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto w-full">
      {/* Dashboard Header */}
      <DashboardHeader
        name={tokenParsed.given_name}
        nextActivity={activities[0]}
      />

      {loading ? (
        <div className="flex flex-col items-center justify-center min-h-[400px] w-full gap-4">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)"></div>
          <p className="text-gray-500 animate-pulse">
            {t("loading_dashboard")}...
          </p>
        </div>
      ) : (
        <div className="flex flex-col w-full gap-4">
          <div className="flex justify-end w-full">
            <Button
              variant="secondary"
              onClick={() => setIsPersonaliseModalOpen(true)}
              className="flex items-center gap-2 text-xs"
            >
              <SlidersHorizontal size={14} />
              {t("personalise_dashboard")}
            </Button>
          </div>

          {!hasAnyWidgets ? (
            <div className="flex flex-col items-center justify-center p-12 text-center bg-slate-50 border border-dashed border-slate-200 rounded-2xl w-full gap-3">
              <SlidersHorizontal size={32} className="text-slate-400" />
              <p className="text-slate-600 font-medium">
                {t("no_widgets_enabled")}
              </p>
              <Button
                variant="secondary"
                onClick={() => setIsPersonaliseModalOpen(true)}
              >
                {t("personalise_dashboard")}
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-4 w-full gap-5 animate-in fade-in duration-500">
              {visibleMainWidgets.length > 0 && (
                <div
                  className={`flex flex-col w-full gap-y-8 col-span-4 ${
                    visibleSidebarWidgets.length > 0
                      ? "lg:col-span-3"
                      : "col-span-4"
                  }`}
                >
                  {visibleMainWidgets.map((w) => renderWidget(w.id, false))}
                </div>
              )}

              {visibleSidebarWidgets.length > 0 && (
                <div
                  className={`flex flex-col col-span-4 gap-4 ${
                    visibleMainWidgets.length > 0
                      ? "lg:col-span-1"
                      : "col-span-4"
                  }`}
                >
                  {visibleSidebarWidgets.map((w) => renderWidget(w.id, true))}
                </div>
              )}
            </div>
          )}
        </div>
      )}

      <PersonaliseDashboardModal
        isOpen={isPersonaliseModalOpen}
        onClose={() => setIsPersonaliseModalOpen(false)}
        widgets={widgets}
        onSave={handleSaveWidgets}
        onReset={handleResetWidgets}
      />
    </div>
  );
}
