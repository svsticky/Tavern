import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import type { ActivityListItemDto, PagedResultDtoActivityListItemDto } from "~/api";
import { getActivitiesList } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

export interface AdminActivitiesParams {
  year: number | null;
  search: string;
  sortBy: string;
  sortDir: "asc" | "desc";
  page: number;
  pageSize: number;
}

/**
 * Fetches a paginated, sorted, searchable list of activities for admin use.
 */
export const loadAdminActivities = async (
  params: AdminActivitiesParams,
  setLoading: (loading: boolean) => void,
  setResult: (result: PagedResultDtoActivityListItemDto) => void,
) => {
  try {
    setLoading(true);
    const response = await getActivitiesList({
      query: {
        Page: params.page,
        PageSize: params.pageSize,
        ...(params.search ? { Search: params.search } : {}),
        SortBy: params.sortBy,
        SortDir: params.sortDir,
        IncludePast: true,
        IncludeFuture: true,
        ...(params.year !== null ? { Year: params.year } : {}),
      },
    });

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to load activities");
    }

    setResult(response.data);
  } catch (error) {
    console.error("Error fetching activities:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
};

/**
 * Navigates to the administrative detail view of a specific activity.
 */
export const handleViewActivity = (
  navigate: NavigateFunction,
  activityId: number,
) => {
  navigate(`/admin/activities/${activityId}`);
};

export type { ActivityListItemDto };
