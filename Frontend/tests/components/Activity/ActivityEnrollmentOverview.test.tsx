import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivityEnrollmentOverview from "~/components/Activity/ActivityEnrollmentOverview";

describe("ActivityEnrollmentOverview", () => {
  it("shows a no-content message when there are no enrollments", () => {
    render(
      <MemoryRouter>
        <ActivityEnrollmentOverview enrolledActivities={[]} />
      </MemoryRouter>,
    );
    expect(screen.getByText("no_enrollments")).toBeInTheDocument();
  });

  it("renders each enrolled activity as a link with its name and date", () => {
    const activities: ActivityResponseDto[] = [
      {
        id: 1,
        name: "Party",
        dateTimeStart: "2026-08-01T10:00:00Z",
      } as ActivityResponseDto,
    ];

    render(
      <MemoryRouter>
        <ActivityEnrollmentOverview enrolledActivities={activities} />
      </MemoryRouter>,
    );

    expect(screen.getByText("Party")).toBeInTheDocument();
    expect(screen.getByRole("link")).toHaveAttribute("href", "/activities/1");
  });
});
