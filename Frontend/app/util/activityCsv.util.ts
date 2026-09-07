import type { ActivityResponseDto } from "~/api";

/**
 * Escapes a field for CSV export according to RFC 4180.
 * Wraps fields containing semicolons, quotes, or newlines in double quotes.
 */
export function escapeCsvField(value: unknown): string {
  if (value === null || value === undefined) return "";
  let str = String(value);
  // Mitigate CSV Formula Injection (OWASP) by neutralizing formula trigger characters
  if (/^[=+\-@\t]/.test(str)) {
    str = `'${str}`;
  }
  if (
    str.includes(";") ||
    str.includes('"') ||
    str.includes("\n") ||
    str.includes("\r")
  ) {
    return `"${str.replace(/"/g, '""')}"`;
  }
  return str;
}

/**
 * Generates RFC 4180 compliant CSV content (semicolon delimited with UTF-8 BOM)
 * containing all participants and waiting list members along with their specification answers.
 */
export function generateActivityEnrollmentsCsv(
  activity: ActivityResponseDto,
  isDutch: boolean = true,
): string {
  const questions = (activity.specificationQuestions || [])
    .slice()
    .sort((a, b) => a.id - b.id);

  const header = [
    isDutch ? "Voornaam" : "First Name",
    isDutch ? "Achternaam" : "Last Name",
    isDutch ? "E-mail" : "Email",
    isDutch ? "Status" : "Status",
    ...questions.map((q) => (isDutch ? q.questionDutch : q.questionEnglish)),
  ];

  const rows: string[][] = [];

  // Sort enrollments: participants first, then waiting list
  const enrollments = (activity.enrollments || []).slice().sort((a, b) => {
    if (a.isOnWaitingList !== b.isOnWaitingList) {
      return a.isOnWaitingList ? 1 : -1;
    }
    return 0;
  });

  for (const enrollment of enrollments) {
    const status = enrollment.isOnWaitingList
      ? isDutch
        ? "Wachtlijst"
        : "Waiting List"
      : isDutch
        ? "Deelnemer"
        : "Participant";

    const answersMap = new Map<number, string>();
    for (const ans of enrollment.specificationAnswers || []) {
      answersMap.set(ans.questionId, ans.answer);
    }

    const row = [
      enrollment.member?.firstName ?? "",
      enrollment.member?.lastName ?? "",
      enrollment.member?.email ?? "",
      status,
      ...questions.map((q) => answersMap.get(q.id) ?? ""),
    ];

    rows.push(row);
  }

  const csvLines = [
    header.map(escapeCsvField).join(";"),
    ...rows.map((row) => row.map(escapeCsvField).join(";")),
  ];

  // Prepend UTF-8 Byte Order Mark (BOM) so Microsoft Excel opens special characters correctly
  return `\uFEFF${csvLines.join("\r\n")}`;
}

/**
 * Triggers a browser download of the activity enrollments CSV.
 */
export function downloadActivityEnrollmentsCsv(
  activity: ActivityResponseDto,
  isDutch: boolean = true,
) {
  const csvContent = generateActivityEnrollmentsCsv(activity, isDutch);
  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);

  const safeName = (activity.name || "activity")
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, "_")
    .replace(/^_+|_+$/g, "");

  const a = document.createElement("a");
  a.href = url;
  a.download = `deelnemers-${safeName}.csv`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
