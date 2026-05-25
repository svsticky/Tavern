import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  type GetSpecificationQuestionResponseDto,
  type GroupResponseDto,
  getGroups,
  postActivities,
  putActivitiesById,
} from "~/api";
import { audienceMap } from "~/types/AudienceMap";
import { getAssociationYear } from "~/util/date.util";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Truncates an ISO date string to a format compatible with HTML `datetime-local` inputs.
 *
 * @param isoString - The ISO date string (e.g., from the API).
 * @returns A string in the format "YYYY-MM-DDTHH:mm".
 */
export const formatForInput = (isoString?: string) =>
  isoString ? isoString.substring(0, 16) : "";

/**
 * Truncates an ISO date string to a date-only format compatible with HTML `date` inputs.
 *
 * @param isoString - The ISO date string.
 * @returns A string in the format "YYYY-MM-DD".
 */
export const formatDateOnly = (isoString?: string) =>
  isoString ? isoString.substring(0, 10) : "";

/**
 * Fetches active association groups for the current membership year from the API.
 *
 * @param setLoading - Callback to update the loading state.
 * @param setGroups - Callback to update the state with the retrieved groups.
 * @returns A Promise that resolves when the groups are loaded or the request fails.
 */
export const loadGroups = async (
  setLoading: (loading: boolean) => void,
  setGroups: (groups: GroupResponseDto[]) => void,
) => {
  try {
    const groupsRes = await getGroups({
      query: { IncludeInactive: false, MembershipYear: getAssociationYear() },
    });
    if (groupsRes.error) {
      throw groupsRes.error ?? new Error("Failed to load groups");
    }
    if (groupsRes.data) setGroups(groupsRes.data);
  } catch (error) {
    console.error("Error loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
};

/**
 * Validates the activity form by checking if all required fields are present in the FormData.
 *
 * @param e - The form event.
 * @param setFormValid - Callback to update the validity state of the form.
 */
export const handleActivityFormChange = (
  e: React.FormEvent<HTMLFormElement>,
  setFormValid: (valid: boolean) => void,
) => {
  const fd = new FormData(e.currentTarget);
  const required = [
    "Name",
    "DateTimeStart",
    "DateTimeEnd",
    "Location",
    "OrganizerId",
    "DutchDescription",
    "EnglishDescription",
  ];
  setFormValid(required.every((field) => !!fd.get(field)));
};

/**
 * Adds a new empty specification question template to the list of questions.
 *
 * @param questions - Current list of questions.
 * @param setQuestions - Callback to update the questions state.
 */
export const addQuestion = (
  questions: Partial<GetSpecificationQuestionResponseDto>[],
  setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void,
) => {
  setQuestions([
    ...questions,
    {
      questionDutch: "",
      questionEnglish: "",
      type: "String",
      isMandatory: false,
      isPublic: true,
      options: [],
    },
  ]);
};

/**
 * Removes a specification question from the list at a specific index.
 *
 * @param index - The index of the question to remove.
 * @param questions - Current list of questions.
 * @param setQuestions - Callback to update the questions state.
 */
export const removeQuestion = (
  index: number,
  questions: Partial<GetSpecificationQuestionResponseDto>[],
  setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void,
) => {
  setQuestions(questions.filter((_, i) => i !== index));
};

/**
 * Updates a specific field of a question within the questions array.
 *
 * @param index - The index of the question to update.
 * @param field - The key of the GetSpecificationQuestionResponseDto to modify.
 * @param value - The new value for the field.
 * @param questions - Current list of questions.
 * @param setQuestions - Callback to update the questions state.
 */
export const updateQuestion = (
  index: number,
  field: keyof GetSpecificationQuestionResponseDto,
  value: any,
  questions: Partial<GetSpecificationQuestionResponseDto>[],
  setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void,
) => {
  const newQuestions = [...questions];
  newQuestions[index] = { ...newQuestions[index], [field]: value };
  setQuestions(newQuestions);
};

/**
 * Arguments for the `handleActivitySubmit` function.
 * @interface HandleActivitySubmitArgs
 */
type HandleActivitySubmitArgs = {
  e: React.FormEvent<HTMLFormElement>;
  isBoard: boolean;
  questions: Partial<GetSpecificationQuestionResponseDto>[];
  setSaving: (saving: boolean) => void;
  isEdit: boolean;
  id: string | undefined;
  pathname: string;
  navigate: NavigateFunction;
};

/**
 * Handles the submission of the activity form, converting FormData into an API-compatible payload.
 * Supports both creation and updates, manages file uploads (posters), and handles toast notifications.
 *
 * @param args - The submission arguments and state setters.
 * @returns A Promise that resolves after the submission and redirection.
 */
export const handleActivitySubmit = async ({
  e,
  isBoard,
  questions,
  setSaving,
  isEdit,
  id,
  pathname,
  navigate,
}: HandleActivitySubmitArgs) => {
  e.preventDefault();

  const fd = new FormData(e.currentTarget);
  const audienceFlags = fd
    .getAll("AudienceBit")
    .reduce((acc, val) => acc + Number(val), 0);

  setSaving(true);

  const payload = {
    body: {
      Name: fd.get("Name") as string,
      Location: fd.get("Location") as string,
      DutchDescription: fd.get("DutchDescription") as string,
      EnglishDescription: fd.get("EnglishDescription") as string,
      Price: Number(fd.get("Price")) || 0,
      ParticipantLimit: fd.get("ParticipantLimit")
        ? Number(fd.get("ParticipantLimit"))
        : undefined,
      OrganizerId: fd.get("OrganizerId")
        ? Number(fd.get("OrganizerId"))
        : undefined,
      DateTimeStart: new Date(fd.get("DateTimeStart") as string).toISOString(),
      DateTimeEnd: new Date(fd.get("DateTimeEnd") as string).toISOString(),
      EnrollmentDeadline: fd.get("EnrollmentDeadline")
        ? new Date(fd.get("EnrollmentDeadline") as string).toISOString()
        : undefined,
      UnenrollmentDeadline: fd.get("UnenrollmentDeadline")
        ? new Date(fd.get("UnenrollmentDeadline") as string).toISOString()
        : undefined,
      EnrollOpenDate: fd.get("EnrollOpenDate")
        ? new Date(fd.get("EnrollOpenDate") as string).toISOString()
        : undefined,

      ShowInKoala: isBoard ? fd.get("ShowInKoala") === "on" : false,
      ShowOnWebsite: isBoard ? fd.get("ShowOnWebsite") === "on" : false,
      IsEnrollable: isBoard ? fd.get("IsEnrollable") === "on" : false,
      AreParticipantsVisible: fd.get("AreParticipantsVisible") === "on",
      IsAdultOnly: fd.get("IsAdultOnly") === "on",
      IsWeeklyDrinks: fd.get("IsWeeklyDrinks") === "on",

      AllowedAudience: audienceMap[audienceFlags],

      VatRate: isBoard
        ? fd.get("VatRate")
          ? Number(fd.get("VatRate"))
          : undefined
        : undefined,
      GLAccountId: isBoard
        ? (fd.get("GLAccountId") as string) || undefined
        : undefined,
      CostCenterId: isBoard
        ? (fd.get("CostCenterId") as string) || undefined
        : undefined,

      Poster:
        (fd.get("Poster") as File)?.size > 0
          ? (fd.get("Poster") as File)
          : undefined,

      SpecificationQuestionsJson: JSON.stringify(questions),

      PaymentDeadline: isBoard
        ? fd.get("PaymentDeadline")
          ? new Date(fd.get("PaymentDeadline") as string).toISOString()
          : undefined
        : undefined,
    },
  };

  const submitProcess = async () => {
    try {
      const redirectPathBase = `${pathname.startsWith("/admin") ? "/admin" : ""}/activities/`;
      if (isEdit) {
        const response = await putActivitiesById({
          path: { id: Number(id) },
          ...payload,
        });
        if (response.error) {
          throw response.error ?? new Error("Failed to update activity");
        }
        navigate(`${redirectPathBase}${id}`);
      } else {
        const response = await postActivities(payload);
        if (response.error || !response.data?.id) {
          throw response.error ?? new Error("Failed to create activity");
        }
        navigate(`${redirectPathBase}${response.data?.id}`);
      }
    } catch (error) {
      console.error(error);
      throw error;
    } finally {
      setSaving(false);
    }
  };

  toast.promise(submitProcess(), {
    loading: isEdit ? t("saving") : t("creating"),
    success: isEdit ? t("activity_updated") : t("activity_created"),
    error: (error) =>
      appendErrorMessage(
        isEdit ? t("activity_update_failed") : t("activity_creation_failed"),
        error,
      ),
  });
};
