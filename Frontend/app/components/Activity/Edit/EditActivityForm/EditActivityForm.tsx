import { t } from "i18next";
import { CheckCircle2Icon, RotateCcwIcon, Trash2Icon } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
  GroupResponseDto,
} from "~/api";
import { parseAudience } from "~/types/AudienceMap";
import {
  type ActivityDraft,
  clearActivityDraft,
  extractDraftFromForm,
  isActivityDraftValid,
  isDraftNotEmpty,
  loadActivityDraft,
  saveActivityDraft,
} from "~/util/activityDraft.util";
import { cn } from "~/util/tailwind.util";
import BorderedTile from "../../../Tiles/BorderedTile";
import { NoContentTile } from "../../../Tiles/NoContentTile";
import Button from "../../../UI/Button";
import Checkbox from "../../../UI/Checkbox";
import { useConfirm } from "../../../UI/ConfirmModal/useConfirm";
import Form from "../../../UI/Form/Form";
import { FormHeader } from "../../../UI/Form/FormHeader";
import { FormSection } from "../../../UI/Form/FormSection";
import Input from "../../../UI/Input";
import Select from "../../../UI/Select";
import TextArea from "../../../UI/TextArea";
import EditQuestionTile from "../EditQuestionTile";
import {
  addQuestion,
  formatDateOnly,
  formatForInput,
  handleActivityFormChange,
  handleActivitySubmit,
  handleDeleteActivity,
  loadGroups,
  removeQuestion,
  updateQuestion,
} from "./EditActivityForm.handlers";

const AUDIENCE_CHECKBOXES = [
  { bit: 1, labelKey: "year_1" },
  { bit: 2, labelKey: "year_2" },
  { bit: 4, labelKey: "year_3_plus" },
  { bit: 8, labelKey: "masters" },
  { bit: 16, labelKey: "gratie" },
  { bit: 64, labelKey: "begunstiger" },
  { bit: 32, labelKey: "active_members" },
] as const;

interface DraftRestoredBannerProps {
  draftTimestamp: string | null;
  onDiscard: () => void;
}

function DraftRestoredBanner({
  draftTimestamp,
  onDiscard,
}: DraftRestoredBannerProps) {
  return (
    <div
      role="alert"
      className="mb-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800/60 rounded-xl text-amber-900 dark:text-amber-200"
    >
      <div className="flex items-start sm:items-center gap-3">
        <RotateCcwIcon className="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5 sm:mt-0" />
        <div>
          <p className="font-semibold text-sm">
            {t("draft_restored")}
            {draftTimestamp && (
              <span className="font-normal text-xs text-amber-700 dark:text-amber-300 ml-1.5">
                ({draftTimestamp})
              </span>
            )}
          </p>
          <p className="text-xs text-amber-700 dark:text-amber-300/90 mt-0.5">
            {t("draft_restored_description")}
          </p>
        </div>
      </div>
      <Button
        type="button"
        variant="secondary"
        className="shrink-0 text-xs py-1.5 px-3 border-amber-300 dark:border-amber-700 text-amber-900 dark:text-amber-200 hover:bg-amber-100 dark:hover:bg-amber-900/50"
        onClick={onDiscard}
      >
        <Trash2Icon size={14} className="mr-1 inline text-red-500" />
        {t("discard_draft")}
      </Button>
    </div>
  );
}

/**
 * A comprehensive form component used to either create a new association activity
 * or edit an existing one.
 *
 * Features:
 * - **Dynamic Permissions**: Visibility of administrative fields (like financial GL accounts
 *   or internal Koala settings) is toggled based on the `isBoard` prop.
 * - **Specification Questions**: Manages a sub-state of custom registration questions
 *   that can be added, updated, or removed dynamically.
 * - **Lifecycle Management**: Handles initial data loading for association groups and
 *   automatically formats date strings for HTML5 compatibility.
 * - **Validation**: Tracks form validity based on required fields and submission status.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto | null} props.activity - The existing activity data (if editing) or null (if creating).
 * @param {string | undefined} props.id - The unique identifier of the activity. If present, the form operates in "Edit" mode.
 * @param {boolean} props.canEditStructural - Flag indicating if the user may edit structural/online fields (board or EditAllActivities).
 * @param {boolean} props.canManageFinances - Flag indicating if the user may edit finance fields (board or ManageFinances).
 *
 * @example
 * ```tsx
 * <EditActivityForm
 *   activity={activityData}
 *   id="123"
 *   canEditStructural={true}
 *   canManageFinances={true}
 * />
 * ```
 */
