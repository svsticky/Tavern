import { t } from "i18next";
import { useEffect, useState } from "react";
import {
  getStudies,
  getStudyenrollments,
  type Study,
  type StudyEnrollmentResponseDto,
  type StudyStatus,
} from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import DataTableTile, { type Column } from "~/components/Tiles/DataTableTile";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { useAuth } from "~/context/AuthContext";
import { handleUpdateEnrollmentStatus } from "./admin/edit-member/edit-member.handlers";

/**
 * Page to update your study. You can only update studies where you have been enrolled
 * for longer then the nominal study enrollment time.
 *
 * @page
 * @component
 */
export default function UpdateStudies() {
  const authService = useAuth();
  const [loading, setLoading] = useState(true);
  const [enrollments, setEnrollments] = useState<StudyEnrollmentResponseDto[]>(
    [],
  );
  const [studies, setStudies] = useState<Study[]>([]);

  useEffect(() => {
    if (!authService) return;

    const loadData = async () => {
      setLoading(true);
      const tokenParsed = await authService.getTokenParsed();

      if (!tokenParsed) {
        console.error("User not authenticated");
        return;
      }

      const studyEnrollmentsResponse = await getStudyenrollments({
        query: { MemberId: tokenParsed.UserId },
      });

      if (studyEnrollmentsResponse.error || !studyEnrollmentsResponse.data) {
        throw (
          studyEnrollmentsResponse.error ??
          new Error("Failed to load study enrollments")
        );
      }

      setEnrollments(studyEnrollmentsResponse.data);

      const studiesResponse = await getStudies();

      if (studiesResponse.error || !studiesResponse.data) {
        throw studiesResponse.error ?? new Error("Failed to load studies");
      }

      setStudies(studiesResponse.data);
      setLoading(false);
    };

    loadData();
  }, [authService]);

  const enrollmentColumns: Column<StudyEnrollmentResponseDto>[] = [
    {
      header: t("study"),
      render: (item) => item.studyTitle,
    },
    {
      header: t("start_date"),
      render: (item) => new Date(item.enrollmentDate).toLocaleDateString(),
    },
    {
      header: t("status"),
      render: (item) => {
        if (!studies || studies.length === 0) return t("loading");

        const deadline = new Date();

        deadline.setFullYear(
          deadline.getFullYear() +
            studies.filter((s) => s.id == item.studyId)[0]
              .nominalDurationYears!,
        );

        if (
          item.status != "Enrolled" ||
          new Date(item.enrollmentDate) > deadline
        )
          return item.status == "Completed"
            ? t("status_completed")
            : t("status_dropped_out");

        return (
          <select
            value={item.status}
            onChange={(e) =>
              handleUpdateEnrollmentStatus(
                item.id,
                e.target.value as StudyStatus,
                setLoading,
                setEnrollments,
              )
            }
            className="text-xs font-semibold px-2 py-1 rounded-full border-none cursor-pointer focus:ring-2 focus:ring-blue-500 bg-blue-100 text-blue-700"
            disabled={loading}
          >
            <option value={"Enrolled"}>{t("status_in_progress")}</option>
            <option value={"Completed"}>{t("status_completed")}</option>
            <option value={"DroppedOut"}>{t("status_dropped_out")}</option>
          </select>
        );
      },
    },
  ];

  return (
    <>
      <PageHeader title={t("update_study_progress")} />
      <BorderedTile>
        <DataTableTile
          data={enrollments}
          columns={enrollmentColumns}
          emptyText={t("no_enrollments_found")}
        />
      </BorderedTile>
    </>
  );
}
