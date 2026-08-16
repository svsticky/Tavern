import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api/types.gen";
import ParticipantTile from "~/components/Activity/ActivityParticipantsTile/ParticipantTile";

function buildEnrollment(
  overrides: Partial<EnrollmentResponseDto> = {},
): EnrollmentResponseDto {
  return {
    member: {
      firstName: "Alice",
      lastName: "Smith",
      profilePicturePath: "alice.png",
    },
    specificationAnswers: [],
    ...overrides,
  } as EnrollmentResponseDto;
}

describe("ParticipantTile", () => {
  it("renders the member's name", () => {
    render(<ParticipantTile enrollment={buildEnrollment()} />);
    expect(screen.getByText("Alice Smith")).toBeInTheDocument();
  });

  it("falls back to the default avatar when there is no profile picture path", () => {
    render(
      <ParticipantTile
        enrollment={buildEnrollment({
          member: { firstName: "Alice", lastName: "Smith" },
        } as EnrollmentResponseDto)}
      />,
    );
    expect(screen.getByAltText("Profile")).toHaveAttribute(
      "src",
      "/profile-picture.svg",
    );
  });

  it("falls back to the default avatar when the profile image fails to load", () => {
    render(<ParticipantTile enrollment={buildEnrollment()} />);
    const img = screen.getByAltText("Profile");
    fireEvent.error(img);
    expect(img).toHaveAttribute("src", "/profile-picture.svg");
  });

  it("shows the first specification answer when there is exactly one", () => {
    render(
      <ParticipantTile
        enrollment={buildEnrollment({
          specificationAnswers: [{ questionId: 1, answer: "Vegetarian" }],
        } as unknown as EnrollmentResponseDto)}
      />,
    );
    expect(screen.getByText("Vegetarian")).toBeInTheDocument();
  });

  it("does not render an answer section when there are no specification answers", () => {
    render(<ParticipantTile enrollment={buildEnrollment()} />);
    expect(screen.queryByText("Vegetarian")).not.toBeInTheDocument();
  });

  describe("cycling between multiple answers", () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it("cycles to the next answer every 3 seconds", () => {
      render(
        <ParticipantTile
          enrollment={buildEnrollment({
            specificationAnswers: [
              { questionId: 1, answer: "Answer A" },
              { questionId: 2, answer: "Answer B" },
            ],
          } as unknown as EnrollmentResponseDto)}
        />,
      );

      expect(screen.getByText("Answer A")).toBeInTheDocument();

      act(() => {
        vi.advanceTimersByTime(3000);
      });

      expect(screen.getByText("Answer B")).toBeInTheDocument();
    });
  });
});
