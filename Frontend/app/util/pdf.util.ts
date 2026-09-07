import { jsPDF } from "jspdf";
import type { ActivityResponseDto } from "~/api";

/**
 * Generates a PDF document in A3 format from an array of image URLs. Each image is added to a new page in the PDF,
 * and the resulting document is saved as "document-a3.pdf". The images are added with a "FAST" compression method for better performance.
 * @param images An array of image URLs to be included in the PDF document.
 * @returns A promise that resolves when the PDF has been generated and saved.
 */
export const generateA3Pdf = async (
  imageUrls: string[],
  token: string,
): Promise<void> => {
  const pdf = new jsPDF({
    orientation: "p",
    unit: "mm",
    format: "a3",
  });

  const width = pdf.internal.pageSize.getWidth();
  const height = pdf.internal.pageSize.getHeight();

  for (let i = 0; i < imageUrls.length; i++) {
    try {
      const response = await fetch(imageUrls[i], {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) continue;

      const blob = await response.blob();
      const base64Data = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result as string);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });

      if (i > 0) {
        pdf.addPage("a3", "p");
      }

      pdf.addImage(base64Data, "JPEG", 0, 0, width, height, undefined, "FAST");
    } catch (error) {
      console.error(error);
    }
  }

  pdf.save("a3.pdf");
};

function getMemberFullName(
  member?: { firstName?: string | null; lastName?: string | null } | null,
  fallback = "",
): string {
  if (!member) return fallback;
  const full = `${member.firstName || ""} ${member.lastName || ""}`.trim();
  return full || fallback;
}

/**
 * Generates a printable participant checklist in A4 portrait format for bar/door duties.
 * Includes checkboxes, participant names, dietary answers, waiting list status, and notes.
 *
 * @param {ActivityResponseDto} activity - The activity whose participants to export.
 * @param {boolean} [isDutch=true] - Whether to format labels in Dutch or English.
 */
