type DateFormatType =
  | "fullDateTime"
  | "shortDate"
  | "monthShort"
  | "timeOnly"
  | "defaultDate";

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

export const getAssociationYear = (): number => {
  const nlDateString = new Date().toLocaleString("en-US", { timeZone: "Europe/Amsterdam" });
  const now = new Date(nlDateString);
  
  const year = now.getFullYear();
  const month = now.getMonth();

  return month >= 7 ? year + 1 : year;
};