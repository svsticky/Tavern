import { t } from "i18next";
import { AlertTriangle, CalendarClock } from "lucide-react";
import { useEffect, useState } from "react";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import {
  copyCalendarUrl,
  loadCalendarUrl,
  rotateCalendarUrl,
} from "./PersonalCalendarTile.handlers";

/**
 * A tile that lets a member subscribe to their personal activity calendar.
 *
 * The tile exposes a single subscription URL that any calendar application can poll, so enrollments
 * stay in sync automatically instead of being copied over once. Because calendar applications cannot
 * authenticate, that URL embeds an unguessable secret and is therefore treated as sensitive: it is
 * never shown to anyone but its owner, the warning below spells out what sharing it would expose, and
 * the member can regenerate it at any time to revoke a link that has leaked.
 *
 * @component
 */
export default function PersonalCalendarTile() {
  const [url, setUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [rotating, setRotating] = useState(false);

  useEffect(() => {
    loadCalendarUrl(setUrl, setLoading);
  }, []);

  return (
    <BorderedTile
      subtitle={t("personal_calendar_explanation")}
      icon={CalendarClock}
    >
      <div className="flex flex-col gap-4 mt-4">
        <div className="flex items-start gap-2 rounded-xl bg-yellow-50 border border-yellow-200 p-3">
          <AlertTriangle
            className="text-yellow-600 shrink-0 mt-0.5"
            size={18}
            aria-hidden="true"
          />
          <div className="flex flex-col gap-1 text-sm text-yellow-900">
            <span>{t("personal_calendar_secret_warning")}</span>
            <span>{t("personal_calendar_scope_warning")}</span>
          </div>
        </div>

        {loading ? (
          <p className="text-sm text-gray-400">{t("loading")}</p>
        ) : url ? (
          <>
            <code className="block w-full overflow-x-auto rounded-xl bg-gray-50 border border-gray-200 p-3 text-xs text-gray-600">
              {url}
            </code>

            <div className="flex flex-col sm:flex-row gap-2">
              <Button
                variant="primary"
                className="w-full sm:w-auto"
                onClick={() => copyCalendarUrl(url)}
              >
                {t("copy_calendar_link")}
              </Button>

              <Button
                variant="danger"
                className="w-full sm:w-auto"
                disabled={rotating}
                onClick={() => rotateCalendarUrl(setUrl, setRotating)}
              >
                {t("reset_calendar_link")}
                {rotating && "..."}
              </Button>
            </div>
          </>
        ) : (
          <p className="text-sm text-gray-400">{t("loading_failed")}</p>
        )}
      </div>
    </BorderedTile>
  );
}
