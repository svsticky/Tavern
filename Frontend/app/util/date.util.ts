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
  switch (format) {
    case "fullDateTime":
      return date.toLocaleDateString("default", {
        day: "numeric",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    case "shortDate":
      return date.toLocaleDateString("default", {
        day: "numeric",
        month: "short",
      });
    case "monthShort":
      return date.toLocaleDateString("default", {
        month: "short",
      });
    case "timeOnly":
      return date.toLocaleTimeString("default", {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      });
    default:
      return date.toLocaleDateString();
  }
}

let globalFinancialYearStartDate: string | null = null;
let globalBoardChangeDate: string | null = null;

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
 * Sets the global board change date.
 * @param dateStr The date string in MM-DD format.
 */
export const setGlobalBoardChangeDate = (dateStr: string) => {
  globalBoardChangeDate = dateStr;
};

/**
 * Gets the global board change date.
 */
export const getGlobalBoardChangeDate = (): string | null => {
  return globalBoardChangeDate;
};

/**
 * Determines the current board year based on the current month and day.
 * @returns The current board year.
 */
export const getBoardYear = (boardChangeDate?: string | null): number => {
  const startDate = boardChangeDate || globalBoardChangeDate || "08-01";
  let targetMonth = 8;
  let targetDay = 1;
  const parts = startDate.split("-");
  if (parts.length === 2) {
    const m = parseInt(parts[0], 10);
    const d = parseInt(parts[1], 10);
    if (!Number.isNaN(m) && !Number.isNaN(d)) {
      targetMonth = m;
      targetDay = d;
    }
  }

  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: "Europe/Amsterdam",
      year: "numeric",
      month: "numeric",
      day: "numeric",
    });
    const parts = formatter.formatToParts(new Date());
    const yearPart = parts.find((p) => p.type === "year")?.value;
    const monthPart = parts.find((p) => p.type === "month")?.value;
    const dayPart = parts.find((p) => p.type === "day")?.value;

    if (yearPart && monthPart && dayPart) {
      const year = parseInt(yearPart, 10);
      const month = parseInt(monthPart, 10); // 1-indexed
      const day = parseInt(dayPart, 10);

      const isAfterOrEqual =
        month > targetMonth || (month === targetMonth && day >= targetDay);
      return isAfterOrEqual ? year + 1 : year;
    }
  } catch (error) {
    console.error(
      "Failed to format date with Europe/Amsterdam timezone",
      error,
    );
  }

  // Fallback to local time if formatting fails
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth() + 1; // 1-indexed
  const day = now.getDate();
  const isAfterOrEqual =
    month > targetMonth || (month === targetMonth && day >= targetDay);
  return isAfterOrEqual ? year + 1 : year;
};

/**
 * Determines the current financial year based on the current month and day.
 * @returns The current financial year.
 */
export const getFinancialYear = (
  financialYearStartDate?: string | null,
): number => {
  const startDate =
    financialYearStartDate || globalFinancialYearStartDate || "08-01";
  let targetMonth = 8;
  let targetDay = 1;
  const parts = startDate.split("-");
  if (parts.length === 2) {
    const m = parseInt(parts[0], 10);
    const d = parseInt(parts[1], 10);
    if (!Number.isNaN(m) && !Number.isNaN(d)) {
      targetMonth = m;
      targetDay = d;
    }
  }

  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: "Europe/Amsterdam",
      year: "numeric",
      month: "numeric",
      day: "numeric",
    });
    const parts = formatter.formatToParts(new Date());
    const yearPart = parts.find((p) => p.type === "year")?.value;
    const monthPart = parts.find((p) => p.type === "month")?.value;
    const dayPart = parts.find((p) => p.type === "day")?.value;

    if (yearPart && monthPart && dayPart) {
      const year = parseInt(yearPart, 10);
      const month = parseInt(monthPart, 10); // 1-indexed
      const day = parseInt(dayPart, 10);

      const isAfterOrEqual =
        month > targetMonth || (month === targetMonth && day >= targetDay);
      return isAfterOrEqual ? year + 1 : year;
    }
  } catch (error) {
    console.error(
      "Failed to format date with Europe/Amsterdam timezone",
      error,
    );
  }

  // Fallback to local time if formatting fails
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth() + 1; // 1-indexed
  const day = now.getDate();
  const isAfterOrEqual =
    month > targetMonth || (month === targetMonth && day >= targetDay);
  return month <= 6
    ? isAfterOrEqual
      ? year
      : year - 1
    : isAfterOrEqual
      ? year + 1
      : year;
};

/**
 * Checks if the current date is within three months before the board change date.
 * @param boardChangeDate The board change date string in MM-DD format.
 * @returns True if within three months before the board change, false otherwise.
 */
export const isWithinThreeMonthsBeforeBoardChange = (
  boardChangeDate?: string | null,
): boolean => {
  const startDate = boardChangeDate || globalBoardChangeDate || "08-01";
  let targetMonth = 8;
  let targetDay = 1;
  const parts = startDate.split("-");
  if (parts.length === 2) {
    const m = parseInt(parts[0], 10);
    const d = parseInt(parts[1], 10);
    if (!Number.isNaN(m) && !Number.isNaN(d)) {
      targetMonth = m;
      targetDay = d;
    }
  }

  let year = new Date().getFullYear();
  let month = new Date().getMonth() + 1;
  let day = new Date().getDate();

  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: "Europe/Amsterdam",
      year: "numeric",
      month: "numeric",
      day: "numeric",
    });
    const parts = formatter.formatToParts(new Date());
    const yearPart = parts.find((p) => p.type === "year")?.value;
    const monthPart = parts.find((p) => p.type === "month")?.value;
    const dayPart = parts.find((p) => p.type === "day")?.value;

    if (yearPart && monthPart && dayPart) {
      year = parseInt(yearPart, 10);
      month = parseInt(monthPart, 10);
      day = parseInt(dayPart, 10);
    }
  } catch (error) {
    console.error(
      "Failed to format date with Europe/Amsterdam timezone",
      error,
    );
  }

  const nowNL = new Date(year, month - 1, day);
  let boardChangeNL = new Date(year, targetMonth - 1, targetDay);

  if (nowNL >= boardChangeNL) {
    boardChangeNL = new Date(year + 1, targetMonth - 1, targetDay);
  }

  const startWindow = new Date(boardChangeNL);
  startWindow.setMonth(startWindow.getMonth() - 3);

  return nowNL >= startWindow && nowNL < boardChangeNL;
};