export const generateParticipantChecklistPdf = (
  activity: ActivityResponseDto,
  isDutch: boolean = true,
): void => {
  const pdf = new jsPDF({
    orientation: "p",
    unit: "mm",
    format: "a4",
  });

  const pageWidth = pdf.internal.pageSize.getWidth();
  const pageHeight = pdf.internal.pageSize.getHeight();
  const margin = 14;
  const contentWidth = pageWidth - margin * 2; // 182mm

  // Clean, well-proportioned columns totaling exactly contentWidth (182mm)
  // Checkbox: 10mm (x: 14..24)
  // Index:     8mm (x: 24..32)
  // Name:     44mm (x: 32..76)
  // Diet:     54mm (x: 76..130)
  // Status:   26mm (x: 130..156)
  // Sign:     40mm (x: 156..196)
  const col = {
    check: margin, // 14
    index: margin + 10, // 24
    name: margin + 18, // 32
    diet: margin + 62, // 76
    status: margin + 116, // 130
    signature: margin + 142, // 156
    end: margin + contentWidth, // 196
  };

  let y = 16;
  const headerHeight = 7.5;
  const rowHeight = 9.5;

  const drawPageHeader = (isFirstPage = true) => {
    if (isFirstPage) {
      // Category badge
      pdf.setFontSize(7.5);
      pdf.setFont("helvetica", "bold");
      pdf.setTextColor(100, 116, 139); // slate-500
      pdf.text(
        (isDutch
          ? "DEELNEMERSLIJST / AFVINKLIJST"
          : "PARTICIPANT CHECKLIST"
        ).toUpperCase(),
        margin,
        y,
      );

      y += 6;
      // Activity Name
      pdf.setFontSize(16);
      pdf.setFont("helvetica", "bold");
      pdf.setTextColor(17, 24, 39); // gray-900
      const activityTitle =
        activity.name || (isDutch ? "Activiteit" : "Activity");
      const titleLines = pdf.splitTextToSize(activityTitle, contentWidth);
      pdf.text(titleLines[0] || "", margin, y);

      y += 5.5;
      // Date & Location
      pdf.setFontSize(8.5);
      pdf.setFont("helvetica", "normal");
      pdf.setTextColor(75, 85, 99); // gray-600

      const startDate = activity.dateTimeStart
        ? new Date(activity.dateTimeStart)
        : null;
      const dateStr = startDate
        ? startDate.toLocaleDateString(isDutch ? "nl-NL" : "en-US", {
            weekday: "short",
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
          })
        : "";

      const locationStr = activity.location ? `  |  ${activity.location}` : "";
      pdf.text(`${dateStr}${locationStr}`, margin, y);

      // Participant counts summary
      const confirmedCount = (activity.enrollments || []).filter(
        (e) => !e.isOnWaitingList,
      ).length;
      const waitingCount = (activity.enrollments || []).filter(
        (e) => e.isOnWaitingList,
      ).length;
      const maxCountStr = activity.participantLimit
        ? ` / ${activity.participantLimit}`
        : "";
      const countText = isDutch
        ? `Deelnemers: ${confirmedCount}${maxCountStr}${waitingCount > 0 ? `  |  Wachtlijst: ${waitingCount}` : ""}`
        : `Participants: ${confirmedCount}${maxCountStr}${waitingCount > 0 ? `  |  Waiting list: ${waitingCount}` : ""}`;

      y += 5;
      pdf.setFontSize(8.5);
      pdf.setFont("helvetica", "bold");
      pdf.setTextColor(31, 41, 55); // gray-800
      pdf.text(countText, margin, y);

      y += 8;
    } else {
      // Continuation Header
      pdf.setFontSize(8.5);
      pdf.setFont("helvetica", "bold");
      pdf.setTextColor(107, 114, 128); // gray-500
      const continuationText = isDutch
        ? `${activity.name || "Activiteit"} (vervolg)`
        : `${activity.name || "Activity"} (continued)`;
      pdf.text(continuationText, margin, y);
      y += 6;
    }

    // Table Header Bar
    pdf.setFillColor(243, 244, 246); // gray-100
    pdf.rect(margin, y - 4.5, contentWidth, headerHeight, "F");

    pdf.setDrawColor(209, 213, 219); // gray-300
    pdf.setLineWidth(0.3);
    pdf.line(margin, y - 4.5, col.end, y - 4.5);
    pdf.line(margin, y + headerHeight - 4.5, col.end, y + headerHeight - 4.5);

    // Vector checkmark in header
    pdf.setDrawColor(100, 116, 139);
    pdf.setLineWidth(0.45);
    pdf.line(col.check + 3.2, y - 0.2, col.check + 4.5, y + 1.1);
    pdf.line(col.check + 4.5, y + 1.1, col.check + 7.2, y - 1.8);

    pdf.setFontSize(8);
    pdf.setFont("helvetica", "bold");
    pdf.setTextColor(55, 65, 81); // gray-700

    pdf.text("#", col.index + 1, y);
    pdf.text(isDutch ? "Naam" : "Name", col.name, y);
    pdf.text(isDutch ? "Dieet / Antwoorden" : "Diet / Answers", col.diet, y);
    pdf.text("Status", col.status, y);
    pdf.text(
      isDutch ? "Handtekening / Paraaf" : "Signature / Notes",
      col.signature,
      y,
    );

    y += 8;
  };

  drawPageHeader(true);

  const sortedEnrollments = [...(activity.enrollments || [])].sort((a, b) => {
    if (a.isOnWaitingList !== b.isOnWaitingList) {
      return a.isOnWaitingList ? 1 : -1;
    }
    const nameA = getMemberFullName(a.member).toLowerCase();
    const nameB = getMemberFullName(b.member).toLowerCase();
    return nameA.localeCompare(nameB);
  });

  let hasDrawnWaitlistDivider = false;

  if (sortedEnrollments.length === 0) {
    pdf.setFontSize(9);
    pdf.setFont("helvetica", "normal");
    pdf.setTextColor(107, 114, 128);
    pdf.text(
      isDutch
        ? "Geen inschrijvingen gevonden voor deze activiteit."
        : "No enrollments found for this activity.",
      margin + 4,
      y + 2,
    );
  } else {
    sortedEnrollments.forEach((enrollment, index) => {
      // Waiting list section banner
      if (enrollment.isOnWaitingList && !hasDrawnWaitlistDivider) {
        hasDrawnWaitlistDivider = true;
        const dividerHeight = 6.5;
        if (y + dividerHeight + rowHeight > pageHeight - 16) {
          pdf.addPage("a4", "p");
          y = 16;
          drawPageHeader(false);
        }

        pdf.setFillColor(254, 243, 199); // amber-100
        pdf.rect(margin, y - 4, contentWidth, dividerHeight, "F");
        pdf.setDrawColor(245, 158, 11); // amber-500
        pdf.setLineWidth(0.25);
        pdf.line(margin, y - 4, col.end, y - 4);
        pdf.line(margin, y + dividerHeight - 4, col.end, y + dividerHeight - 4);

        pdf.setFontSize(8);
        pdf.setFont("helvetica", "bold");
        pdf.setTextColor(146, 64, 14); // amber-800
        pdf.text(
          (isDutch ? "WACHTLIJST" : "WAITING LIST").toUpperCase(),
          margin + 4,
          y + 0.3,
        );
        y += dividerHeight + 2;
      }

      if (y + rowHeight > pageHeight - 16) {
        pdf.addPage("a4", "p");
        y = 16;
        drawPageHeader(false);
      }

      const memberName = getMemberFullName(
        enrollment.member,
        isDutch ? "Onbekend" : "Unknown",
      );

      const answersText =
        (enrollment.specificationAnswers || [])
          .map((a) => a.answer)
          .filter(Boolean)
          .join(", ") || "-";

      const statusText = enrollment.isOnWaitingList
        ? isDutch
          ? "Wachtlijst"
          : "Waitlist"
        : isDutch
          ? "Deelnemer"
          : "Enrolled";

      // Subtle zebra striping on odd rows
      if (index % 2 === 1) {
        pdf.setFillColor(249, 250, 251); // gray-50
        pdf.rect(margin, y - 4, contentWidth, rowHeight, "F");
      }

      // Checkbox box
      pdf.setDrawColor(156, 163, 175); // gray-400
      pdf.setLineWidth(0.3);
      pdf.roundedRect(col.check + 2.5, y - 3.2, 4.5, 4.5, 0.6, 0.6);

      // Row sequence number
      pdf.setFontSize(8);
      pdf.setFont("helvetica", "normal");
      pdf.setTextColor(107, 114, 128); // gray-500
      pdf.text(String(index + 1), col.index + 1, y);

      // Name
      pdf.setFontSize(8.5);
      pdf.setFont("helvetica", "bold");
      pdf.setTextColor(31, 41, 55); // gray-800
      const nameLines = pdf.splitTextToSize(memberName, 42);
      pdf.text(nameLines[0] || "", col.name, y);

      // Answers / Dietary
      pdf.setFontSize(8);
      pdf.setFont("helvetica", "normal");
      pdf.setTextColor(75, 85, 99); // gray-600
      const answerLines = pdf.splitTextToSize(answersText, 52);
      pdf.text(answerLines[0] || "", col.diet, y);

      // Status
      if (enrollment.isOnWaitingList) {
        pdf.setTextColor(180, 83, 9); // amber-700
      } else {
        pdf.setTextColor(22, 101, 52); // green-700
      }
      pdf.setFont("helvetica", "normal");
      pdf.text(statusText, col.status, y);

      // Signature line (spanning 38mm across the signature column)
      pdf.setDrawColor(209, 213, 219); // gray-300
      pdf.setLineWidth(0.25);
      pdf.line(col.signature, y + 1.2, col.end - 2, y + 1.2);

      y += rowHeight;

      // Row bottom separator rule
      pdf.setDrawColor(229, 231, 235); // gray-200
      pdf.setLineWidth(0.2);
      pdf.line(margin, y - 4, col.end, y - 4);
    });
  }

  // Footer on each page
  const totalPages = pdf.getNumberOfPages();
  const printDateStr = new Date().toLocaleDateString(
    isDutch ? "nl-NL" : "en-US",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    },
  );

  for (let p = 1; p <= totalPages; p++) {
    pdf.setPage(p);
    pdf.setFontSize(7.5);
    pdf.setFont("helvetica", "normal");
    pdf.setTextColor(156, 163, 175); // gray-400

    // Footer divider line
    pdf.setDrawColor(229, 231, 235);
    pdf.setLineWidth(0.2);
    pdf.line(margin, pageHeight - 12, col.end, pageHeight - 12);

    // Left: Document info
    pdf.text(
      isDutch ? "Tavern Deelnemerslijst" : "Tavern Participant Checklist",
      margin,
      pageHeight - 8,
    );

    // Center: Page count
    const pageStr = isDutch
      ? `Pagina ${p} van ${totalPages}`
      : `Page ${p} of ${totalPages}`;
    pdf.text(pageStr, pageWidth / 2, pageHeight - 8, { align: "center" });

    // Right: Print date
    pdf.text(
      `${isDutch ? "Geprint op:" : "Printed on:"} ${printDateStr}`,
      col.end,
      pageHeight - 8,
      { align: "right" },
    );
  }

  const cleanName = (activity.name || "activiteit")
    .toLowerCase()
    .replace(/[^a-z0-9]/g, "-")
    .replace(/-+/g, "-");
  pdf.save(`afvinklijst-${cleanName}.pdf`);
};
