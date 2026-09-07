import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";

function buildEnrollment(name: string): EnrollmentResponseDto {
  return {
    member: { firstName: name, lastName: "Doe" },
    specificationAnswers: [],
  } as unknown as EnrollmentResponseDto;
}

describe("ActivityParticipantsTile", () => {
  it("renders nothing when there are no enrollments", () => {
    const { container } = render(<ActivityParticipantsTile enrollments={[]} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("renders the default title and a count badge", () => {
    render(
      <ActivityParticipantsTile
        enrollments={[buildEnrollment("Alice"), buildEnrollment("Bob")]}
      />,
    );

    expect(screen.getByText("participants")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
  });

  it("renders a custom title when provided", () => {
    render(
      <ActivityParticipantsTile
        title="Attendees"
        enrollments={[buildEnrollment("Alice")]}
      />,
    );
    expect(screen.getByText("Attendees")).toBeInTheDocument();
  });

  it("renders a ParticipantTile for each enrollment", () => {
    render(
      <ActivityParticipantsTile
        enrollments={[buildEnrollment("Alice"), buildEnrollment("Bob")]}
      />,
    );

    expect(screen.getByText("Alice Doe")).toBeInTheDocument();
    expect(screen.getByText("Bob Doe")).toBeInTheDocument();
  });

  it("renders export_csv button and handles click when onExportCsv is provided", () => {
    const onExport = vi.fn();
    render(
      <ActivityParticipantsTile
        enrollments={[buildEnrollment("Alice")]}
        onExportCsv={onExport}
      />,
    );

    const exportBtn = screen.getByRole("button", { name: /export_csv/i });
    expect(exportBtn).toBeInTheDocument();

    fireEvent.click(exportBtn);
    expect(onExport).toHaveBeenCalledTimes(1);
  });

  it("renders export_pdf button and handles click when onExportPdf is provided", () => {
    const onExportPdf = vi.fn();
    render(
      <ActivityParticipantsTile
        enrollments={[buildEnrollment("Alice")]}
        onExportPdf={onExportPdf}
      />,
    );

    const exportPdfBtn = screen.getByRole("button", { name: /export_pdf/i });
    expect(exportPdfBtn).toBeInTheDocument();

    fireEvent.click(exportPdfBtn);
    expect(onExportPdf).toHaveBeenCalledTimes(1);
  });
});
