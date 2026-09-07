import { t } from "i18next";
import { Download, FileText } from "lucide-react";
import type { EnrollmentResponseDto } from "~/api";
import Tile from "../../Tiles/Tile";
import Button from "../../UI/Button";
import ParticipantTile from "./ParticipantTile";

/**
 * A layout component that displays a collection of activity participants in a responsive grid.
 * It renders a list of `ParticipantTile` components and includes a total count badge in the header.
 *
 * Note: If the `enrollments` array is empty, this component will return `null` and render nothing.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {string} [props.title] - Optional override for the section title. Defaults to the localized "participants" string.
 * @param {EnrollmentResponseDto[]} props.enrollments - An array of enrollment data objects to be displayed.
 * @param {boolean} [props.isAdmin] - Optional flag indicating whether the viewer has admin privileges.
 * @param {() => void} [props.onExportCsv] - Optional callback to trigger a CSV export.
 * @param {() => void} [props.onExportPdf] - Optional callback to trigger a PDF checklist export.
 */
export default function ActivityParticipantsTile({
  title,
  enrollments,
  isAdmin,
  onExportCsv,
  onExportPdf,
}: {
  title?: string;
  enrollments: EnrollmentResponseDto[];
  isAdmin?: boolean;
  onExportCsv?: () => void;
  onExportPdf?: () => void;
}) {
  const count = enrollments.length;

  if (count === 0) return null;

  return (
    <Tile className="w-full">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-8">
        <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-3">
          {title || t("participants")}
          <span className="bg-gray-100 text-gray-500 text-sm py-1 px-3 rounded-full font-bold">
            {count}
          </span>
        </h2>
        <div className="flex items-center gap-2">
          {onExportCsv && (
            <Button
              variant="secondary"
              onClick={onExportCsv}
              className="text-xs px-3 py-1 flex items-center gap-1.5 w-fit"
            >
              <Download size={14} />
              {t("export_csv")}
            </Button>
          )}
          {onExportPdf && (
            <Button
              variant="secondary"
              onClick={onExportPdf}
              className="text-xs px-3 py-1 flex items-center gap-1.5 w-fit"
            >
              <FileText size={14} />
              {t("export_pdf")}
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fit,minmax(250px,1fr))] gap-4">
        {enrollments.map((enrollment, idx) => (
          <ParticipantTile
            key={idx}
            enrollment={enrollment}
            isAdmin={isAdmin}
          />
        ))}
      </div>
    </Tile>
  );
}
