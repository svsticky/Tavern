import { t } from "i18next";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
  GroupResponseDto,
} from "~/api";
import BorderedTile from "../../../Tiles/BorderedTile";
import { NoContentTile } from "../../../Tiles/NoContentTile";
import Button from "../../../UI/Button";
import Checkbox from "../../../UI/Checkbox";
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
  loadGroups,
  removeQuestion,
  updateQuestion,
} from "./EditActivityForm.handlers";

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
 * @param {boolean} props.isBoard - Flag indicating if the current user has board-level permissions.
 *
 * @example
 * ```tsx
 * <EditActivityForm
 *   activity={activityData}
 *   id="123"
 *   isBoard={true}
 * />
 * ```
 */
export default function EditActivityForm({
  activity,
  id,
  isBoard,
}: {
  activity: ActivityResponseDto | null;
  id: string | undefined;
  isBoard: boolean;
}) {
  const navigate = useNavigate();
  const { pathname } = window.location;

  const isEdit = !!id;

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [groups, setGroups] = useState<GroupResponseDto[]>([]);
  const [formValid, setFormValid] = useState(isEdit);
  const [questions, setQuestions] = useState<
    Partial<GetSpecificationQuestionResponseDto>[]
  >([]);

  useEffect(() => {
    if (activity?.specificationQuestions) {
      setQuestions(activity.specificationQuestions);
    }
  }, [activity]);

  useEffect(() => {
    if (isEdit) {
      setFormValid(true);
    }

    loadGroups(setLoading, setGroups);
  }, [isEdit]);

  if (loading) return t("loading");

  return (
    <div>
      <BorderedTile>
        <Form
          onSubmit={(e) =>
            handleActivitySubmit({
              e,
              isBoard,
              questions,
              setSaving,
              isEdit,
              id,
              pathname,
              navigate,
            })
          }
          onChange={(e) => handleActivityFormChange(e, setFormValid)}
        >
          <FormSection title={t("basic_information")}>
            <Input
              label={t("name")}
              name="Name"
              defaultValue={activity?.name}
              required
            />
            <Input
              label={t("location")}
              name="Location"
              defaultValue={activity?.location}
              required
            />
            <TextArea
              label={t("dutch_description")}
              rows={10}
              name="DutchDescription"
              defaultValue={activity?.dutchDescription}
            />
            <TextArea
              label={t("english_description")}
              rows={10}
              name="EnglishDescription"
              defaultValue={activity?.englishDescription}
            />
          </FormSection>

          <FormSection title={t("planning_enrollment")} columns={2}>
            <Input
              label={t("datetime_start")}
              name="DateTimeStart"
              type="datetime-local"
              defaultValue={formatForInput(activity?.dateTimeStart)}
              required
            />
            <Input
              label={t("datetime_end")}
              name="DateTimeEnd"
              type="datetime-local"
              defaultValue={formatForInput(activity?.dateTimeEnd)}
              required
            />
            <Input
              label={t("enrollment_deadline")}
              name="EnrollmentDeadline"
              type="datetime-local"
              defaultValue={formatForInput(activity?.enrollmentDeadline ?? "")}
            />
            <Input
              label={t("unenrollment_deadline")}
              name="UnenrollmentDeadline"
              type="datetime-local"
              defaultValue={formatForInput(
                activity?.unenrollmentDeadline ?? "",
              )}
            />
            {isBoard && (
              <Input
                label={t("enroll_open_date")}
                name="EnrollOpenDate"
                type="datetime-local"
                defaultValue={formatForInput(activity?.enrollOpenDate ?? "")}
              />
            )}
            <Checkbox
              label={t("weekly_drinks")}
              name="IsWeeklyDrinks"
              defaultChecked={activity?.isWeeklyDrinks ?? false}
            />
          </FormSection>

          <FormSection columns={2}>
            <div>
              <FormHeader title={t("target_audience")} border />
              <div className="flex flex-wrap gap-4 p-4 bg-gray-50 rounded-xl mt-4">
                <Checkbox
                  label={t("year_1")}
                  name="AudienceBit"
                  value="1"
                  defaultChecked={
                    isEdit ? !!(activity?.allowedAudience ?? 0 & 1) : true
                  }
                />
                <Checkbox
                  label={t("year_2")}
                  name="AudienceBit"
                  value="2"
                  defaultChecked={
                    isEdit ? !!(activity?.allowedAudience ?? 0 & 2) : true
                  }
                />
                <Checkbox
                  label={t("year_3_plus")}
                  name="AudienceBit"
                  value="4"
                  defaultChecked={
                    isEdit ? !!(activity?.allowedAudience ?? 0 & 4) : true
                  }
                />
                <Checkbox
                  label={t("masters")}
                  name="AudienceBit"
                  value="8"
                  defaultChecked={
                    isEdit ? !!(activity?.allowedAudience ?? 0 & 8) : true
                  }
                />
                <Checkbox
                  label={t("gratie")}
                  name="AudienceBit"
                  value="16"
                  defaultChecked={
                    isEdit ? !!(activity?.allowedAudience ?? 0 & 16) : true
                  }
                />
              </div>
            </div>
            <div>
              <FormHeader title={t("organizer")} border />
              <Select
                label={t("organizer")}
                name="OrganizerId"
                defaultValue={activity?.organizerId ?? ""}
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
              defaultValue={activity?.price}
            />
            <Input
              label={t("participant_limit")}
              name="ParticipantLimit"
              type="number"
              defaultValue={activity?.participantLimit ?? ""}
            />
            {isBoard && (
              <>
                <Input
                  label={t("vat_rate")}
                  name="VatRate"
                  type="number"
                  defaultValue={activity?.vatRate ?? ""}
                />
                <Input
                  label={t("gl_account_id")}
                  name="GLAccountId"
                  defaultValue={activity?.glAccountId ?? ""}
                />
                <Input
                  label={t("cost_center_id")}
                  name="CostCenterId"
                  defaultValue={activity?.costCenterId ?? ""}
                />
                <Input
                  label={t("payment_deadline")}
                  name="PaymentDeadline"
                  type="date"
                  defaultValue={formatDateOnly(activity?.paymentDeadline ?? "")}
                />
              </>
            )}
          </FormSection>

          <FormSection columns={2}>
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2">
                <FormHeader title={t("settings")} border />
              </div>
              {isBoard && (
                <>
                  <Checkbox
                    label={t("is_enrollable")}
                    name="IsEnrollable"
                    defaultChecked={activity?.isEnrollable ?? true}
                  />
                  <Checkbox
                    label={t("show_in_koala")}
                    name="ShowInKoala"
                    defaultChecked={activity?.showInKoala ?? true}
                  />
                  <Checkbox
                    label={t("show_on_website")}
                    name="ShowOnWebsite"
                    defaultChecked={activity?.showOnWebsite ?? true}
                  />
                </>
              )}
              <Checkbox
                label={t("are_participants_visible")}
                name="AreParticipantsVisible"
                defaultChecked={activity?.areParticipantsVisible ?? true}
              />
              <Checkbox
                label={t("is_adult_only")}
                name="IsAdultOnly"
                defaultChecked={activity?.isAdultOnly ?? false}
              />
            </div>
            <div>
              <FormHeader title={t("poster")} border />
              <input
                name="Poster"
                type="file"
                accept="image/png, image/jpeg, image/gif, image/webp"
                className="w-full p-2 border border-dashed rounded-lg mt-4"
              />
              {isEdit && (
                <p className="text-xs text-gray-400 mt-2 italic">
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

          <Button
            type="submit"
            disabled={saving || !formValid}
            className="w-full"
          >
            {saving ? t("saving") : isEdit ? t("save") : t("create_activity")}
          </Button>
        </Form>
      </BorderedTile>
    </div>
  );
}
