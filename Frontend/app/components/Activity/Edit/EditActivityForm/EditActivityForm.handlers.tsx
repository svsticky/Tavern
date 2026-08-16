import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  type GetSpecificationQuestionResponseDto,
  type GroupResponseDto,
  getGroups,
  patchActivitiesById,
  postActivities,
  postActivitiesByIdPoster,
} from "~/api";
import { getAudienceString } from "~/types/AudienceMap";
import { getCommitteeYear } from "~/util/date.util";
import { appendErrorMessage } from "~/util/error.util";

export { formatDateOnly, formatForInput } from "~/util/date.util";

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
      query: { IncludeInactive: false, MembershipYear: getCommitteeYear() },
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

  const dateTimeStart = new Date(fd.get("DateTimeStart") as string);
  const dateTimeEnd = new Date(fd.get("DateTimeEnd") as string);
  if (dateTimeEnd < dateTimeStart) {
    toast.error(t("activity_end_before_start"));
    return;
  }

  const enrollmentDeadline = fd.get("EnrollmentDeadline") as string;
  if (enrollmentDeadline && new Date(enrollmentDeadline) > dateTimeEnd) {
    toast.error(t("activity_enrollment_deadline_after_end"));
    return;
  }

  const unenrollmentDeadline = fd.get("UnenrollmentDeadline") as string;
  if (unenrollmentDeadline && new Date(unenrollmentDeadline) > dateTimeEnd) {
    toast.error(t("activity_unenrollment_deadline_after_end"));
    return;
  }

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

      AllowedAudience: getAudienceString(audienceFlags),

      VatRate: isBoard
        ? fd.get("VatRate")
          ? Number(fd.get("VatRate"))
          : undefined
        : undefined,
      GLAccountId: isBoard
        ? (fd.get("GLAccountId") as string) || undefined
        : undefined,
      CostUnitId: isBoard
        ? (fd.get("CostUnitId") as string) || undefined
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
      IsOpenForPayment: isBoard
        ? fd.get("IsOpenForPayment") === "on"
        : undefined,
    },
  };

  const submitProcess = async () => {
    try {
      const redirectPathBase = `${pathname.startsWith("/admin") ? "/admin" : ""}/activities/`;
      if (isEdit) {
        const patchOperations: any[] = [
          { op: "replace", path: "/Name", value: fd.get("Name") as string },
          {
            op: "replace",
            path: "/Location",
            value: fd.get("Location") as string,
          },
          {
            op: "replace",
            path: "/DutchDescription",
            value: fd.get("DutchDescription") as string,
          },
          {
            op: "replace",
            path: "/EnglishDescription",
            value: fd.get("EnglishDescription") as string,
          },
          {
            op: "replace",
            path: "/Price",
            value: Number(fd.get("Price")) || 0,
          },
          {
            op: "replace",
            path: "/ParticipantLimit",
            value: fd.get("ParticipantLimit")
              ? Number(fd.get("ParticipantLimit"))
              : null,
          },
          {
            op: "replace",
            path: "/OrganizerId",
            value: fd.get("OrganizerId") ? Number(fd.get("OrganizerId")) : null,
          },
          {
            op: "replace",
            path: "/DateTimeStart",
            value: new Date(fd.get("DateTimeStart") as string).toISOString(),
          },
          {
            op: "replace",
            path: "/DateTimeEnd",
            value: new Date(fd.get("DateTimeEnd") as string).toISOString(),
          },
          {
            op: "replace",
            path: "/EnrollmentDeadline",
            value: fd.get("EnrollmentDeadline")
              ? new Date(fd.get("EnrollmentDeadline") as string).toISOString()
              : null,
          },
          {
            op: "replace",
            path: "/UnenrollmentDeadline",
            value: fd.get("UnenrollmentDeadline")
              ? new Date(fd.get("UnenrollmentDeadline") as string).toISOString()
              : null,
          },
          {
            op: "replace",
            path: "/EnrollOpenDate",
            value: fd.get("EnrollOpenDate")
              ? new Date(fd.get("EnrollOpenDate") as string).toISOString()
              : null,
          },
          {
            op: "replace",
            path: "/AreParticipantsVisible",
            value: fd.get("AreParticipantsVisible") === "on",
          },
          {
            op: "replace",
            path: "/IsAdultOnly",
            value: fd.get("IsAdultOnly") === "on",
          },
          {
            op: "replace",
            path: "/IsWeeklyDrinks",
            value: fd.get("IsWeeklyDrinks") === "on",
          },
          {
            op: "replace",
            path: "/AllowedAudience",
            value: getAudienceString(audienceFlags),
          },
          {
            op: "replace",
            path: "/SpecificationQuestionsJson",
            value: JSON.stringify(questions),
          },
        ];

        if (isBoard) {
          patchOperations.push(
            {
              op: "replace",
              path: "/ShowInKoala",
              value: fd.get("ShowInKoala") === "on",
            },
            {
              op: "replace",
              path: "/ShowOnWebsite",
              value: fd.get("ShowOnWebsite") === "on",
            },
            {
              op: "replace",
              path: "/IsEnrollable",
              value: fd.get("IsEnrollable") === "on",
            },
            {
              op: "replace",
              path: "/VatRate",
              value: fd.get("VatRate") ? Number(fd.get("VatRate")) : null,
            },
            {
              op: "replace",
              path: "/GLAccountId",
              value: (fd.get("GLAccountId") as string) || null,
            },
            {
              op: "replace",
              path: "/CostUnitId",
              value: (fd.get("CostUnitId") as string) || null,
            },
            {
              op: "replace",
              path: "/CostCenterId",
              value: (fd.get("CostCenterId") as string) || null,
            },
            {
              op: "replace",
              path: "/PaymentDeadline",
              value: fd.get("PaymentDeadline")
                ? new Date(fd.get("PaymentDeadline") as string).toISOString()
                : null,
            },
            {
              op: "replace",
              path: "/IsOpenForPayment",
              value: fd.get("IsOpenForPayment") === "on",
            },
          );
        }

        const response = await patchActivitiesById({
          path: { id: Number(id) },
          body: patchOperations,
        });
        if (response.error) {
          throw response.error ?? new Error("Failed to update activity");
        }

        const posterFile = fd.get("Poster") as File;
        if (posterFile && posterFile.size > 0) {
          const posterResponse = await postActivitiesByIdPoster({
            path: { id: Number(id) },
            body: {
              poster: posterFile,
            },
          });
          if (posterResponse.error) {
            throw posterResponse.error ?? new Error("Failed to upload poster");
          }
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
