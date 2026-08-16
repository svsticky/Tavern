import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import EditWaitinglistParticipantTile from "~/components/Activity/Edit/EditParticipantsTile/EditWaitinglistParticipantTile/EditWaitinglistParticipantTile";

const {
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
  patchEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
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

function buildEnrollment(): EnrollmentResponseDto {
  return {
    member: { id: "member-1", firstName: "Alice", lastName: "Smith" },
  } as EnrollmentResponseDto;
}

describe("EditWaitinglistParticipantTile", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the member's name", () => {
    render(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
        onMoveToParticipants={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).toBeInTheDocument();
  });

  it("calls the move-to-participants API and onMoveToParticipants when clicked", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onMoveToParticipants = vi.fn();
    render(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
        onMoveToParticipants={onMoveToParticipants}
      />,
    );
    fireEvent.click(screen.getByText("move_to_participants"));

    await vi.waitFor(() => expect(onMoveToParticipants).toHaveBeenCalled());
  });

  it("calls the unenroll API and onUnenroll when clicked", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();
    render(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={onUnenroll}
        onMoveToParticipants={vi.fn()}
      />,
    );
    fireEvent.click(screen.getByText("unenroll"));

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
  });
});
