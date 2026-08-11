import { t } from "i18next";
import { AlertTriangle, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  deleteMembersById,
  getStudies,
  getStudyenrollments,
  type Study,
  type StudyEnrollmentResponseDto,
  type StudyStatus,
} from "~/api";
import { loadStudyStartDates } from "~/components/Register/RegisterForm/RegisterForm.handlers";
import BorderedTile from "~/components/Tiles/BorderedTile";
import DataTableTile, { type Column } from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Select from "~/components/UI/Select";
import { useAuth } from "~/context/AuthContext";
import { appendErrorMessage } from "~/util/error.util";
import {
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
} from "./admin/edit-member/edit-member.handlers";

/**
 * Page to update your study. You can only update studies where you have been enrolled
 * for longer then the nominal study enrollment time, or choose to delete your account.
 *
 * @page
 * @component
 */
export default function UpdateStudies() {
  const authService = useAuth();
  const [loading, setLoading] = useState(true);
  const [memberId, setMemberId] = useState<string | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedStudyId, setSelectedStudyId] = useState<number | "">("");
  const [selectedStartDate, setSelectedStartDate] = useState<string>("");
  const [startDatesRaw, setStartDatesRaw] = useState<string>("");
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

      setMemberId(tokenParsed.UserId);

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
      await loadStudyStartDates(setStartDatesRaw);
      setLoading(false);
    };

    loadData();
  }, [authService]);

  const startDateOptions = useMemo(() => {
    const configured = startDatesRaw
      .split(",")
      .map((s) => s.trim())
      .filter((s) => s.includes("-"));

    if (configured.length === 0) return [];

    const now = new Date();
    const currentYear = now.getFullYear();
    const yearsBack = 6;
    const generatedDates: Date[] = [];

    configured.forEach((md) => {
      const [monthStr, dayStr] = md.split("-");
      const month = Number.parseInt(monthStr, 10) - 1;
      const day = Number.parseInt(dayStr, 10);

      for (let y = currentYear - yearsBack; y <= currentYear + 1; y++) {
        const d = new Date(Date.UTC(y, month, day));
        if (!Number.isNaN(d.getTime())) {
          generatedDates.push(d);
        }
      }
    });

    generatedDates.sort((a, b) => a.getTime() - b.getTime());

    const pastDates = generatedDates.filter((d) => d <= now);
    const nextFutureDate = generatedDates.find((d) => d > now);

    const validDates = [...pastDates];
    if (nextFutureDate) {
      validDates.push(nextFutureDate);
    }

    return validDates.map((d) => {
      const isoDate = d.toISOString().split("T")[0];
      return {
        value: isoDate,
        label: isoDate,
      };
    });
  }, [startDatesRaw]);

  useEffect(() => {
    if (startDateOptions.length > 0 && !selectedStartDate) {
      const nowTime = Date.now();
      let closestOption = startDateOptions[0];
      let minDiff = Math.abs(
        new Date(startDateOptions[0].value).getTime() - nowTime,
      );

      startDateOptions.forEach((opt) => {
        const diff = Math.abs(new Date(opt.value).getTime() - nowTime);
        if (diff < minDiff) {
          minDiff = diff;
          closestOption = opt;
        }
      });

      setSelectedStartDate(String(closestOption.value));
    }
  }, [startDateOptions, selectedStartDate]);

  const handleDeleteAccount = async () => {
    if (!memberId) return;

    const deleteProcess = async () => {
      try {
        setLoading(true);
        const response = await deleteMembersById({ path: { id: memberId } });
        if (response.error) {
          throw response.message ?? new Error("Failed to delete account");
        }
        await authService?.logout(window.location.origin);
      } catch (err) {
        console.error("Failed to delete account:", err);
        throw err;
      } finally {
        setLoading(false);
      }
    };

    toast.promise(deleteProcess(), {
      loading: t("deleting"),
      success: t("delete_success"),
      error: (error) => appendErrorMessage(t("delete_error"), error),
    });
  };

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
            studies.filter((s) => s.id === item.studyId)[0]
              .nominalDurationYears!,
        );

        if (
          item.status !== "Enrolled" ||
          new Date(item.enrollmentDate) > deadline
        )
          return item.status === "Completed"
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
      <div className="flex flex-col gap-6 p-4">
        <BorderedTile>
          <DataTableTile
            data={enrollments}
            columns={enrollmentColumns}
            emptyText={t("no_enrollments_found")}
          />

          <div className="flex flex-col sm:flex-row items-end gap-4 w-full mt-6 pt-4 border-t border-slate-200">
            <div className="flex-1 w-full">
              <Select
                label={t("add_study_enrollment")}
                onChange={(e) => {
                  if (e.target.value) {
                    setSelectedStudyId(parseInt(e.target.value, 10));
                  } else {
                    setSelectedStudyId("");
                  }
                }}
                defaultValue=""
                options={[
                  { value: "", label: `${t("select_a_study")}...` },
                  ...studies.map((study) => ({
                    value: study.id!.toString(),
                    label: study.title,
                  })),
                ]}
              />
            </div>

            <div className="w-full sm:w-56">
              <Select
                label={t("start_date")}
                value={selectedStartDate}
                onChange={(e) => setSelectedStartDate(e.target.value)}
                options={startDateOptions}
              />
            </div>

            <Button
              variant="primary"
              onClick={() =>
                handleAddEnrollment(
                  memberId ?? undefined,
                  selectedStudyId,
                  setLoading,
                  setEnrollments,
                  selectedStartDate,
                )
              }
              disabled={!selectedStudyId || !selectedStartDate || loading}
              className="h-[46px] whitespace-nowrap px-6"
              type="button"
            >
              {t("add")}
            </Button>
          </div>
        </BorderedTile>

        <div className="mt-8 border border-red-200 bg-red-50/50 rounded-xl p-6 shadow-sm flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6 transition-all">
          <div className="flex items-start gap-4">
            <div className="p-3 bg-red-100 text-red-600 rounded-xl shrink-0 mt-0.5 sm:mt-0">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <div className="space-y-1">
              <h3 className="font-semibold text-lg text-slate-900 flex items-center gap-2">
                {t("delete_account", "Account Verwijderen")}
              </h3>
              <p className="text-sm text-slate-600 leading-relaxed max-w-xl">
                {t(
                  "delete_account_description",
                  "Als je niet meer studeert of geen lid meer wilt zijn, kun je hier je account definitief laten verwijderen en anonimiseren.",
                )}
              </p>
            </div>
          </div>

          <Button
            variant="secondary"
            className="bg-red-600 hover:bg-red-700 active:bg-red-800 text-white font-medium px-5 py-2.5 rounded-lg border-none shadow-sm hover:shadow transition-all flex items-center gap-2 shrink-0 self-stretch sm:self-auto justify-center"
            onClick={() => setIsDeleteModalOpen(true)}
            disabled={loading}
          >
            <Trash2 className="w-4 h-4" />
            <span>{t("delete_account", "Account Verwijderen")}</span>
          </Button>
        </div>
      </div>

      <Modal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        title={t("delete_account", "Account Verwijderen")}
      >
        <div className="flex flex-col gap-5 p-1">
          <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-100 rounded-xl text-red-700 text-sm">
            <AlertTriangle className="w-5 h-5 shrink-0" />
            <span>
              {t(
                "are_you_sure_delete_own_account",
                "Weet je zeker dat je je account wilt verwijderen? Lopen de studie-inschrijvingen worden verwijderd en je persoonsgegevens worden geanonimiseerd.",
              )}
            </span>
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button
              variant="secondary"
              onClick={() => setIsDeleteModalOpen(false)}
              disabled={loading}
              className="px-4 py-2"
            >
              {t("cancel", "Annuleren")}
            </Button>
            <Button
              variant="primary"
              className="bg-red-600 hover:bg-red-700 active:bg-red-800 text-white border-transparent px-5 py-2 flex items-center gap-2 shadow-sm"
              onClick={handleDeleteAccount}
              disabled={loading}
            >
              <Trash2 className="w-4 h-4" />
              <span>
                {loading
                  ? t("deleting", "Verwijderen...")
                  : t("delete", "Definitief Verwijderen")}
              </span>
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
}
