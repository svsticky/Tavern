import { fireEvent, render, screen } from "@testing-library/react";
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

  it("allows switching between upcoming and past enrollments tabs", () => {
    const futureDate = new Date(Date.now() + 86400000).toISOString();
    const pastDate = new Date(Date.now() - 86400000).toISOString();

    const activities: ActivityResponseDto[] = [
      {
        id: 1,
        name: "Future Activity",
        dateTimeStart: futureDate,
        dateTimeEnd: futureDate,
      } as ActivityResponseDto,
      {
        id: 2,
        name: "Past Activity",
        dateTimeStart: pastDate,
        dateTimeEnd: pastDate,
      } as ActivityResponseDto,
    ];

    render(
      <MemoryRouter>
        <ActivityEnrollmentOverview enrolledActivities={activities} />
      </MemoryRouter>,
    );

    // By default upcoming tab is active with Future Activity visible
    expect(screen.getByText("Future Activity")).toBeInTheDocument();
    expect(screen.queryByText("Past Activity")).not.toBeInTheDocument();

    // Click Past tab
    const pastButton = screen.getByRole("button", { name: /past/i });
    fireEvent.click(pastButton);

    // Now Past Activity should be visible
    expect(screen.getByText("Past Activity")).toBeInTheDocument();
    expect(screen.queryByText("Future Activity")).not.toBeInTheDocument();

    // Click Upcoming tab to switch back
    const upcomingButton = screen.getByRole("button", { name: /upcoming/i });
    fireEvent.click(upcomingButton);
    expect(screen.getByText("Future Activity")).toBeInTheDocument();
  });

  it("defaults to past tab when only past activities exist, and shows no_enrollments on upcoming tab", () => {
    const pastDate = new Date(Date.now() - 86400000).toISOString();
    const activities: ActivityResponseDto[] = [
      {
        id: 2,
        name: "Only Past Activity",
        dateTimeStart: pastDate,
        dateTimeEnd: pastDate,
      } as ActivityResponseDto,
    ];

    render(
      <MemoryRouter>
        <ActivityEnrollmentOverview enrolledActivities={activities} />
      </MemoryRouter>,
    );

    // Initial state should be past
    expect(screen.getByText("Only Past Activity")).toBeInTheDocument();

    // Switch to upcoming
    const upcomingButton = screen.getByRole("button", { name: /upcoming/i });
    fireEvent.click(upcomingButton);
    expect(screen.getByText("no_enrollments")).toBeInTheDocument();
  });

  it("shows no_past_enrollments message when past tab is selected with no past activities", () => {
    const futureDate = new Date(Date.now() + 86400000).toISOString();

    const activities: ActivityResponseDto[] = [
      {
        id: 1,
        name: "Only Future Activity",
        dateTimeStart: futureDate,
        dateTimeEnd: futureDate,
      } as ActivityResponseDto,
    ];

    render(
      <MemoryRouter>
        <ActivityEnrollmentOverview enrolledActivities={activities} />
      </MemoryRouter>,
    );

    const pastButton = screen.getByRole("button", { name: /past/i });
    fireEvent.click(pastButton);

    expect(screen.getByText("no_past_enrollments")).toBeInTheDocument();
  });
});
