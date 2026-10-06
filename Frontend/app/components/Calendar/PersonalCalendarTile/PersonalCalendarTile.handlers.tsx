import { t } from "i18next";
import toast from "react-hot-toast";
import { getCalendarsMe, postCalendarsMeRotate } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Fetches the authenticated member's personal calendar feed URL.
 *
 * The URL is only ever requested for the member themselves; it contains a secret that grants read
 * access to their enrollments, which is why it is not exposed through any other member endpoint.
 *
 * @param setUrl - State setter receiving the feed URL.
 * @param setLoading - Callback toggling the loading state.
 */
export const loadCalendarUrl = async (
  setUrl: (url: string | null) => void,
  setLoading: (loading: boolean) => void,
) => {
  try {
    setLoading(true);
    const response = await getCalendarsMe();

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to load calendar URL");
    }

    setUrl(response.data.url);
  } catch (error) {
    console.error("Error while loading the calendar URL:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
    setUrl(null);
  } finally {
    setLoading(false);
  }
};

/**
 * Copies the personal calendar feed URL to the clipboard.
 *
 * @param url - The feed URL to copy.
 */
export const copyCalendarUrl = async (url: string) => {
  toast.promise(navigator.clipboard.writeText(url), {
    loading: t("copying"),
    success: t("copy_successful"),
    error: (error) => appendErrorMessage(t("copy_failed"), error),
  });
};

/**
 * Regenerates the personal calendar feed URL, immediately invalidating the previous one.
 *
 * This is the only way to revoke a URL that has been shared or leaked, so any calendar application
 * still subscribed to the old URL will stop receiving updates and must be pointed at the new one.
 *
 * @param setUrl - State setter receiving the new feed URL.
 * @param setRotating - Callback toggling the rotating state.
 */
export const rotateCalendarUrl = async (
  setUrl: (url: string | null) => void,
  setRotating: (rotating: boolean) => void,
) => {
  const rotateProcess = async () => {
    try {
      setRotating(true);
      const response = await postCalendarsMeRotate();

      if (response.error || !response.data) {
        throw response.error ?? new Error("Failed to reset calendar URL");
      }

      setUrl(response.data.url);
      return response.data;
    } finally {
      setRotating(false);
    }
  };

  toast.promise(rotateProcess(), {
    loading: t("saving"),
    success: t("calendar_link_reset"),
    error: (error) =>
      appendErrorMessage(t("calendar_link_reset_failed"), error),
  });
};