export default function EditActivityForm({
  activity,
  id,
  canEditStructural,
  canManageFinances,
  isBoard = false,
}: {
  activity: ActivityResponseDto | null;
  id: string | undefined;
  /** True when the user may edit structural/online fields (board or EditAllActivities). */
  canEditStructural: boolean;
  /** True when the user may edit finance fields (board or ManageFinances). */
  canManageFinances: boolean;
  /** True when the user is board or candidate board (allows deleting activities). */
  isBoard?: boolean;
}) {
  const navigate = useNavigate();
  const { pathname } = window.location;
  const [confirmModal, confirm] = useConfirm();

  const isEdit = !!id;
  const isCreate = !isEdit && !activity;

  const [draft, setDraft] = useState<ActivityDraft | null>(() =>
    isCreate ? loadActivityDraft() : null,
  );
  const [draftRestored, setDraftRestored] = useState<boolean>(() => !!draft);
  const [draftKey, setDraftKey] = useState<number>(0);
  const [lastSavedAt, setLastSavedAt] = useState<string | null>(null);

  const formRef = useRef<HTMLFormElement>(null);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const questionsMountedRef = useRef(false);

  const audienceMask = parseAudience(
    draft ? draft.allowedAudience : activity?.allowedAudience,
  );
  const hasAudienceSource = isEdit || !!draft;

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [groups, setGroups] = useState<GroupResponseDto[]>([]);
  const [formValid, setFormValid] = useState<boolean>(() => {
    if (isEdit || activity) return true;
    return isActivityDraftValid(draft);
  });
  const [questions, setQuestions] = useState<
    Partial<GetSpecificationQuestionResponseDto>[]
  >(() => {
    if (activity?.specificationQuestions) {
      return activity.specificationQuestions;
    }
    if (draft?.specificationQuestions) {
      return draft.specificationQuestions;
    }
    return [];
  });

  useEffect(() => {
    if (activity?.specificationQuestions) {
      setQuestions(activity.specificationQuestions);
    }
  }, [activity]);

  useEffect(() => {
    if (isEdit || activity) {
      setFormValid(true);
    } else if (draft) {
      setFormValid(isActivityDraftValid(draft));
    }

    loadGroups(setLoading, setGroups);
  }, [isEdit, activity, draft]);

  const triggerAutoSave = useCallback(
    (formEl?: HTMLFormElement | null) => {
      if (isEdit || activity) return;
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
      debounceTimerRef.current = setTimeout(() => {
        const targetForm = formEl || formRef.current;
        if (!targetForm) return;
        const currentDraft = extractDraftFromForm(targetForm, questions);
        if (isDraftNotEmpty(currentDraft)) {
          saveActivityDraft(currentDraft);
          setLastSavedAt(
            new Date().toLocaleTimeString([], {
              hour: "2-digit",
              minute: "2-digit",
            }),
          );
        } else {
          clearActivityDraft();
          setLastSavedAt(null);
        }
      }, 500);
    },
    [isEdit, activity, questions],
  );

  useEffect(() => {
    if (!questionsMountedRef.current) {
      questionsMountedRef.current = true;
      return;
    }
    triggerAutoSave();
  }, [triggerAutoSave]);

  useEffect(() => {
    if (isEdit || activity) return;
    const handleBeforeUnload = () => {
      if (!formRef.current) return;
      const currentDraft = extractDraftFromForm(formRef.current, questions);
      if (isDraftNotEmpty(currentDraft)) {
        saveActivityDraft(currentDraft);
      }
    };
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => {
      window.removeEventListener("beforeunload", handleBeforeUnload);
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, [isEdit, activity, questions]);

  const handleDiscardDraft = async () => {
    const confirmed = await confirm(t("confirm_discard_draft"), {
      title: t("discard_draft"),
      confirmLabel: t("discard_draft"),
    });
    if (!confirmed) return;

    clearActivityDraft();
    setDraft(null);
    setQuestions([]);
    setDraftRestored(false);
    setLastSavedAt(null);
    setFormValid(false);
    setDraftKey((prev) => prev + 1);
    toast.success(t("draft_discarded"));
  };

  const draftTimestamp = draft?.savedAt
    ? new Date(draft.savedAt).toLocaleString(undefined, {
        dateStyle: "short",
        timeStyle: "short",
      })
    : null;

  if (loading) return t("loading");

  return (
    <div>
      <BorderedTile>
        {draftRestored && (
          <DraftRestoredBanner
            draftTimestamp={draftTimestamp}
            onDiscard={handleDiscardDraft}
          />
        )}

        <Form
          key={draftKey}
          ref={formRef}
          onSubmit={(e) =>
            handleActivitySubmit({
              e,
              canEditStructural,
              canManageFinances,
              questions,
              setSaving,
              isEdit,
              id,
              pathname,
              navigate,
            })
          }
          onChange={(e) => {
            handleActivityFormChange(e, setFormValid);
            triggerAutoSave(e.currentTarget);
          }}
        >
          <FormSection title={t("basic_information")}>
            <Input
              label={t("name")}
              name="Name"
              defaultValue={draft?.name ?? activity?.name}
              required
            />
            <Input
              label={t("location")}
              name="Location"
              defaultValue={draft?.location ?? activity?.location}
              required
            />
            <TextArea
              label={t("dutch_description")}
              rows={10}
              name="DutchDescription"
              defaultValue={
                draft?.dutchDescription ?? activity?.dutchDescription
              }
              required
            />
            <TextArea
              label={t("english_description")}
              rows={10}
              name="EnglishDescription"
              defaultValue={
                draft?.englishDescription ?? activity?.englishDescription
              }
              required
            />
          </FormSection>

          <FormSection title={t("planning_enrollment")} columns={2}>
            <Input
              label={t("datetime_start")}
              name="DateTimeStart"
              type="datetime-local"
              defaultValue={
                draft?.dateTimeStart ?? formatForInput(activity?.dateTimeStart)
              }
              required
            />
            <Input
              label={t("datetime_end")}
              name="DateTimeEnd"
              type="datetime-local"
              defaultValue={
                draft?.dateTimeEnd ?? formatForInput(activity?.dateTimeEnd)
              }
              required
            />
            <Input
              label={t("enrollment_deadline")}
              name="EnrollmentDeadline"
              type="datetime-local"
              defaultValue={
                draft?.enrollmentDeadline ??
                formatForInput(activity?.enrollmentDeadline ?? "")
              }
            />
            <Input
              label={t("unenrollment_deadline")}
              name="UnenrollmentDeadline"
              type="datetime-local"
              defaultValue={
                draft?.unenrollmentDeadline ??
                formatForInput(activity?.unenrollmentDeadline ?? "")
              }
            />
            {canEditStructural && (
              <Input
                label={t("enroll_open_date")}
                name="EnrollOpenDate"
                type="datetime-local"
                defaultValue={
                  draft?.enrollOpenDate ??
                  formatForInput(activity?.enrollOpenDate ?? "")
                }
              />
            )}
            <Checkbox
              label={t("weekly_drinks")}
              name="IsWeeklyDrinks"
              defaultChecked={
                draft
                  ? !!draft.isWeeklyDrinks
                  : (activity?.isWeeklyDrinks ?? false)
              }
            />
          </FormSection>

          <FormSection columns={2}>
            <div>
              <FormHeader title={t("target_audience")} border />
              <div className="flex flex-wrap gap-4 p-4 bg-gray-50 rounded-xl mt-4">
                {AUDIENCE_CHECKBOXES.map(({ bit, labelKey }) => (
                  <Checkbox
                    key={bit}
                    label={t(labelKey)}
                    name="AudienceBit"
                    value={String(bit)}
                    defaultChecked={
                      hasAudienceSource ? !!(audienceMask & bit) : true
                    }
                  />
                ))}
              </div>
            </div>
            <div>
              <FormHeader title={t("organizer")} border />
              <Select
                key={`organizer-${groups.length}-${draftKey}`}
                label={t("organizer")}
                name="OrganizerId"
                defaultValue={draft?.organizerId ?? activity?.organizerId ?? ""}
                required
                options={[
                  { value: "", label: t("select_organizer") },
                  ...groups.map((g) => ({
                    value: g?.id ?? 0,
                    label: g?.name ?? "",
                  })),
                ]}
              />
            </div>
          </FormSection>

          <FormSection title={t("finance_capacity")} columns={2}>
            <Input
              label={t("price")}
              name="Price"
              type="number"
              step="0.01"
              min="0"
              defaultValue={
                draft?.price !== undefined
                  ? draft.price
                  : activity?.price?.toFixed(2)
              }
            />
            <Input
              label={t("participant_limit")}
              name="ParticipantLimit"
              type="number"
              min="1"
              defaultValue={
                draft?.participantLimit ?? activity?.participantLimit ?? ""
              }
            />
            {canManageFinances && (
              <>
                <Input
                  label={t("vat_rate")}
                  name="VatRate"
                  type="number"
                  defaultValue={draft?.vatRate ?? activity?.vatRate ?? ""}
                />
                <Input
                  label={`${t("gl_account_id")} (${t("leave_empty_for_group_default")})`}
                  name="GLAccountId"
                  defaultValue={
                    draft?.glAccountId ?? activity?.glAccountId ?? ""
                  }
                />
                <Input
                  label={`${t("cost_unit_id")} (${t("leave_empty_for_group_default")})`}
                  name="CostUnitId"
                  defaultValue={draft?.costUnitId ?? activity?.costUnitId ?? ""}
                />
                <Input
                  label={`${t("cost_center_id")} (${t("leave_empty_for_group_default")})`}
                  name="CostCenterId"
                  defaultValue={
                    draft?.costCenterId ?? activity?.costCenterId ?? ""
                  }
                />
                <Input
                  label={t("payment_deadline")}
                  name="PaymentDeadline"
                  type="date"
                  defaultValue={
                    draft?.paymentDeadline ??
                    formatDateOnly(activity?.paymentDeadline ?? "")
                  }
                />
                <Checkbox
                  label={t("is_open_for_payment")}
                  name="IsOpenForPayment"
                  defaultChecked={
                    draft
                      ? !!draft.isOpenForPayment
                      : (activity?.isOpenForPayment ?? false)
                  }
                />
              </>
            )}
          </FormSection>

          <FormSection columns={2}>
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2">
                <FormHeader title={t("settings")} border />
              </div>
              {canEditStructural && (
                <>
                  <Checkbox
                    label={t("is_enrollable")}
                    name="IsEnrollable"
                    defaultChecked={
                      draft
                        ? !!draft.isEnrollable
                        : (activity?.isEnrollable ?? false)
                    }
                  />
                  <Checkbox
                    label={t("show_in_koala")}
                    name="ShowInKoala"
                    defaultChecked={
                      draft
                        ? !!draft.showInKoala
                        : (activity?.showInKoala ?? false)
                    }
                  />
                  <Checkbox
                    label={t("show_on_website")}
                    name="ShowOnWebsite"
                    defaultChecked={
                      draft
                        ? !!draft.showOnWebsite
                        : (activity?.showOnWebsite ?? false)
                    }
                  />
                </>
              )}
              <Checkbox
                label={t("are_participants_visible")}
                name="AreParticipantsVisible"
                defaultChecked={
                  draft
                    ? !!draft.areParticipantsVisible
                    : (activity?.areParticipantsVisible ?? true)
                }
              />
              <Checkbox
                label={t("is_adult_only")}
                name="IsAdultOnly"
                defaultChecked={
                  draft ? !!draft.isAdultOnly : (activity?.isAdultOnly ?? false)
                }
              />
            </div>
            <div>
              <FormHeader title={t("poster")} border />
              <input
                name="Poster"
                type="file"
                accept="image/png, image/jpeg, image/gif, image/webp"
                className={cn(
                  "w-full px-2 py-auto h-6 border border-dashed border-gray-300 rounded-md mt-4",
                )}
              />
              {isEdit && (
                <p className="text-xs text-gray-400 mt-1 italic">
                  {t("leave_empty_to_keep_current")}
                </p>
              )}
            </div>
          </FormSection>

          <FormSection title={t("specification_questions")} columns={1}>
            <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4">
              <span className="min-w-0 flex-1">
                {t("specification_questions_description")}
              </span>
              <Button
                type="button"
                variant="secondary"
                onClick={() => addQuestion(questions, setQuestions)}
                className="flex-none whitespace-nowrap"
              >
                + {t("add_question")}
              </Button>
            </div>
            {questions.map((q, index) => (
              <EditQuestionTile
                key={index}
                question={q}
                onRemove={() => removeQuestion(index, questions, setQuestions)}
                onUpdate={(field, value) =>
                  updateQuestion(index, field, value, questions, setQuestions)
                }
              />
            ))}

            {questions.length === 0 && (
              <NoContentTile text={t("no_specification_questions_yet")} />
            )}
          </FormSection>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
            <div className="flex flex-col sm:flex-row gap-3 flex-1">
              <Button
                type="submit"
                disabled={saving || !formValid}
                className="w-full sm:w-auto"
              >
                {saving
                  ? t("saving")
                  : isEdit
                    ? t("save")
                    : t("create_activity")}
              </Button>

              {isBoard && isEdit && activity && (
                <Button
                  type="button"
                  variant="danger"
                  className="w-full sm:w-auto flex items-center justify-center gap-2"
                  onClick={async () => {
                    if (
                      !(await confirm(t("are_you_sure_delete_activity"), {
                        title: t("delete"),
                        confirmLabel: t("delete"),
                      }))
                    ) {
                      return;
                    }
                    handleDeleteActivity(activity.id, () =>
                      navigate(
                        `${pathname.startsWith("/admin") ? "/admin" : ""}/activities`,
                      ),
                    );
                  }}
                >
                  <Trash2Icon size={18} />
                  {t("delete")}
                </Button>
              )}
            </div>

            {!isEdit && !activity && lastSavedAt && (
              <div className="flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-400 self-end sm:self-center">
                <CheckCircle2Icon
                  size={14}
                  className="text-emerald-500 shrink-0"
                />
                <span>
                  {t("draft_autosaved")} ({lastSavedAt})
                </span>
              </div>
            )}
          </div>
        </Form>
      </BorderedTile>

      {confirmModal}
    </div>
  );
}
