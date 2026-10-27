export type DashboardWidgetId =
  | "announcements"
  | "upcoming_activities"
  | "my_enrollments"
  | "my_groups";

export type DashboardColumn = "main" | "sidebar";

export interface DashboardWidgetConfig {
  id: DashboardWidgetId;
  visible: boolean;
  column: DashboardColumn;
  order: number;
}

export const DEFAULT_DASHBOARD_WIDGETS: readonly DashboardWidgetConfig[] = [
  { id: "announcements", visible: true, column: "main", order: 0 },
  { id: "upcoming_activities", visible: true, column: "main", order: 1 },
  { id: "my_enrollments", visible: true, column: "sidebar", order: 0 },
  { id: "my_groups", visible: true, column: "sidebar", order: 1 },
];

export const DASHBOARD_STORAGE_KEY_PREFIX = "tavern_dashboard_widgets";

export function getDashboardStorageKey(userId?: string): string {
  return userId
    ? `${DASHBOARD_STORAGE_KEY_PREFIX}_${userId}`
    : DASHBOARD_STORAGE_KEY_PREFIX;
}

/**
 * Loads the dashboard widget configuration from localStorage, falling back to
 * the default configuration if nothing is saved or if stored data is corrupted.
 */
export function loadDashboardWidgetConfig(
  userId?: string,
): DashboardWidgetConfig[] {
  if (typeof window === "undefined" || !window.localStorage) {
    return DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w }));
  }

  try {
    const raw = localStorage.getItem(getDashboardStorageKey(userId));
    if (!raw) return DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w }));

    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w }));
    }

    // Merge with defaults to ensure all required widgets exist
    return DEFAULT_DASHBOARD_WIDGETS.map((defaultWidget) => {
      const match = parsed.find((item) => item?.id === defaultWidget.id);
      if (match && typeof match.visible === "boolean") {
        return {
          id: defaultWidget.id,
          visible: match.visible,
          column: match.column === "sidebar" ? "sidebar" : "main",
          order:
            typeof match.order === "number" ? match.order : defaultWidget.order,
        };
      }
      return { ...defaultWidget };
    });
  } catch {
    return DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w }));
  }
}

/**
 * Saves the dashboard widget configuration to localStorage.
 */
export function saveDashboardWidgetConfig(
  config: DashboardWidgetConfig[],
  userId?: string,
): void {
  if (typeof window === "undefined" || !window.localStorage) return;
  try {
    localStorage.setItem(
      getDashboardStorageKey(userId),
      JSON.stringify(config),
    );
  } catch (err) {
    console.error(
      "Failed to save dashboard widget config to localStorage:",
      err,
    );
  }
}

/**
 * Resets the dashboard widget configuration to default and clears localStorage.
 */
export function resetDashboardWidgetConfig(
  userId?: string,
): DashboardWidgetConfig[] {
  if (typeof window !== "undefined" && window.localStorage) {
    try {
      localStorage.removeItem(getDashboardStorageKey(userId));
    } catch (err) {
      console.error(
        "Failed to clear dashboard widget config from localStorage:",
        err,
      );
    }
  }
  return DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w }));
}
