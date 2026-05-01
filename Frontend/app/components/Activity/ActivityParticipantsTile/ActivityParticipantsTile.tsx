import { t } from "i18next";
import type { EnrollmentResponseDto } from "~/api";
import Tile from "../../Tiles/Tile";
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
 *
 * @example
 * ```tsx
 * <ActivityParticipantsTile
 *   title="Attendees"
 *   enrollments={activity.enrollments}
 * />
 * ```
 */
export default function ActivityParticipantsTile({
  title,
  enrollments,
}: {
  title?: string;
  enrollments: EnrollmentResponseDto[];
}) {
  const count = enrollments.length;

  if (count === 0) return null;

  return (
    <Tile className="w-full">
      <h2 className="text-2xl font-extrabold text-slate-900 mb-8 flex items-center gap-3">
        {title || t("participants")}
        <span className="bg-slate-100 text-slate-500 text-sm py-1 px-3 rounded-full font-bold">
          {count}
        </span>
      </h2>

      <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fit,minmax(250px,1fr))] gap-4">
        {enrollments.map((enrollment, idx) => (
          <ParticipantTile key={idx} enrollment={enrollment} />
        ))}
      </div>
    </Tile>
  );
}
