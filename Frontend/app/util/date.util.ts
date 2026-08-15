type DateFormatType =
  | "fullDateTime"
  | "shortDate"
  | "monthShort"
  | "timeOnly"
  | "defaultDate";

/**
 * Formats a date according to the specified format.
 * @param date The date to format.
 * @param format The desired format type.
 * @returns The formatted date string.
 */
export function formatDate(date: Date, format: DateFormatType): string {
  const deviceLocale =
    typeof navigator !== "undefined" ? navigator.language : undefined;

  switch (format) {
    case "fullDateTime":
      return date.toLocaleDateString(deviceLocale, {
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    case "shortDate":
      return date.toLocaleDateString(deviceLocale, {
        day: "numeric",
        month: "short",
      });
    case "monthShort":
      return date.toLocaleDateString(deviceLocale, {
        month: "short",
      });
    case "timeOnly":
      return date.toLocaleTimeString(deviceLocale, {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    default:
      return date.toLocaleDateString(deviceLocale);
  }
}

/**
 * Formats an ISO date string to a local format compatible with HTML `datetime-local` inputs (YYYY-MM-DDTHH:mm).
 *
 * @param isoString - The ISO date string (e.g. from the API in UTC).
 * @returns A string in the local time format "YYYY-MM-DDTHH:mm".
 */
export function formatForInput(isoString?: string): string {
  if (!isoString) return "";
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return "";

  const pad = (num: number) => String(num).padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

/**
 * Formats an ISO date string to a local date-only format compatible with HTML `date` inputs (YYYY-MM-DD).
 *
 * @param isoString - The ISO date string (e.g. from the API in UTC).
 * @returns A string in the local date format "YYYY-MM-DD".
 */
export function formatDateOnly(isoString?: string): string {
  if (!isoString) return "";
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return "";

  const pad = (num: number) => String(num).padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());

  return `${year}-${month}-${day}`;
}

let globalFinancialYearStartDate: string | null = null;
let globalCommitteeCreationDate: string | null = null;

/**
 * Sets the global financial year start date.
 * @param dateStr The date string in MM-DD format.
 */
export const setGlobalFinancialYearStartDate = (dateStr: string) => {
  globalFinancialYearStartDate = dateStr;
};

/**
 * Gets the global financial year start date.
 */
export const getGlobalFinancialYearStartDate = (): string | null => {
  return globalFinancialYearStartDate;
};

/**
 * Sets the global committee creation date.
 * @param dateStr The date string in MM-DD format.
 */
export const setGlobalCommitteeCreationDate = (dateStr: string) => {
  globalCommitteeCreationDate = dateStr;
};

/**
 * Gets the global committee creation date.
 */
export const getGlobalCommitteeCreationDate = (): string | null => {
  return globalCommitteeCreationDate;
};

/**
 * Determines the year for a given date based on a specified start date.
 * @param date The date to evaluate.
 * @param startDateStr The start date string in MM-DD format (optional).
 * @returns The determined year.
 */
const getYearForDate = (date: Date, startDateStr?: string | null): number => {
  let targetMonth = 8;
  let targetDay = 1;

  if (startDateStr) {
    const parts = startDateStr.split("-");
    if (parts.length === 2) {
      const m = parseInt(parts[0], 10);
      const d = parseInt(parts[1], 10);
      if (!Number.isNaN(m) && !Number.isNaN(d)) {
        targetMonth = m;
        targetDay = d;
      }
    }
  }

  let year: number;
  let month: number;
  let day: number;

  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: "Europe/Amsterdam",
      year: "numeric",
      month: "numeric",
      day: "numeric",
    });
    const parts = formatter.formatToParts(date);
    const yearPart = parts.find((p) => p.type === "year")?.value;
    const monthPart = parts.find((p) => p.type === "month")?.value;
    const dayPart = parts.find((p) => p.type === "day")?.value;

    if (yearPart && monthPart && dayPart) {
      year = parseInt(yearPart, 10);
      month = parseInt(monthPart, 10);
      day = parseInt(dayPart, 10);
    } else {
      throw new Error("Missing date parts");
    }
  } catch (error) {
    console.error(
      "Failed to format date with Europe/Amsterdam timezone",
      error,
    );
    year = date.getFullYear();
    month = date.getMonth() + 1;
    day = date.getDate();
  }

  const isAfterOrEqual =
    month > targetMonth || (month === targetMonth && day >= targetDay);

  return targetMonth <= 6
    ? isAfterOrEqual
      ? year
      : year - 1
    : isAfterOrEqual
      ? year + 1
      : year;
};

/**
 * Determines the current board year based on the current month and day.
 * @returns The current board year.
 */
export const getCommitteeYear = (
  committeeCreationDate?: string | null,
): number => {
  return getYearForDate(
    new Date(),
    committeeCreationDate || globalCommitteeCreationDate || "08-01",
  );
};

/**
 * Determines the current financial year based on the current month and day.
 * @returns The current financial year.
 */
export const getFinancialYear = (
  financialYearStartDate?: string | null,
): number => {
  return getYearForDate(
    new Date(),
    financialYearStartDate || globalFinancialYearStartDate || "08-01",
  );
};
