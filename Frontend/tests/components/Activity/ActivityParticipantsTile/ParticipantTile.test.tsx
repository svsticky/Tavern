import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api/types.gen";
import ParticipantTile from "~/components/Activity/ActivityParticipantsTile/ParticipantTile";

function buildEnrollment(
  overrides: Partial<EnrollmentResponseDto> = {},
): EnrollmentResponseDto {
  return {
    member: {
      id: "member-1",
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
          specificationAnswers: [
            { questionId: 1, answer: "Vegetarian", isPublic: true },
          ],
        } as unknown as EnrollmentResponseDto)}
      />,
    );
    expect(screen.getByText("Vegetarian")).toBeInTheDocument();
  });

  it("does not render an answer section when there are no specification answers", () => {
    render(<ParticipantTile enrollment={buildEnrollment()} />);
    expect(screen.queryByText("Vegetarian")).not.toBeInTheDocument();
  });

  it("excludes answers that are not flagged as public", () => {
    render(
      <ParticipantTile
        enrollment={buildEnrollment({
          specificationAnswers: [
            { questionId: 1, answer: "Secret", isPublic: false },
          ],
        } as unknown as EnrollmentResponseDto)}
      />,
    );
    expect(screen.queryByText("Secret")).not.toBeInTheDocument();
  });

  it("does not render an answer section when every answer is non-public", () => {
    render(
      <ParticipantTile
        enrollment={buildEnrollment({
          specificationAnswers: [
            { questionId: 1, answer: "Secret A", isPublic: false },
            { questionId: 2, answer: "Secret B", isPublic: false },
          ],
        } as unknown as EnrollmentResponseDto)}
      />,
    );
    expect(screen.queryByText("Secret A")).not.toBeInTheDocument();
    expect(screen.queryByText("Secret B")).not.toBeInTheDocument();
  });

  it("shows only the public answer out of a mix of public and non-public answers", () => {
    render(
      <ParticipantTile
        enrollment={buildEnrollment({
          specificationAnswers: [
            { questionId: 1, answer: "Secret", isPublic: false },
            { questionId: 2, answer: "Vegetarian", isPublic: true },
          ],
        } as unknown as EnrollmentResponseDto)}
      />,
    );
    expect(screen.getByText("Vegetarian")).toBeInTheDocument();
    expect(screen.queryByText("Secret")).not.toBeInTheDocument();
  });

  describe("board navigation", () => {
    it("does not render a link when isBoard is not set", () => {
      render(<ParticipantTile enrollment={buildEnrollment()} />);
      expect(screen.queryByRole("link")).not.toBeInTheDocument();
    });

    it("does not render a link when isBoard is true but the member has no id", () => {
      render(
        <ParticipantTile
          enrollment={buildEnrollment({
            member: { firstName: "Alice", lastName: "Smith" },
          } as EnrollmentResponseDto)}
          isBoard
        />,
      );
      expect(screen.queryByRole("link")).not.toBeInTheDocument();
    });

    it("links to the member's admin page when isBoard is true", () => {
      render(
        <MemoryRouter>
          <ParticipantTile enrollment={buildEnrollment()} isBoard />
        </MemoryRouter>,
      );
      expect(screen.getByRole("link")).toHaveAttribute(
        "href",
        "/admin/members/member-1",
      );
    });
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
              { questionId: 1, answer: "Answer A", isPublic: true },
              { questionId: 2, answer: "Answer B", isPublic: true },
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

    it("only ever cycles through public answers, never a non-public one", () => {
      render(
        <ParticipantTile
          enrollment={buildEnrollment({
            specificationAnswers: [
              { questionId: 1, answer: "Secret", isPublic: false },
              { questionId: 2, answer: "Answer A", isPublic: true },
              { questionId: 3, answer: "Answer B", isPublic: true },
            ],
          } as unknown as EnrollmentResponseDto)}
        />,
      );

      expect(screen.getByText("Answer A")).toBeInTheDocument();

      act(() => {
        vi.advanceTimersByTime(3000);
      });
      expect(screen.getByText("Answer B")).toBeInTheDocument();
      expect(screen.queryByText("Secret")).not.toBeInTheDocument();

      act(() => {
        vi.advanceTimersByTime(3000);
      });
      expect(screen.getByText("Answer A")).toBeInTheDocument();
      expect(screen.queryByText("Secret")).not.toBeInTheDocument();
    });

    it("keeps a tile that mounts later in sync with an already-cycling tile via the shared heartbeat", () => {
      const alice = buildEnrollment({
        specificationAnswers: [
          { questionId: 1, answer: "Alice A1", isPublic: true },
          { questionId: 2, answer: "Alice A2", isPublic: true },
        ],
      } as unknown as EnrollmentResponseDto);
      const bob = buildEnrollment({
        member: { id: "member-2", firstName: "Bob", lastName: "Jones" },
        specificationAnswers: [
          { questionId: 1, answer: "Bob B1", isPublic: true },
          { questionId: 2, answer: "Bob B2", isPublic: true },
        ],
      } as unknown as EnrollmentResponseDto);

      const { rerender } = render(<ParticipantTile enrollment={alice} />);

      // Bob's tile mounts partway through Alice's cycle, not at the shared clock's origin.
      act(() => {
        vi.advanceTimersByTime(1500);
      });
      rerender(
        <>
          <ParticipantTile enrollment={alice} />
          <ParticipantTile enrollment={bob} />
        </>,
      );

      expect(screen.getByText("Bob B1")).toBeInTheDocument();

      // Reaches t=3000 since Alice's tile mounted - the heartbeat's next tick - even though
      // Bob's tile has only been mounted for 1500ms of its own.
      act(() => {
        vi.advanceTimersByTime(1500);
      });

      expect(screen.getByText("Alice A2")).toBeInTheDocument();
      expect(screen.getByText("Bob B2")).toBeInTheDocument();
    });
  });
});
