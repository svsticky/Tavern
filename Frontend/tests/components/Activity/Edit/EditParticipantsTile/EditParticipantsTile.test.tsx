import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import EditParticipantsTile from "~/components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import {
  handleDownloadEnrollments,
  handleEnrollParticipant,
  handleMoveToParticipants,
  handleUnenrollParticipant,
} from "~/components/Activity/Edit/EditParticipantsTile/EditParticipantsTile.handlers";

vi.mock(
  "~/components/Activity/Edit/EditParticipantsTile/EditParticipantsTile.handlers",
  () => ({
    handleDownloadEnrollments: vi.fn(),
    handleEnrollParticipant: vi.fn(),
    handleMoveToParticipants: vi.fn(),
    handleUnenrollParticipant: vi.fn(),
  }),
);

vi.mock("~/components/Member/SearchMemberOverlay", () => ({
  default: ({ onSelect }: { onSelect: (m: any) => void }) => (
    <button type="button" onClick={() => onSelect({ id: "m1" })}>
      select-member
    </button>
  ),
}));

vi.mock("~/api", () => ({
  deleteEnrollmentsByActivityIdByMemberId: vi.fn().mockResolvedValue({}),
  patchEnrollmentsByActivityIdByMemberId: vi.fn().mockResolvedValue({}),
}));

vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => args, {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    enrollments: [],
    ...overrides,
  } as ActivityResponseDto;
}

describe("EditParticipantsTile", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a placeholder when there are no participants", () => {
    render(
      <EditParticipantsTile activity={buildActivity()} setActivity={vi.fn()} />,
    );
    expect(screen.getByText("no_participants_yet")).toBeInTheDocument();
  });

  it("renders the participant count with the capacity limit", () => {
    render(
      <EditParticipantsTile
        activity={buildActivity({
          participantLimit: 20,
          enrollments: [
            { member: { id: "m1", firstName: "A", lastName: "B" } },
          ] as ActivityResponseDto["enrollments"],
        })}
        setActivity={vi.fn()}
      />,
    );
    expect(screen.getByText("participants (1/20)")).toBeInTheDocument();
  });

  it("calls handleDownloadEnrollments when the download button is clicked", () => {
    const activity = buildActivity();
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);
    fireEvent.click(screen.getByText("download_enrollments"));
    expect(handleDownloadEnrollments).toHaveBeenCalledWith(activity);
  });

  it("renders enrolled participants and the waiting list separately", () => {
    render(
      <EditParticipantsTile
        activity={buildActivity({
          enrollments: [
            {
              member: { id: "m1", firstName: "Alice", lastName: "A" },
              isOnWaitingList: false,
            },
            {
              member: { id: "m2", firstName: "Bob", lastName: "B" },
              isOnWaitingList: true,
            },
          ] as ActivityResponseDto["enrollments"],
        })}
        setActivity={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice A")).toBeInTheDocument();
    expect(screen.getByText("Bob B")).toBeInTheDocument();
    expect(screen.getByText("waiting_list (1)")).toBeInTheDocument();
  });

  it("calls handleUnenrollParticipant when a participant's unenroll button is clicked", async () => {
    const activity = buildActivity({
      enrollments: [
        {
          member: { id: "m1", firstName: "Alice", lastName: "A" },
          isOnWaitingList: false,
        },
      ] as ActivityResponseDto["enrollments"],
    });
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);
    fireEvent.click(screen.getByText("unenroll"));
    await vi.waitFor(() =>
      expect(handleUnenrollParticipant).toHaveBeenCalledWith(
        "m1",
        activity,
        expect.any(Function),
      ),
    );
  });

  it("calls handleMoveToParticipants when moving the first waiting-list member", async () => {
    const activity = buildActivity({
      enrollments: [
        {
          member: { id: "m2", firstName: "Bob", lastName: "B" },
          isOnWaitingList: true,
        },
      ] as ActivityResponseDto["enrollments"],
    });
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);
    fireEvent.click(screen.getByText("move_to_participants"));
    await vi.waitFor(() =>
      expect(handleMoveToParticipants).toHaveBeenCalledWith(
        "m2",
        activity,
        expect.any(Function),
      ),
    );
  });

  it("calls handleUnenrollParticipant when unenrolling the first waiting-list member", async () => {
    const activity = buildActivity({
      enrollments: [
        {
          member: { id: "m2", firstName: "Bob", lastName: "B" },
          isOnWaitingList: true,
        },
      ] as ActivityResponseDto["enrollments"],
    });
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);
    fireEvent.click(screen.getByText("unenroll"));
    await vi.waitFor(() =>
      expect(handleUnenrollParticipant).toHaveBeenCalledWith(
        "m2",
        activity,
        expect.any(Function),
      ),
    );
  });

  it("closes the search modal without enrolling when dismissed", () => {
    const activity = buildActivity();
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);

    fireEvent.click(screen.getByText("enroll_member"));
    expect(screen.getByText("select-member")).toBeInTheDocument();

    fireEvent.keyDown(window, { key: "Escape" });

    expect(screen.queryByText("select-member")).not.toBeInTheDocument();
  });

  it("opens the search modal and calls handleEnrollParticipant on member selection", () => {
    const activity = buildActivity();
    render(<EditParticipantsTile activity={activity} setActivity={vi.fn()} />);

    fireEvent.click(screen.getByText("enroll_member"));
    fireEvent.click(screen.getByText("select-member"));

    expect(handleEnrollParticipant).toHaveBeenCalledWith(
      expect.objectContaining({
        member: { id: "m1" },
        activity,
      }),
    );
  });
});
