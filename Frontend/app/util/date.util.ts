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

/**
 * Determines the current association year based on the current month.
 * @returns The current association year.
 */
export const getAssociationYear = (): number => {
  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: "Europe/Amsterdam",
      year: "numeric",
      month: "numeric",
    });
    const parts = formatter.formatToParts(new Date());
    const yearPart = parts.find((p) => p.type === "year")?.value;
    const monthPart = parts.find((p) => p.type === "month")?.value;

    if (yearPart && monthPart) {
      const year = parseInt(yearPart, 10);
      const month = parseInt(monthPart, 10) - 1; // Convert 1-indexed to 0-indexed
      return month >= 7 ? year + 1 : year;
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
  const month = now.getMonth();
  return month >= 7 ? year + 1 : year;
};
