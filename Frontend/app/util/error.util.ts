import { t } from "i18next";

type ErrorDetails = Record<string, unknown>;

// Exact backend messages (see Backend/Utils/MemberErrorMessages.cs) mapped to a
// translated, user-friendly toast message instead of the raw backend text.
const KNOWN_ERROR_TRANSLATION_KEYS: Record<string, string> = {
  "An account with this email address already exists.":
    "email_already_registered",
  "An account with this student number already exists.":
    "student_number_already_registered",
};

const getValidationErrorMessage = (errors: unknown): string | undefined => {
  if (!errors || typeof errors !== "object") return undefined;
  for (const [key, value] of Object.entries(errors as ErrorDetails)) {
    if (Array.isArray(value)) {
      const message = value.find(
        (entry) => typeof entry === "string" && entry.trim(),
      );
      if (message) return key ? `${key}: ${message}` : message;
      continue;
    }
    if (typeof value === "string" && value.trim()) {
      return key ? `${key}: ${value.trim()}` : value.trim();
    }
  }
  return undefined;
};

const getStringValue = (value: unknown): string | undefined => {
  if (typeof value !== "string") return undefined;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : undefined;
};

export const getErrorMessage = (error: unknown): string | undefined => {
  if (typeof error === "string") return getStringValue(error);
  if (error instanceof Error) return getStringValue(error.message);
  if (!error || typeof error !== "object") return undefined;

  const details = error as ErrorDetails;
  return (
    getValidationErrorMessage(details.errors) ??
    getStringValue(details.detail) ??
    getStringValue(details.title) ??
    getStringValue(details.message) ??
    getStringValue(details.error)
  );
};

export const appendErrorMessage = (
  baseMessage: string,
  error?: unknown,
): string => {
  const errorMessage = getErrorMessage(error);
  return errorMessage ? `${baseMessage}: ${errorMessage}` : baseMessage;
};

/**
 * Like {@link appendErrorMessage}, but swaps known backend error messages for a
 * translated, user-friendly message instead of appending the raw backend text.
 */
export const getFriendlyErrorMessage = (
  baseMessage: string,
  error?: unknown,
): string => {
  const errorMessage = getErrorMessage(error);
  const translationKey = errorMessage
    ? KNOWN_ERROR_TRANSLATION_KEYS[errorMessage]
    : undefined;
  return translationKey
    ? t(translationKey)
    : appendErrorMessage(baseMessage, error);
};
