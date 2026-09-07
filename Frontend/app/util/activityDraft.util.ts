import type {
  GetSpecificationQuestionResponseDto,
  TargetAudience,
} from "~/api";
import { getAudienceString } from "~/types/AudienceMap";

export interface ActivityDraft {
  name?: string;
  location?: string;
  dutchDescription?: string;
  englishDescription?: string;
  dateTimeStart?: string;
  dateTimeEnd?: string;
  enrollmentDeadline?: string;
  unenrollmentDeadline?: string;
  enrollOpenDate?: string;
  isWeeklyDrinks?: boolean;
  allowedAudience?: TargetAudience;
  organizerId?: string;
  price?: string;
  participantLimit?: string;
  vatRate?: string;
  glAccountId?: string;
  costUnitId?: string;
  costCenterId?: string;
  paymentDeadline?: string;
  isOpenForPayment?: boolean;
  isEnrollable?: boolean;
  showInKoala?: boolean;
  showOnWebsite?: boolean;
  areParticipantsVisible?: boolean;
  isAdultOnly?: boolean;
  specificationQuestions?: Partial<GetSpecificationQuestionResponseDto>[];
  savedAt?: string;
}

export const ACTIVITY_CREATE_DRAFT_KEY = "tavern_activity_create_draft";

/**
 * Checks whether an activity draft contains any non-empty user input.
 */
export function isDraftNotEmpty(draft: ActivityDraft | null): boolean {
  if (!draft) return false;
  return Boolean(
    draft.name?.trim() ||
      draft.location?.trim() ||
      draft.dutchDescription?.trim() ||
      draft.englishDescription?.trim() ||
      draft.dateTimeStart ||
      draft.dateTimeEnd ||
      draft.enrollmentDeadline ||
      draft.unenrollmentDeadline ||
      draft.enrollOpenDate ||
      draft.organizerId ||
      draft.price ||
      draft.participantLimit ||
      draft.vatRate?.trim() ||
      draft.glAccountId?.trim() ||
      draft.costUnitId?.trim() ||
      draft.costCenterId?.trim() ||
      draft.paymentDeadline ||
      (draft.specificationQuestions && draft.specificationQuestions.length > 0),
  );
}

/**
 * Checks whether an activity draft has all required fields populated to allow submission.
 */
export function isActivityDraftValid(draft: ActivityDraft | null): boolean {
  if (!draft) return false;
  return Boolean(
    draft.name?.trim() &&
      draft.dateTimeStart &&
      draft.dateTimeEnd &&
      draft.location?.trim() &&
      draft.organizerId &&
      draft.dutchDescription?.trim() &&
      draft.englishDescription?.trim(),
  );
}

/**
 * Extracts form field values and custom specification questions into an ActivityDraft object.
 */
export function extractDraftFromForm(
  form: HTMLFormElement,
  questions: Partial<GetSpecificationQuestionResponseDto>[],
): ActivityDraft {
  const fd = new FormData(form);
  const audienceFlags = fd
    .getAll("AudienceBit")
    .reduce((acc, val) => acc + Number(val), 0);

  return {
    name: (fd.get("Name") as string) || "",
    location: (fd.get("Location") as string) || "",
    dutchDescription: (fd.get("DutchDescription") as string) || "",
    englishDescription: (fd.get("EnglishDescription") as string) || "",
    dateTimeStart: (fd.get("DateTimeStart") as string) || "",
    dateTimeEnd: (fd.get("DateTimeEnd") as string) || "",
    enrollmentDeadline: (fd.get("EnrollmentDeadline") as string) || "",
    unenrollmentDeadline: (fd.get("UnenrollmentDeadline") as string) || "",
    enrollOpenDate: (fd.get("EnrollOpenDate") as string) || "",
    isWeeklyDrinks: fd.get("IsWeeklyDrinks") === "on",
    allowedAudience: getAudienceString(audienceFlags),
    organizerId: (fd.get("OrganizerId") as string) || "",
    price: (fd.get("Price") as string) || "",
    participantLimit: (fd.get("ParticipantLimit") as string) || "",
    vatRate: (fd.get("VatRate") as string) || "",
    glAccountId: (fd.get("GLAccountId") as string) || "",
    costUnitId: (fd.get("CostUnitId") as string) || "",
    costCenterId: (fd.get("CostCenterId") as string) || "",
    paymentDeadline: (fd.get("PaymentDeadline") as string) || "",
    isOpenForPayment: fd.get("IsOpenForPayment") === "on",
    isEnrollable: fd.get("IsEnrollable") === "on",
    showInKoala: fd.get("ShowInKoala") === "on",
    showOnWebsite: fd.get("ShowOnWebsite") === "on",
    areParticipantsVisible: fd.get("AreParticipantsVisible") === "on",
    isAdultOnly: fd.get("IsAdultOnly") === "on",
    specificationQuestions: questions,
    savedAt: new Date().toISOString(),
  };
}

/**
 * Loads the saved activity creation draft from localStorage.
 */
export function loadActivityDraft(): ActivityDraft | null {
  if (typeof window === "undefined" || !window.localStorage) {
    return null;
  }
  try {
    const raw = localStorage.getItem(ACTIVITY_CREATE_DRAFT_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (parsed && typeof parsed === "object" && isDraftNotEmpty(parsed)) {
      return parsed as ActivityDraft;
    }
    return null;
  } catch {
    return null;
  }
}

/**
 * Saves the given activity creation draft to localStorage.
 */
export function saveActivityDraft(draft: ActivityDraft): void {
  if (typeof window === "undefined" || !window.localStorage) {
    return;
  }
  try {
    localStorage.setItem(ACTIVITY_CREATE_DRAFT_KEY, JSON.stringify(draft));
  } catch (err) {
    console.error("Failed to save activity draft to localStorage:", err);
  }
}

/**
 * Clears any saved activity creation draft from localStorage.
 */
export function clearActivityDraft(): void {
  if (typeof window === "undefined" || !window.localStorage) {
    return;
  }
  try {
    localStorage.removeItem(ACTIVITY_CREATE_DRAFT_KEY);
  } catch (err) {
    console.error("Failed to clear activity draft from localStorage:", err);
  }
}
