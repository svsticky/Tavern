import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";

function buildEnrollment(name: string): EnrollmentResponseDto {
  return {
    member: { id: `${name}-id`, firstName: name, lastName: "Doe" },
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

  it("does not link participant tiles when isBoard is not set", () => {
    render(
      <ActivityParticipantsTile enrollments={[buildEnrollment("Alice")]} />,
    );
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });

  it("forwards isBoard to each ParticipantTile so they link to the member's admin page", () => {
    render(
      <MemoryRouter>
        <ActivityParticipantsTile
          enrollments={[buildEnrollment("Alice"), buildEnrollment("Bob")]}
          isBoard
        />
      </MemoryRouter>,
    );

    expect(screen.getAllByRole("link")).toHaveLength(2);
    expect(screen.getByText("Alice Doe").closest("a")).toHaveAttribute(
      "href",
      "/admin/members/Alice-id",
    );
  });
});
