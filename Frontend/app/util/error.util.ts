type ErrorDetails = Record<string, unknown>;

const getValidationErrorMessage = (errors: unknown): string | undefined => {
  if (!errors || typeof errors !== "object") return undefined;
  for (const [key, value] of Object.entries(errors as ErrorDetails)) {
    if (Array.isArray(value)) {
      const message = value.find((entry) => typeof entry === "string" && entry.trim());
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
