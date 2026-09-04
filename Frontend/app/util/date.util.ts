import { getEnv } from "./config.utils";

type DateFormatType =
  | "fullDateTime"
  | "shortDate"
  | "shortDateWithWeekday"
  | "monthShort"
  | "timeOnly"
  | "dateOnly"
  | "weekdayDate"
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
  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";

  switch (format) {
    case "weekdayDate": {
      return date.toLocaleDateString(deviceLocale, {
        timeZone,
        weekday: "long",
        day: "numeric",
        month: "long",
      });
    }
    case "dateOnly":
      return date.toLocaleDateString(deviceLocale, {
        timeZone: timeZone,
        day: "numeric",
        month: "long",
        year: "numeric",
      });
    case "fullDateTime":
      return date.toLocaleDateString(deviceLocale, {
        timeZone: timeZone,
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    case "shortDate":
      return date.toLocaleDateString(deviceLocale, {
        timeZone: timeZone,
        day: "numeric",
        month: "short",
      });
    case "shortDateWithWeekday":
      return date.toLocaleDateString(deviceLocale, {
        timeZone,
        weekday: "short",
        day: "numeric",
        month: "short",
      });
    case "monthShort":
      return date.toLocaleDateString(deviceLocale, {
        timeZone: timeZone,
        month: "short",
      });
    case "timeOnly":
      return date.toLocaleTimeString(deviceLocale, {
        timeZone: timeZone,
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    default:
      return date.toLocaleDateString(deviceLocale);
  }
}

/**
 * Reads the year/month/day/hour/minute/second that a given instant renders as
 * in a specific timezone, via `Intl.DateTimeFormat` (no external tz library needed).
 */
function getDateTimePartsInTimeZone(date: Date, timeZone: string) {
  const formatter = new Intl.DateTimeFormat("en-US", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hourCycle: "h23",
  });
  const parts = formatter.formatToParts(date);
  const get = (type: string) =>
    Number(parts.find((p) => p.type === type)?.value ?? 0);

  return {
    year: get("year"),
    month: get("month"),
    day: get("day"),
    hour: get("hour"),
    minute: get("minute"),
    second: get("second"),
  };
}

/**
 * Checks whether two instants fall on the same calendar day in the association's configured
 * timezone. Unlike comparing `Date#toDateString()` directly, this doesn't depend on the
 * viewer's own browser timezone, so it agrees with what `formatDate` actually renders.
 *
 * @param a - The first instant.
 * @param b - The second instant.
 * @returns Whether `a` and `b` render as the same day in the association's timezone.
 */
export function isSameDayInAssociationTimeZone(a: Date, b: Date): boolean {
  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const partsA = getDateTimePartsInTimeZone(a, timeZone);
  const partsB = getDateTimePartsInTimeZone(b, timeZone);

  return (
    partsA.year === partsB.year &&
    partsA.month === partsB.month &&
    partsA.day === partsB.day
  );
}

/**
 * Formats an ISO date string for HTML `datetime-local` inputs (YYYY-MM-DDTHH:mm),
 * rendered in the association's configured timezone so it matches what `formatDate` displays.
 *
 * @param isoString - The ISO date string (e.g. from the API in UTC).
 * @returns A string in the format "YYYY-MM-DDTHH:mm".
 */
export function formatForInput(isoString?: string): string {
  if (!isoString) return "";
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return "";

  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const { year, month, day, hour, minute } = getDateTimePartsInTimeZone(
    date,
    timeZone,
  );
  const pad = (num: number) => String(num).padStart(2, "0");

  return `${year}-${pad(month)}-${pad(day)}T${pad(hour)}:${pad(minute)}`;
}

/**
 * Formats an ISO date string for HTML `date` inputs (YYYY-MM-DD), rendered in the
 * association's configured timezone so it matches what `formatDate` displays.
 *
 * @param isoString - The ISO date string (e.g. from the API in UTC).
 * @returns A string in the format "YYYY-MM-DD".
 */
export function formatDateOnly(isoString?: string): string {
  if (!isoString) return "";
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return "";

  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const { year, month, day } = getDateTimePartsInTimeZone(date, timeZone);
  const pad = (num: number) => String(num).padStart(2, "0");

  return `${year}-${pad(month)}-${pad(day)}`;
}

/**
 * Converts the value of an HTML `date` or `datetime-local` input back into a UTC
 * instant, interpreting the wall-clock value as being in the association's configured
 * timezone. This is the inverse of `formatForInput`/`formatDateOnly`.
 *
 * @param value - The raw input value, e.g. "2026-08-01T10:00" or "2026-08-01".
 * @returns The corresponding Date, or an invalid Date if `value` is empty/malformed.
 */
export function parseInputAsAssociationTime(value?: string): Date {
  if (!value) return new Date(Number.NaN);

  const [datePart, timePart] = value.split("T");
  const [year, month, day] = (datePart ?? "").split("-").map(Number);
  const [hour, minute] = (timePart ?? "00:00").split(":").map(Number);
  if ([year, month, day, hour, minute].some(Number.isNaN)) {
    return new Date(Number.NaN);
  }

  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";

  // Treat the wall-clock value as if it were UTC, then measure how far that guess
  // drifts once rendered in the target timezone, and correct the guess by that drift.
  const guess = Date.UTC(year, month - 1, day, hour, minute);
  const rendered = getDateTimePartsInTimeZone(new Date(guess), timeZone);
  const renderedAsUtc = Date.UTC(
    rendered.year,
    rendered.month - 1,
    rendered.day,
    rendered.hour,
    rendered.minute,
    rendered.second,
  );

  return new Date(guess - (renderedAsUtc - guess));
}

/**
 * Calculates the age in whole years for a given birth date, based on what "today" and the
 * birth date render as in the association's configured timezone (so the result doesn't
 * depend on the browser's own timezone).
 *
 * @param birthDateString - The birth date, e.g. "1990-05-15" or a full ISO string.
 * @returns The age in whole years, or null if `birthDateString` is empty/malformed.
 */
export function calculateAge(birthDateString?: string): number | null {
  if (!birthDateString) return null;
  const birthDate = new Date(birthDateString);
  if (Number.isNaN(birthDate.getTime())) return null;

  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const today = getDateTimePartsInTimeZone(new Date(), timeZone);
  const birth = getDateTimePartsInTimeZone(birthDate, timeZone);

  let age = today.year - birth.year;
  const monthDiff = today.month - birth.month;
  if (monthDiff < 0 || (monthDiff === 0 && today.day < birth.day)) {
    age--;
  }

  return age;
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
      timeZone: getEnv("AssociationTimeZone") || "Europe/Amsterdam",
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
