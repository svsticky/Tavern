import type React from "react";
import type { NavigateFunction } from "react-router";
import { t } from "i18next";
import toast from "react-hot-toast";
import {
  getApiGroups,
  postApiActivities,
  putApiActivitiesById,
  type GetSpecificationQuestionResponseDto,
  type GroupResponseDto
} from "~/api";
import { audienceMap } from "~/types/AudienceMap";
import { getAssociationYear } from "~/util/date.util";

export const formatForInput = (isoString?: string) => (isoString ? isoString.substring(0, 16) : "");
export const formatDateOnly = (isoString?: string) => (isoString ? isoString.substring(0, 10) : "");

export const loadGroups = async (
  setLoading: (loading: boolean) => void,
  setGroups: (groups: GroupResponseDto[]) => void
) => {
  try {
    const groupsRes = await getApiGroups({
      query: { IncludeInactive: false, MembershipYear: getAssociationYear() }
    });
    if (groupsRes.data) setGroups(groupsRes.data);
  } catch (error) {
    console.error("Error loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

export const handleActivityFormChange = (e: React.FormEvent<HTMLFormElement>, setFormValid: (valid: boolean) => void) => {
  const fd = new FormData(e.currentTarget);
  const required = ["Name", "DateTimeStart", "DateTimeEnd", "Location", "OrganizerId"];
  setFormValid(required.every((field) => !!fd.get(field)));
};

export const addQuestion = (questions: Partial<GetSpecificationQuestionResponseDto>[], setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void) => {
  setQuestions([
    ...questions,
    {
      questionDutch: "",
      questionEnglish: "",
      type: "String",
      isMandatory: false,
      isPublic: true,
      options: []
    }
  ]);
};

export const removeQuestion = (
  index: number,
  questions: Partial<GetSpecificationQuestionResponseDto>[],
  setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void
) => {
  setQuestions(questions.filter((_, i) => i !== index));
};

export const updateQuestion = (
  index: number,
  field: keyof GetSpecificationQuestionResponseDto,
  value: any,
  questions: Partial<GetSpecificationQuestionResponseDto>[],
  setQuestions: (value: Partial<GetSpecificationQuestionResponseDto>[]) => void
) => {
  const newQuestions = [...questions];
  newQuestions[index] = { ...newQuestions[index], [field]: value };
  setQuestions(newQuestions);
};

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

export const handleActivitySubmit = async ({
  e,
  isBoard,
  questions,
  setSaving,
  isEdit,
  id,
  pathname,
  navigate
}: HandleActivitySubmitArgs) => {
  e.preventDefault();

  const fd = new FormData(e.currentTarget);
  const audienceFlags = fd.getAll("AudienceBit").reduce((acc, val) => acc + Number(val), 0);

  setSaving(true);

  const payload = {
    body: {
      Name: fd.get("Name") as string,
      Location: fd.get("Location") as string,
      DutchDescription: fd.get("DutchDescription") as string,
      EnglishDescription: fd.get("EnglishDescription") as string,
      Price: Number(fd.get("Price")) || 0,
      ParticipantLimit: fd.get("ParticipantLimit") ? Number(fd.get("ParticipantLimit")) : undefined,
      OrganizerId: fd.get("OrganizerId") ? Number(fd.get("OrganizerId")) : undefined,
      DateTimeStart: new Date(fd.get("DateTimeStart") as string).toISOString(),
      DateTimeEnd: new Date(fd.get("DateTimeEnd") as string).toISOString(),
      EnrollmentDeadline: fd.get("EnrollmentDeadline") ? new Date(fd.get("EnrollmentDeadline") as string).toISOString() : undefined,
      UnenrollmentDeadline: fd.get("UnenrollmentDeadline") ? new Date(fd.get("UnenrollmentDeadline") as string).toISOString() : undefined,
      EnrollOpenDate: fd.get("EnrollOpenDate") ? new Date(fd.get("EnrollOpenDate") as string).toISOString() : undefined,

      ShowInKoala: isBoard ? fd.get("ShowInKoala") === "on" : false,
      ShowOnWebsite: isBoard ? fd.get("ShowOnWebsite") === "on" : false,
      IsEnrollable: isBoard ? fd.get("IsEnrollable") === "on" : false,
      AreParticipantsVisible: fd.get("AreParticipantsVisible") === "on",
      IsAdultOnly: fd.get("IsAdultOnly") === "on",
      IsWeeklyDrinks: fd.get("IsWeeklyDrinks") === "on",

      AllowedAudience: audienceMap[audienceFlags],

      VatRate: isBoard ? (fd.get("VatRate") ? Number(fd.get("VatRate")) : undefined) : undefined,
      GLAccountId: isBoard ? ((fd.get("GLAccountId") as string) || undefined) : undefined,
      CostCenterId: isBoard ? ((fd.get("CostCenterId") as string) || undefined) : undefined,

      Poster: (fd.get("Poster") as File)?.size > 0 ? (fd.get("Poster") as File) : undefined,

      SpecificationQuestionsJson: JSON.stringify(questions),

      PaymentDeadline: isBoard ? (fd.get("PaymentDeadline") ? new Date(fd.get("PaymentDeadline") as string).toISOString() : undefined) : undefined
    }
  };

  const submitProcess = async () => {
    try {
      const redirectPathBase = `${pathname.startsWith("/admin") ? "/admin" : ""}/activities/`;
      if (isEdit) {
        await putApiActivitiesById({ path: { id: Number(id) }, ...payload });
        navigate(`${redirectPathBase}${id}`);
      } else {
        const response = await postApiActivities(payload);
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
    error: isEdit ? t("activity_update_failed") : t("activity_creation_failed")
  });
};
